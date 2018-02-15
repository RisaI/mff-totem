using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.Common;
using FarseerPhysics.Common.Decomposition;

using ClipperLib;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public class Terrain
	{
		const int CHUNK_CACHE = 7;

		public const int SPACING = 32;
		public const int BASE_HEIGHT = 0, BASE_STEP = 2048;

		public OpenSimplexNoise NoiseMap
		{
			get;
			private set;
		}

		long _seed;
		public long Seed
		{
			get { return _seed; }
			set { _seed = value; NoiseMap = new OpenSimplexNoise(value); }
		}

		public GameWorld World
		{
			get;
			private set;
		}

		public Dictionary<ulong, Chunk> ChunkCache = new Dictionary<ulong, Chunk>();
		public Chunk[] ActiveChunks = new Chunk[CHUNK_CACHE * CHUNK_CACHE];
		//Body TerrainBody;

		public Terrain(GameWorld world)
		{
			World = world;
			c = new Clipper();
		}

		Clipper c;
		Random Random;
		public void Generate(long seed = 0)
		{
			Random = new Random();
			Seed = seed;
		}

		public void Damage(params IntPoint[] points)
		{
			Damage(points.ToList());
		}

		public void Damage(List<IntPoint> points)
		{
			damageQueue.Add(points);
		}

		private void ApplyDamage(List<IntPoint> damage)
		{
			long minX = long.MaxValue, minY = long.MaxValue, 
				maxX = long.MinValue, maxY = long.MinValue;

			for (int i = 0; i < damage.Count; ++i)
			{
				if (damage[i].X < minX)
					minX = damage[i].X;
				if (damage[i].X > maxX)
					maxX = damage[i].X;
				
				if (damage[i].Y < minY)
					minY = damage[i].Y;
				if (damage[i].Y > maxY)
					maxY = damage[i].Y;
			}

			minX = Helper.NegDivision(minX, Chunk.SIZE);
			maxX = Helper.NegDivision(maxX, Chunk.SIZE);
			minY = Helper.NegDivision(minY, Chunk.SIZE);
			maxY = Helper.NegDivision(maxY, Chunk.SIZE);

			var clipper = new Clipper();
			for (int y = (int)minY; y <= maxY; ++y)
			{
				for (int x = (int)minX; x <= maxX; ++x)
				{
					var chunk = GetChunk(TerrainHelper.PackCoordinates(x, y));

					clipper.Clear();
					clipper.AddPolygon(new List<IntPoint>() {
						new IntPoint(chunk.Left, chunk.Top),
						new IntPoint(chunk.Left + Chunk.SIZE, chunk.Top),
						new IntPoint(chunk.Left + Chunk.SIZE, chunk.Top + Chunk.SIZE),
						new IntPoint(chunk.Left, chunk.Top + Chunk.SIZE)
					}, PolyType.ptSubject);
					clipper.AddPolygon(damage, PolyType.ptClip);
					clipper.AddPolygons(chunk.Damage, PolyType.ptClip);
					clipper.Execute(ClipType.ctIntersection, chunk.Damage, 
					                PolyFillType.pftNonZero, PolyFillType.pftNonZero);

					chunk.Recalculate = true;
					if (ActiveChunks.Contains(chunk))
						Task.Run(() => { PlaceChunk(chunk); });
				}
			}
		}

		List<List<IntPoint>> damageQueue = new List<List<IntPoint>>();
		public void Update()
		{
			damageQueue.ForEach(d => ApplyDamage(d));
			damageQueue.Clear();
		}

		public float HeightMap(float x)
		{
			return BASE_HEIGHT + (float)((NoiseMap.Evaluate(x / (Chunk.SIZE * 32), 0) - 0.5) * BASE_STEP + 8 * NoiseMap.Evaluate(x / 128, Chunk.SIZE));
		}

		public ulong ChunkID(float x, float y)
		{
			return TerrainHelper.PackCoordinates(Helper.NegDivision((int)x, Chunk.SIZE), 
			                                     Helper.NegDivision((int)y, Chunk.SIZE));
		}

		int _activeX, _activeY;
		public void ActiveRegion(Vector2 center)
		{
			_activeX = Helper.NegDivision((int)center.X, Chunk.SIZE);
			_activeY = Helper.NegDivision((int)center.Y, Chunk.SIZE);

			Chunk[] newActive = new Chunk[CHUNK_CACHE * CHUNK_CACHE];
			for (int y = 0; y < CHUNK_CACHE; ++y)
			{
				for (int x = 0; x < CHUNK_CACHE; ++x)
				{
					var cX = x + _activeX - CHUNK_CACHE / 2;
					var cY = y + _activeY - CHUNK_CACHE / 2;
					var id = TerrainHelper.PackCoordinates(cX, cY);
					Chunk chunk = null;
					if (ActiveChunks.Any(c => c != null && c.ID == id))
					{
						chunk = ActiveChunks.First(c => c != null && c.ID == id);
					}
					else
					{
						chunk = GetChunk(id);
						Task.Run(() => { PlaceChunk(chunk); });
					}
					newActive[x + y * CHUNK_CACHE] = chunk;
				}
			}

			for (int i = 0; i < ActiveChunks.Length; ++i)
			{
				if (ActiveChunks[i] != null && !newActive.Contains(ActiveChunks[i]))
				{
					UnplaceChunk(ActiveChunks[i]);
					if (ActiveChunks[i].Damage.Count <= 0)
						ChunkCache.Remove(ActiveChunks[i].ID);
				}
			}
			ActiveChunks = newActive;
		}

		public Chunk GetChunk(ulong id, bool onlyread = false)
		{
			if (ChunkCache.ContainsKey(id))
				return ChunkCache[id];
			else
			{
				Chunk chunk = new Chunk(id);
				if (!onlyread)
					ChunkCache.Add(id, chunk);
				return chunk;
			}
		}

		public void GenerateChunk(Chunk chunk)
		{
			if (chunk.State != ChunkStateEnum.Emtpy)
				return;

			long top = chunk.Top, left = chunk.Left;
			var clipper = new Clipper();

			chunk.Polygons = new List<List<IntPoint>>();

			// Add chunk bounding rectangle
			clipper.AddPolygon(new List<IntPoint>() { 
				new IntPoint(left, top),
				new IntPoint(left + Chunk.SIZE, top),
				new IntPoint(left + Chunk.SIZE, top + Chunk.SIZE),
				new IntPoint(left, top + Chunk.SIZE)
			}, PolyType.ptSubject);

			List<IntPoint> surfacePolygon = new List<IntPoint>(Chunk.SIZE / SPACING + 3);
			for (int i = 0; i <= Chunk.SIZE / SPACING; ++i)
			{
				surfacePolygon.Add(new IntPoint(left + i * SPACING, (int)HeightMap(left + i * SPACING)));
			}
			surfacePolygon.Add(new IntPoint(left + Chunk.SIZE, top + Chunk.SIZE));
			surfacePolygon.Add(new IntPoint(left, top + Chunk.SIZE));
			clipper.AddPolygon(surfacePolygon, PolyType.ptClip);

			// Add solution to chunk data
			clipper.Execute(ClipType.ctIntersection, 
			                chunk.Polygons, 
			                PolyFillType.pftNonZero,
			                PolyFillType.pftNonZero);

			chunk.Cavities = new List<List<IntPoint>>();

			chunk.Recalculate = true;
			chunk.State = ChunkStateEnum.Generated;
		}

		public void PlaceChunk(Chunk chunk)
		{
			if (chunk.State == ChunkStateEnum.Emtpy)
				GenerateChunk(chunk);

			if (chunk.Recalculate)
				chunk.Calculate();

			lock (World.Physics)
			{
				lock (chunk)
				{
					if (chunk.Body != null)
						World.Physics.RemoveBody(chunk.Body);

					chunk.Body = BodyFactory.CreateBody(World.Physics, Vector2.Zero, 0, BodyType.Static, this);

					chunk.PhysicsOutput.ForEach(o =>
					{
						var fixture = FixtureFactory.AttachLoopShape(o, chunk.Body, this);
					});
				}
			}

			chunk.State = ChunkStateEnum.Placed;
		}

		public void UnplaceChunk(Chunk chunk)
		{
			if (chunk.State != ChunkStateEnum.Placed)
				return;

			lock (World.Physics)
			{
				World.Physics.RemoveBody(chunk.Body);
			}
			chunk.Body = null;
			chunk.State = ChunkStateEnum.Generated;
		}

		public static class TerrainHelper
		{
			public static ulong PackCoordinates(int x, int y)
			{
				return ((ulong)((uint)x) << 32) | (uint)y;
			}

			public static Tuple<int, int> UnpackCoordinates(ulong packed)
			{
				return Tuple.Create((int)(packed >> 32), (int)packed);
			}

			public static VertexPositionColor[] TriangulatedRenderData(List<Vertices> triangulated, Color c)
			{
				var output = new VertexPositionColor[triangulated.Count * 3];
				int index = 0;
				triangulated.ForEach(triangle => triangle.ForEach(vert =>
																  output[index++] = new VertexPositionColor(new Vector3(vert.X, vert.Y, 0), c)));
				return output;
			}

			public static List<Vertices> Triangulate(List<IntPoint> polygon, TriangulationAlgorithm algo = TriangulationAlgorithm.Earclip)
			{
				return FarseerPhysics.Common.Decomposition.Triangulate.ConvexPartition(PolygonToVertices(polygon, 1), algo);
			}

			public static List<Vertices> Triangulate(List<List<IntPoint>> polygons, TriangulationAlgorithm algo = TriangulationAlgorithm.Earclip)
			{
				List<Vertices> result = new List<Vertices>();
				polygons.ForEach(p => { result.AddRange(Triangulate(p)); });
				return result;
			}

			public static Vertices PolygonToVertices(List<IntPoint> polygon, float scale = 1 / 64f)
			{
				Vector2[] v = new Vector2[polygon.Count];
				int i = 0;
				polygon.ForEach(p => v[i++] = new Vector2(p.X, p.Y) * scale);
				return new Vertices(v);
			}

			public static List<IntPoint> VerticesToPolygon(Vertices verts, float scale = 1 / 64f)
			{
				List<IntPoint> result = new List<IntPoint>();
				verts.ForEach(v => result.Add(new IntPoint((int)(v.X * scale), (int)(v.Y * scale))));
				return result;
			}
		}

		public class Chunk
		{
			public const int SIZE = 512;

			public ulong ID;
			//public Task GenerationTask;
			public ChunkStateEnum State;
			public bool Recalculate = true;

			public List<Entity> Trees = new List<Entity>();

			public List<List<IntPoint>> Polygons;
			public List<List<IntPoint>> Cavities;
			public List<List<IntPoint>> Damage;

			// Calculated by the Calculate method
			public List<Vertices> PhysicsOutput
			{
				get;
				private set;
			}

			// Calculated by the Calculate method
			public VertexPositionColor[] TriangulatedBackgroundVertices
			{
				get;
				private set;
			}

			// Calculated by the Calculate method
			public VertexPositionColor[] TriangulatedForegroundVertices
			{
				get;
				private set;
			}

			public Body Body;

			public long Left
			{
				get { return X * SIZE; }
			}

			public long Right
			{
				get { return Left + SIZE; }
			}

			public long Top
			{
				get { return Y * SIZE; }
			}

			public long Bottom
			{
				get { return Top + SIZE; }
			}

			public int X
			{
				get { return TerrainHelper.UnpackCoordinates(ID).Item1; }
			}

			public int Y
			{
				get { return TerrainHelper.UnpackCoordinates(ID).Item2; }
			}

			public Rectangle BoundingBox
			{
				get { return new Rectangle((int)Left, (int)Top, SIZE, SIZE); }
			}

			public Chunk(int x, int y) :
				this(Terrain.TerrainHelper.PackCoordinates(x, y))
			{

			}

			public Chunk(ulong id)
			{
				ID = id;
				State = ChunkStateEnum.Emtpy;
				PhysicsOutput = new List<Vertices>();
				Damage = new List<List<IntPoint>>();
			}

			public void Calculate()
			{
				var clipper = new Clipper();
				List<List<IntPoint>> output = new List<List<IntPoint>>();

				// Calculate difference
				clipper.AddPolygons(Polygons, PolyType.ptSubject);
				clipper.AddPolygons(Cavities, PolyType.ptClip);
				clipper.AddPolygons(Damage, PolyType.ptClip);
				clipper.Execute(ClipType.ctDifference, output, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

				// Triangulate background for rendering
				{
					var triangulation = TerrainHelper.Triangulate(Polygons);
					TriangulatedBackgroundVertices = new VertexPositionColor[triangulation.Count * 3];
					int index = 0;
					for (int i = 0; i < triangulation.Count; ++i)
						for (int b = 0; b < 3; ++b)
							TriangulatedBackgroundVertices[index++] = new VertexPositionColor(
								new Vector3(triangulation[i][b].X, triangulation[i][b].Y, 0), Color.Gray);
				}

				// Triangulate foreground for rendering
				{
					var triangulation = TerrainHelper.Triangulate(output);
					TriangulatedForegroundVertices = new VertexPositionColor[triangulation.Count * 3];
					int index = 0;
					for (int i = 0; i < triangulation.Count; ++i)
						for (int b = 0; b < 3; ++b)
							TriangulatedForegroundVertices[index++] = new VertexPositionColor(
								new Vector3(triangulation[i][b].X, triangulation[i][b].Y, 0), Color.White);
				}

				// Convert to Farseer data
				PhysicsOutput.Clear();
				output.ForEach(p =>
				{
					PhysicsOutput.Add(TerrainHelper.PolygonToVertices(p));
				});
			}
		}

		public enum ChunkStateEnum
		{
			Emtpy,
			Generated,
			Placed
		}
	}
}
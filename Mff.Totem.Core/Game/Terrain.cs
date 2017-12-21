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
		const int CHUNK_CACHE = 64;

		public const int MAX_DEPTH = 10000 * 6, SPACING = 32;
		public const int BASE_HEIGHT = 0, BASE_STEP = 2048;
		public const int CAVE_SPACING = Chunk.WIDTH / 16;

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

		public List<List<IntPoint>> DamageMap;

		public Terrain(GameWorld world)
		{
			World = world;
			DamageMap = new List<List<IntPoint>>();
			c = new Clipper();
		}

		Clipper c;
		public void Generate(long seed = 0)
		{
			Random = new Random();
			Seed = seed != 0 ? seed : (long)Random.Next();
			DamageMap.Clear();
			TerrainBody = BodyFactory.CreateBody(World.Physics, Vector2.Zero, 0, BodyType.Static, this);
			SetActiveRegion(-1, 0, false);
		}

		public void Update()
		{
			
		}

		public void CreateDamage(List<IntPoint> polygon)
		{
			DamageMap.Add(polygon);
			c.Clear();
			c.AddPolygons(DamageMap, PolyType.ptClip);
			DamageMap.Clear();
			c.Execute(ClipType.ctXor, DamageMap, PolyFillType.pftPositive, PolyFillType.pftNonZero);
			var left = Helper.NegDivision(DamageMap.Min(po => po.Min(p => p.X)), Chunk.WIDTH);
			var right = Helper.NegDivision(DamageMap.Max(po => po.Max(p => p.X)), Chunk.WIDTH);

			for (long i = left; i <= right; ++i)
			{
				Chunk ch = ActiveChunks.Find(c => c.ID == i);
				if (ch != null)
					PlaceChunk(ch);
			}
		}

		public void CreateDamage(params IntPoint[] points)
		{
			CreateDamage(points.ToList());
		}

		Body TerrainBody;

		private Random Random;
		private void CreateChunkTerrain(Chunk ch)
		{
			long x = ch.Left;
			List<List<IntPoint>> solution = new List<List<IntPoint>>(), cavities = new List<List<IntPoint>>();
			List<IntPoint> verts = new List<IntPoint>();

			// Add end faces
			verts.Add(new IntPoint(x, MAX_DEPTH));

			for (int i = 0; i <= Chunk.WIDTH / SPACING; ++i)
			{
				long x0 = x + i * SPACING, height = (long)HeightMap(x0);
				verts.Add(new IntPoint(x0, height));
			}

			var cl = new Clipper();

			/*for (int tX = 0; tX < Chunk.WIDTH / CAVE_SPACING; ++tX)
			{
				for (int tY = 0; tY < (MAX_DEPTH - BASE_HEIGHT - BASE_STEP / 2) / CAVE_SPACING; ++tY)
				{
					var val = NoiseMap.Evaluate((x + tX * CAVE_SPACING) / (1024.0), (tY*CAVE_SPACING) / 1024.0);
					if (val > 0.3)
						cl.AddPolygon(Helper.CreateRectangle((int)x + tX*CAVE_SPACING, BASE_HEIGHT + BASE_STEP / 2 + tY*CAVE_SPACING,CAVE_SPACING, CAVE_SPACING), PolyType.ptClip);
				}
			}*/

			// Add second end face point
			verts.Add(new IntPoint(x + Chunk.WIDTH, MAX_DEPTH));

			cl.AddPolygon(verts, PolyType.ptSubject);
			//cl.Execute(ClipType.ctDifference, solution, PolyFillType.pftPositive, PolyFillType.pftNonZero);
			cl.Execute(ClipType.ctIntersection, cavities, PolyFillType.pftPositive, PolyFillType.pftNonZero);

			cavities.ForEach(c =>
			{
				for (int i = 0; i < c.Count; ++i)
				{
					var p = c[i];
					if (p.X == ch.Left)
						c[i] = new IntPoint(p.X - 1, p.Y);
					if (p.X == ch.Right)
						c[i] = new IntPoint(p.X + 1, p.Y);
				}
			});

			ch.Polygon = verts;
			ch.Cavities = cavities;
			ch.TriangulatedVertices = Chunk.TriangulatedRenderData(Helper.Triangulate(solution), Color.White);
			ch.TriangulatedWholeVertices = Chunk.TriangulatedRenderData(Helper.Triangulate(verts), Color.Gray);
		}

		void GenerateChunk(Chunk ch)
		{
			if (ch.Generated)
				return;
			CreateChunkTerrain(ch);
			for (int i = 0; i < 16; ++i)
			{
				var tree = ContentLoader.Entities["tree"].Clone();
				var pos = new Vector2(ch.Left + (i + 1) * (Chunk.WIDTH / 16f), 0);
				pos.Y = HeightMap(pos.X);
				tree.GetComponent<BodyComponent>().Position = pos;
				ch.Trees.Add(tree);
			}
			ch.Generated = true;
		}

		void PlaceChunk(Chunk ch)
		{
			if (!ch.Generated)
				return;

			var cl = new Clipper();
			cl.Clear();
			cl.AddPolygon(ch.Polygon, PolyType.ptSubject);
			cl.AddPolygons(ch.Cavities, PolyType.ptClip);
			cl.AddPolygons(DamageMap, PolyType.ptClip);
			List<List<IntPoint>> result = new List<List<IntPoint>>();
			cl.Execute(ClipType.ctDifference, result, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

			var holes = new List<List<IntPoint>>();
			cl.Execute(ClipType.ctIntersection, holes, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
			ch.TriangulatedVertices = Chunk.TriangulatedRenderData(Helper.TriangulateWithHoles(new List<List<IntPoint>>() { ch.Polygon }, holes), Color.White);


			lock (TerrainBody)
			{
				UnplaceChunk(ch);

				result.ForEach(polygon =>
				{
					var f = FixtureFactory.AttachLoopShape(Helper.PolygonToVertices(polygon), TerrainBody, this);
					ch.Fixtures.Add(f);
				});

				ch.Trees.ForEach(tree =>
				{
					tree.Remove = false;
					World.SpawnEntity(tree);
				});
			}
		}

		void UnplaceChunk(Chunk ch)
		{
			lock (TerrainBody)
			{
				if (ch.Fixtures.Count > 0)
				{
					ch.Fixtures.ForEach(f => TerrainBody.DestroyFixture(f));
					ch.Fixtures.Clear();
				}
			}
			ch.Trees.ForEach(t => t.Remove = true);
		}

		public void ClearFromWorld()
		{
			if (TerrainBody != null)
			{
				World.Physics.RemoveBody(TerrainBody);
				TerrainBody = null;
			}
		}

		List<Chunk> ChunkCache = new List<Chunk>(CHUNK_CACHE);
		public List<Chunk> ActiveChunks = new List<Chunk>();
		/// <summary>
		/// Sets the active region of terrain. Only chunks in this region will be displayed in world.
		/// </summary>
		/// <param name="a">Left bound.</param>
		/// <param name="b">Right bound.</param>
		public void SetActiveRegion(long a, long b, bool asynch = true)
		{
			if (a > b)
			{
				a = a ^ b;
				b = a ^ b;
				a = a ^ b;
			}

			a = Helper.NegDivision(a, Chunk.WIDTH);
			b = Helper.NegDivision(b, Chunk.WIDTH) + 1;
			for (long i = a; i < b; ++i)
			{
				Chunk ch = ActiveChunks.Find(c => c.ID == i);
				if (ch == null)
				{
					ch = ChunkCache.Find(c => c.ID == i);
					if (ch == null)
					{
						ActiveChunks.Add(ch = new Chunk(i));
						CacheChunk(ch);
						CreateGenerationTask(ch, asynch);
					}
					else
					{
						ActiveChunks.Add(ch);
						PlaceChunk(ch);
					}
				}
			}
			ActiveChunks.ForEach(ch => {
				if (ch.ID < a || ch.ID >= b)
					UnplaceChunk(ch);
			});
			ActiveChunks.RemoveAll(ch => ch.ID < a || ch.ID >= b);
		}

		void CacheChunk(Chunk ch)
		{
			if (ChunkCache.Count >= CHUNK_CACHE)
			{
				var center = (ActiveChunks.Min(c => c.ID) + ActiveChunks.Max(c => c.ID)) / 2;
				Chunk furthest = null;
				long dist = 0;
				ChunkCache.ForEach(c =>
				{
					var d = Math.Abs(c.ID - center);
					if (furthest == null || d > dist)
					{
						furthest = c;
						dist = d;
					}
				});
				ChunkCache.Remove(furthest);
			}
			ChunkCache.Add(ch);
		}

		void CreateGenerationTask(Chunk ch, bool asynch = true)
		{
			if (asynch)
			{
				if (ch.GenerationTask != null)
				{
					ch.GenerationTask.Wait();
					ch.GenerationTask.Dispose();
				}
				ch.GenerationTask = Task.Run(() => { GenerateChunk(ch); PlaceChunk(ch); });
			}
			else
			{
				GenerateChunk(ch);
				PlaceChunk(ch);
			}
		}

		public float HeightMap(float x)
		{
			return BASE_HEIGHT + (float)((NoiseMap.Evaluate(x / (Chunk.WIDTH * 8), 0)- 0.5) * BASE_STEP + 8 * NoiseMap.Evaluate(x / 128, Chunk.WIDTH));
		}

		public long ChunkID(float x)
		{
			return Helper.NegDivision((int)x, Chunk.WIDTH);
		}
	}

	public class Chunk
	{
		public const int WIDTH = Terrain.SPACING * 64;

		public bool Generated;
		public long ID;

		public Task GenerationTask;

		public List<Entity> Trees = new List<Entity>();

		/*List<List<IntPoint>> _polygons;
		public List<List<IntPoint>> Polygons
		{
			get { return _polygons; }
			set
			{
				_polygons = value;
			}
		}*/

		public List<IntPoint> Polygon
		{
			get;
			set;
		}

		public List<List<IntPoint>> Cavities
		{
			get;
			set;
		}

		public VertexPositionColor[] TriangulatedVertices
		{
			get;
			set;
		}

		public VertexPositionColor[] TriangulatedWholeVertices
		{
			get;
			set;
		}

		public List<Fixture> Fixtures = new List<Fixture>();

		public long Left
		{
			get { return ID * WIDTH; }
		}

		public long Right
		{
			get { return Left + WIDTH; }
		}

		public Chunk(long id)
		{
			ID = id;
		}

		public static VertexPositionColor[] TriangulatedRenderData(List<Vertices> triangulated, Color c)
		{
			var output = new VertexPositionColor[triangulated.Count * 3];
			int index = 0;
			triangulated.ForEach(triangle => triangle.ForEach(vert =>
			                                                  output[index++] = new VertexPositionColor(new Vector3(vert.X, vert.Y, 0), c)));
			return output;
		}
	}
}
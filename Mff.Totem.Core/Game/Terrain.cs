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

namespace Mff.Totem.Core
{
	public class Terrain
	{
		public const int MAX_DEPTH = 10000, SPACING = 32, CHUNK_WIDTH = SPACING * 32;
		public const int BASE_HEIGHT = 0, BASE_STEP = 2048;
		public const float STEP_WIDTH = 8f * CHUNK_WIDTH;

		public uint Seed;

		public GameWorld World
		{
			get;
			private set;
		}

		public List<List<IntPoint>> Polygons, DamageMap;
		public List<Vertices> TriangulatedActiveArea;
		DualList<List<IntPoint>> Chunks;

		public Terrain(GameWorld world)
		{
			World = world;
			Polygons = new List<List<IntPoint>>();
			DamageMap = new List<List<IntPoint>>();
			Chunks = new DualList<List<IntPoint>>(512);
			c = new Clipper();
		}

		Clipper c;
		public void Generate(uint seed = 0)
		{
			Random = new Random();
			Seed = seed != 0 ? seed : (uint)Random.Next();
			Polygons.Clear();
			DamageMap.Clear();
			TerrainBody = BodyFactory.CreateBody(World.Physics, Vector2.Zero, 0, BodyType.Static, this);
			GenerateChunk(0);
			GenerateChunk(-1);
		}

		Task generationTask;
		public void Update()
		{
			if (generationTask != null && generationTask.IsCompleted)
			{
				generationTask.Dispose();
				generationTask = null;
			}
		}

		public void CreateDamage(List<IntPoint> polygon)
		{
			DamageMap.Add(polygon);
			c.Clear();
			c.AddPolygons(DamageMap, PolyType.ptClip);
			DamageMap.Clear();
			c.Execute(ClipType.ctXor, DamageMap, PolyFillType.pftPositive, PolyFillType.pftNonZero);
			CreateGenerationTask();
		}

		public void CreateDamage(params IntPoint[] points)
		{
			CreateDamage(points.ToList());
		}

		Body TerrainBody;
		public void PlaceInWorld(List<List<IntPoint>> polygons, bool clearMap = true)
		{
			lock (TerrainBody)
			{
				if (TerrainBody == null)
					TerrainBody = new Body(World.Physics, Vector2.Zero, 0, BodyType.Static, this);
				else if (clearMap)
				{
					while (TerrainBody.FixtureList.Count > 0)
					{
						for (int i = 0; i < TerrainBody.FixtureList.Count; ++i)
							TerrainBody.DestroyFixture(TerrainBody.FixtureList[i]);
						World.Physics.ProcessChanges();
					}
				}

				polygons.ForEach(r => FixtureFactory.AttachLoopShape(ConvertToVertices(r), TerrainBody, this));
			}
		}

		private void GenerateActiveRegion(int a, int b)
		{
			c.Clear();
			for (int i = Math.Max(a, Chunks.LowerBound); i < Math.Min(b, Chunks.UpperBound); ++i)
			{
				c.AddPolygon(Chunks[i], PolyType.ptSubject);
			}
			c.AddPolygons(DamageMap, PolyType.ptClip);
			List<List<IntPoint>> result = new List<List<IntPoint>>();
			c.Execute(ClipType.ctDifference, result, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
			PlaceInWorld(result);

			List<Vertices> verts = new List<Vertices>();
			result.ForEach(p =>
			{
				var triangulation = Triangulate.ConvexPartition(ConvertToVertices(p, false), TriangulationAlgorithm.Earclip);
				verts.AddRange(triangulation);
			});
			TriangulatedActiveArea = verts;
		}

		private int lastLeft = 0, lastRight = -1;
		public void GenerateChunk(int i)
		{
			for (int c = i >= 0 ? lastRight + 1 : lastLeft - 1; Math.Abs(c) <= Math.Abs(i); c += i >= 0 ? 1 : -1)
			{
				var chunk = CreateChunk(c * CHUNK_WIDTH, i >= 0);
				Chunks.Add(chunk, i >= 0);
				if (i >= 0)
					lastRight = c;
				else
					lastLeft = c;
			}
		}

		private Random Random;
		private List<IntPoint> CreateChunk(int x, bool fromLeft = true)
		{
			List<IntPoint> verts = new List<IntPoint>();

			// Add end faces
			verts.Add(fromLeft ? new IntPoint(x, MAX_DEPTH) : new IntPoint(x + CHUNK_WIDTH, MAX_DEPTH));
			verts.Add(fromLeft ? new IntPoint(x, (int)HeightMap(x)) : new IntPoint(x + CHUNK_WIDTH, (int)HeightMap(x+CHUNK_WIDTH)));

			for (int i = 1; i <= CHUNK_WIDTH / SPACING; ++i)
			{
				int x0 = fromLeft ? x + i * SPACING : x + CHUNK_WIDTH - (i) * SPACING;
				verts.Add(new IntPoint(x0, (int)HeightMap(x0)));
			}

			// Add second end face point
			verts.Add(fromLeft ? new IntPoint(x + CHUNK_WIDTH, MAX_DEPTH) : new IntPoint(x, MAX_DEPTH));
			return verts;
		}

		public void ClearFromWorld()
		{
			if (TerrainBody != null)
			{
				World.Physics.RemoveBody(TerrainBody);
				TerrainBody = null;
			}
		}

		public static Vertices ConvertToVertices(List<IntPoint> points, bool divide = true)
		{
			Vector2[] v = new Vector2[points.Count];
			int i = 0;
			if (divide)
				points.ForEach(p => v[i++] = new Vector2(p.X, p.Y) / 64f);
			else 
				points.ForEach(p => v[i++] = new Vector2(p.X, p.Y));
			return new Vertices(v);
		}

		int regionA = 0, regionB = 0;

		/// <summary>
		/// Sets the active region of terrain. Only chunks in this region will be displayed in world.
		/// </summary>
		/// <param name="a">Left bound.</param>
		/// <param name="b">Right bound.</param>
		public void SetActiveRegion(int a, int b)
		{
			if (a > b)
			{
				a = a ^ b;
				b = a ^ b;
				a = a ^ b;
			}

			int origA = regionA, origB = regionB;
			regionA = Helper.NegDivision(a, CHUNK_WIDTH);
			regionB = Helper.NegDivision(b, CHUNK_WIDTH) + 1;

			if (regionA != origA || regionB != origB)
			{
				CreateGenerationTask();
			}
		}

		void CreateGenerationTask()
		{
			if (generationTask != null)
			{
				generationTask.Wait();
				generationTask.Dispose();
			}
			generationTask = Task.Run(() => { GenerateActiveRegion(regionA, regionB); });
		}

		public float HeightMap(float x)
		{
			x /= STEP_WIDTH;
			int lower = (int)x - (x < 0 ? 1 : 0);
			uint hash = Helper.Hash(lower);

			double p0 = HashToDouble(Seed + Helper.Hash(lower -1)),
				p1 = HashToDouble(Seed + hash),
				p2 = HashToDouble(Seed + Helper.Hash(lower + 1)),
			p3 = HashToDouble(Seed + Helper.Hash(lower + 2));
			var r = MathHelper.CatmullRom((float)p0, (float)p1, (float)p2, (float)p3, x - lower) - 0.5f;
			return BASE_HEIGHT + (float)(r * BASE_STEP + HashToDouble(hash + Helper.Hash((int)(CHUNK_WIDTH*x))) * 4);
		}

		public double HashToDouble(uint hash)
		{
			return (hash / (double)uint.MaxValue) % 1;
		}

		class DualList<T>
		{
			private List<T> A, B;

			public DualList(int initialCapacity)
			{
				A = new List<T>(initialCapacity / 2 + 1);
				B = new List<T>(initialCapacity / 2);
			}

			public int UpperBound
			{
				get { return A.Count; }
			}

			public int LowerBound
			{
				get { return -B.Count; }
			}

			public void Add(T obj, bool positive)
			{
				if (positive)
					A.Add(obj);
				else
					B.Add(obj);
			}

			public T this[int index]
			{
				get
				{
					return index >= 0 ? A[index] : B[-(index + 1)];
				}
				set
				{
					if (index >= 0)
						A[index] = value;
					else
						B[-(index + 1)] = value;
				}
			}
		}
	}
}
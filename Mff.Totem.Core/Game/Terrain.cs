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

		public List<List<IntPoint>> Polygons, DamageMap;
		public List<Vertices> TriangulatedActiveArea;

		public Terrain(GameWorld world)
		{
			World = world;
			Polygons = new List<List<IntPoint>>();
			DamageMap = new List<List<IntPoint>>();
			c = new Clipper();
		}

		Clipper c;
		public void Generate(long seed = 0)
		{
			Random = new Random();
			Seed = seed != 0 ? seed : (long)Random.Next();
			Polygons.Clear();
			DamageMap.Clear();
			TerrainBody = BodyFactory.CreateBody(World.Physics, Vector2.Zero, 0, BodyType.Static, this);
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
			for (int i = a; i < b; ++i)
			{
				c.AddPolygons(CreateChunk(i), PolyType.ptSubject);
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

		private Random Random;
		private List<List<IntPoint>> CreateChunk(int x)
		{
			x *= CHUNK_WIDTH;
			List<List<IntPoint>> solution = new List<List<IntPoint>>();
			List<IntPoint> verts = new List<IntPoint>();
			List<IntPoint> cave = new List<IntPoint>();

			// Add end faces
			verts.Add(new IntPoint(x, MAX_DEPTH));

			for (int i = 0; i <= CHUNK_WIDTH / SPACING; ++i)
			{
				int x0 = x + i * SPACING, height = (int)HeightMap(x0);
				verts.Add(new IntPoint(x0, height));
				cave.Add(new IntPoint(x0, (int)((MAX_DEPTH - height) * NoiseMap.Evaluate(x0 / (4096f * 16f), 4096))));
			}

			for (int i = cave.Count - 1; i >= 0; --i)
			{
				cave.Add(new IntPoint(cave[i].X, cave[i].Y - 200));
			}

			// Add second end face point
			verts.Add(new IntPoint(x + CHUNK_WIDTH, MAX_DEPTH));

			var cl = new Clipper();
			cl.AddPolygon(verts, PolyType.ptSubject);
			cl.AddPolygon(cave, PolyType.ptClip);
			cl.Execute(ClipType.ctDifference, solution, PolyFillType.pftPositive, PolyFillType.pftNonZero);
			return solution;
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
			return BASE_HEIGHT + (float)((NoiseMap.Evaluate(x / (CHUNK_WIDTH * 8), 0)- 0.5) * BASE_STEP + 8 * NoiseMap.Evaluate(x / 128, CHUNK_WIDTH));
		}


	}
}
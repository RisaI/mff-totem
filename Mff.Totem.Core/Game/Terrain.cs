﻿using System;
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
		public const int MAX_DEPTH = 10000, SPACING = 32;
		public const int BASE_HEIGHT = 0, BASE_STEP = 2048;
		public const float STEP_WIDTH = 8f * Chunk.WIDTH;

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
		private List<List<IntPoint>> CreateChunkPoints(long x)
		{
			List<List<IntPoint>> solution = new List<List<IntPoint>>();
			List<IntPoint> verts = new List<IntPoint>();
			List<IntPoint> cave = new List<IntPoint>();

			// Add end faces
			verts.Add(new IntPoint(x, MAX_DEPTH));

			for (int i = 0; i <= Chunk.WIDTH / SPACING; ++i)
			{
				long x0 = x + i * SPACING, height = (long)HeightMap(x0);
				verts.Add(new IntPoint(x0, height));
				cave.Add(new IntPoint(x0, (int)((MAX_DEPTH - height) * NoiseMap.Evaluate(x0 / (4096f * 16f), 4096))));
			}

			for (int i = cave.Count - 1; i >= 0; --i)
			{
				cave.Add(new IntPoint(cave[i].X, cave[i].Y - 200));
			}

			// Add second end face point
			verts.Add(new IntPoint(x + Chunk.WIDTH, MAX_DEPTH));

			var cl = new Clipper();
			cl.AddPolygon(verts, PolyType.ptSubject);
			cl.AddPolygon(cave, PolyType.ptClip);
			cl.Execute(ClipType.ctDifference, solution, PolyFillType.pftPositive, PolyFillType.pftNonZero);
			return solution;
		}

		void GenerateChunk(Chunk ch)
		{
			if (ch.Generated)
				return;
			ch.Polygons = CreateChunkPoints(ch.Left);
			ch.Generated = true;
		}

		void PlaceChunk(Chunk ch)
		{
			if (!ch.Generated)
				return;

			lock (TerrainBody)
			{
				UnplaceChunk(ch);

				var cl = new Clipper();
				cl.Clear();
				cl.AddPolygons(ch.Polygons, PolyType.ptSubject);
				cl.AddPolygons(DamageMap, PolyType.ptClip);
				List<List<IntPoint>> result = new List<List<IntPoint>>();
				cl.Execute(ClipType.ctDifference, result, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

				ch.TriangulatedVertices.Clear();
				result.ForEach(polygon =>
				{
					Vertices toVerts = Terrain.ConvertToVertices(polygon, false);
					var triangulation = Triangulate.ConvexPartition(toVerts, TriangulationAlgorithm.Earclip);
					ch.TriangulatedVertices.AddRange(triangulation);
				});

				result.ForEach(polygon =>
				{
					var f = FixtureFactory.AttachLoopShape(ConvertToVertices(polygon), TerrainBody, this);
					ch.Fixtures.Add(f);
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

		public List<Chunk> ActiveChunks = new List<Chunk>();
		/// <summary>
		/// Sets the active region of terrain. Only chunks in this region will be displayed in world.
		/// </summary>
		/// <param name="a">Left bound.</param>
		/// <param name="b">Right bound.</param>
		public void SetActiveRegion(long a, long b)
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
					ActiveChunks.Add(ch = new Chunk(i));
					CreateGenerationTask(ch);
				}
			}
			ActiveChunks.ForEach(ch => {
				if (ch.ID < a || ch.ID >= b)
					UnplaceChunk(ch);
			});
			ActiveChunks.RemoveAll(ch => ch.ID < a || ch.ID >= b);
		}

		void CreateGenerationTask(Chunk ch)
		{
			if (ch.GenerationTask != null)
			{
				ch.GenerationTask.Wait();
				ch.GenerationTask.Dispose();
			}
			ch.GenerationTask = Task.Run(() => { GenerateChunk(ch); PlaceChunk(ch); });
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
		public const int WIDTH = Terrain.SPACING * 32;

		public bool Generated;
		public long ID;

		public Task GenerationTask;

		List<List<IntPoint>> _polygons;
		public List<List<IntPoint>> Polygons
		{
			get { return _polygons; }
			set
			{
				_polygons = value;
			}
		}

		public List<Vertices> TriangulatedVertices
		{
			get;
			private set;
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
			TriangulatedVertices = new List<Vertices>();
		}
	}
}
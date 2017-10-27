using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.Common;

using ClipperLib;

namespace Mff.Totem.Core
{
	public class Terrain
	{
		public const int MAX_DEPTH = 10000, SPACING = 32, VERTICAL_STEP = 5, CHUNK_WIDTH = SPACING * 32;

		public GameWorld World
		{
			get;
			private set;
		}

		public List<List<IntPoint>> Polygons, DamageMap;

		public Terrain(GameWorld world)
		{
			World = world;
			Polygons = new List<List<IntPoint>>();
			DamageMap = new List<List<IntPoint>>();
			c = new Clipper();
		}

		Clipper c;
		public void Generate()
		{
			Random = new Random();
			Polygons.Clear();
			DamageMap.Clear();
			TerrainBody = BodyFactory.CreateBody(World.Physics, this);
			GenerateChunk(0);
			GenerateChunk(-1);
		}

		public void CreateDamage(List<IntPoint> polygon)
		{
			DamageMap.Add(polygon);
			PlaceInWorld();
		}

		Body TerrainBody;
		public void PlaceInWorld()
		{
			ClearFromWorld();
			TerrainBody = new Body(World.Physics, Vector2.Zero, 0, this) { BodyType = BodyType.Static, };
			Polygons.ForEach(p =>
			{
				FixtureFactory.AttachLoopShape(ConvertToVertices(p), TerrainBody, this);
			});
		}

		private int lastLeft = 0, lastRight = -1, lastRightHeight = 500, lastLeftHeight = 500;
		public void GenerateChunk(int i)
		{
			bool refresh = false;
			if (i >= 0) // Generating from left to right
			{
				for (int c = i >= 0 ? lastRight + 1 : lastLeft - 1; c <= i; ++c)
				{
					CreateChunk(c * CHUNK_WIDTH, lastRightHeight, i >= 0);
					refresh = true;
					if (i >= 0)
						lastRight = c;
					else
						lastLeft = c;
				}
			}

			if (refresh) // Should the physics engine process changes?
				World.Physics.ProcessChanges();
		}

		private Random Random;
		private void CreateChunk(int x, int startY, bool fromLeft = true)
		{
			List<IntPoint> verts = new List<IntPoint>();

			// Add end faces
			verts.Add(fromLeft ? new IntPoint(x, MAX_DEPTH) : new IntPoint(x + CHUNK_WIDTH, MAX_DEPTH));
			verts.Add(fromLeft ? new IntPoint(x, startY) : new IntPoint(x + CHUNK_WIDTH, startY));

			int lastHeight = startY;
			for (int i = 1; i <= CHUNK_WIDTH / SPACING; ++i)
			{
				var height = lastHeight = lastHeight + Random.Next(-VERTICAL_STEP, VERTICAL_STEP);
				verts.Add(new IntPoint(fromLeft ? x + i * SPACING : x + CHUNK_WIDTH - (i) * SPACING, height));
			}

			// Save last generated height
			if (fromLeft)
				lastRightHeight = lastHeight;
			else
				lastLeftHeight = lastHeight;

			// Add second end face point
			verts.Add(fromLeft ? new IntPoint(x + CHUNK_WIDTH, MAX_DEPTH) : new IntPoint(x, MAX_DEPTH));

			Polygons.Add(verts);
			FixtureFactory.AttachLoopShape(ConvertToVertices(verts), TerrainBody, this);
		}

		public void ClearFromWorld()
		{
			if (TerrainBody != null)
			{
				World.Physics.RemoveBody(TerrainBody);
				TerrainBody = null;
			}
		}

		public static Vertices ConvertToVertices(List<IntPoint> points)
		{
			Vector2[] v = new Vector2[points.Count];
			int i = 0;
			points.ForEach(p => v[i++] = new Vector2(p.X, p.Y) / 64f);
			return new Vertices(v);
		}
	}
}
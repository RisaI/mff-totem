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
		const int POINTS = 100;
		const int SPACING = 45, VERTICAL_STEP = 20;

		public GameWorld World
		{
			get;
			private set;
		}

		public List<List<IntPoint>> Polygons;

		public Terrain(GameWorld world)
		{
			World = world;
			Polygons = new List<List<IntPoint>>();
			c = new Clipper();
		}

		Clipper c;
		public void Generate()
		{
			Polygons.Clear();
			c.Clear();

			int[] heights = new int[POINTS];
			for (int i = 1; i < heights.Length; ++i)
			{
				// float weight = -((2*i - POINTS) * (-POINTS + 2*i)) / ((float)POINTS * POINTS * 2f) + 0.5f;
				float weight = 1f - (2 * i / (float)POINTS);
				heights[i] = heights[i - 1] + (int)(((float)TotemGame.Random.NextDouble() - 0.5f - weight * 0.2f) * VERTICAL_STEP);
			}

			for (int i = 1; i < heights.Length - 1; ++i)
			{
				heights[i] = (heights[i - 1] + heights[i + 1]) / 2;
			}

			List<IntPoint> MainLand = new List<IntPoint>();
			MainLand.Add(new IntPoint(0,2000));
			for (int i = 0; i < heights.Length; ++i)
			{
				MainLand.Add(new IntPoint(i * SPACING, 500 + heights[i]));
			}
			MainLand.Add(new IntPoint((heights.Length - 1)*SPACING, 2000));

			c.AddPolygon(MainLand, PolyType.ptSubject);
			//c.AddPolygon(new List<IntPoint>() { new IntPoint(200,0), new IntPoint(250, 0), new IntPoint(250,1990), new IntPoint(200,2000) }, PolyType.ptClip);
			c.Execute(ClipType.ctDifference, Polygons);
		}

		public void DiffPolygons(List<IntPoint> polygon)
		{
			c.Clear();
			c.AddPolygons(Polygons, PolyType.ptSubject);
			c.AddPolygon(polygon, PolyType.ptClip);
			c.Execute(ClipType.ctDifference, Polygons);
			PlaceInWorld();
		}

		Body TerrainBody;
		public void PlaceInWorld()
		{
			ClearFromWorld();
				TerrainBody = new Body(World.Physics, Vector2.Zero, 0, this) { BodyType = BodyType.Static, };

			Polygons.ForEach(p =>
			{
				Vertices verts = new Vertices();
				p.ForEach(point => verts.Add(new Vector2(point.X, point.Y) / 64f));
				FixtureFactory.AttachLoopShape(verts, TerrainBody, this);
			});
		}

		public void ClearFromWorld()
		{
			if (TerrainBody != null)
			{
				World.Physics.RemoveBody(TerrainBody);
				TerrainBody = null;
			}
		}
	}
}

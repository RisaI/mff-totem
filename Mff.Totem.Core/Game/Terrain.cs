using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using FarseerPhysics.Common;

namespace Mff.Totem.Core
{
	public class Terrain
	{
		public GameWorld World
		{
			get;
			private set;
		}

		public List<Vector2> Vertices;


		public Terrain(GameWorld world)
		{
			Vertices = new List<Vector2>();
			World = world;
		}

		public void Generate()
		{
			Vertices.Clear();
			Vertices.Add(new Vector2(0,2000));
			for (int i = 0; i < 100; ++i)
			{
				Vertices.Add(new Vector2(i * 50, 500 + ((float)TotemGame.Random.NextDouble() - 0.5f) * 40));
			}
			Vertices.Add(new Vector2(99*50, 2000));
		}

		public void PlaceInWorld()
		{
			var verts = new Vertices(Vertices);
			verts.Scale(Vector2.One / 64f);
			var edge = BodyFactory.CreateLoopShape(World.Physics, verts, this);
		}
	}
}

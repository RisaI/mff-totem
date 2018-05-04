using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Penumbra;

namespace Mff.Totem.Core
{
	public class LightingManager
	{
		public List<Light> Lights = new List<Light>();
		public HullList Hulls = new HullList();

		public TotemGame Game
		{
			get;
			private set;
		}

		public LightingManager(TotemGame game)
		{
			Game = game;
		}

		public void BeginDraw(Color ambient, Matrix matrix)
		{
			Game.Lighting.Lights = Lights; 
			Game.Lighting.Hulls = Hulls;

			Game.Lighting.AmbientColor = ambient;
			Game.Lighting.Transform = matrix;

			Game.Lighting.BeginDraw();
		}

		public void Draw()
		{
			Game.Lighting.Draw();
		}
	}
}

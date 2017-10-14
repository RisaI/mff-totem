using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Mff.Totem.Core
{
	public abstract class TotemGame : Game
	{
		public static string ProjectName
		{
			get { return "mff-totem"; } 
		}

		public static string Version
		{
			get { return "dev"; }
		}

        GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		public TotemGame()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
		}
	}
}

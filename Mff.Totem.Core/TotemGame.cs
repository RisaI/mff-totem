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

        protected GraphicsDeviceManager graphics;
		protected SpriteBatch spriteBatch;
		protected DeveloperConsole Console;

		/// <summary>
		/// Returns screen size as a Vector2
		/// </summary>
		/// <value>The resolution.</value>
		public Vector2 Resolution
		{
			get { return new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight); }
		}

		public TotemGame()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			Console = new DeveloperConsole(this);
		}

		protected override void LoadContent()
		{
			base.LoadContent();
			spriteBatch = new SpriteBatch(GraphicsDevice);

			// Load textues and fonts
			ContentLoader.Load(this);
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			Input.Update();
			Console.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);
			GraphicsDevice.Clear(Color.White);

			if (Console.Enabled)
				Console.Draw(spriteBatch);
		}
	}
}

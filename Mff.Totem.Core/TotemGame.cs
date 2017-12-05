using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Krypton;

namespace Mff.Totem.Core
{
    public abstract class TotemGame : Game
    {
        static TotemGame()
        {
            DeserializationRegister.ScanAssembly(typeof(TotemGame).Assembly);
            Random = new Random();
        }

        public static Random Random
        {
            get;
            private set;
        }

        public static string ProjectName
        {
            get { return "mff-totem"; }
        }

        public static string Version
        {
            get { return "dev"; }
        }

        protected GraphicsDeviceManager graphics;
		public GraphicsDeviceManager GraphicsManager
		{
			get { return graphics; }
		}

		public Input Input
		{
			get;
			protected set;
		}

		public Gui.GuiManager GuiManager
		{
			get;
			private set;
		}

		public KryptonEngine Krypton
        {
            get;
            private set;
        }

        protected SpriteBatch spriteBatch;
        protected DeveloperConsole Console;

		public event Action<int, int> OnResolutionChange;

        /// <summary>
        /// Returns screen size as a Vector2
        /// </summary>
        /// <value>The resolution.</value>
        public Vector2 Resolution
        {
            get { return new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight); }
        }

        public GameWorld World
        {
            get;
            private set;
        }

        public bool InputEnabled
        {
            get { return !Console.Enabled; }
        }

		public TotemGame()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			Console = new DeveloperConsole(this);
			GuiManager = new Gui.GuiManager(this);
			Input = new DesktopInput(this);
			Krypton = new KryptonEngine(this, "shaders/lighting");
			graphics.GraphicsProfile = GraphicsProfile.Reach;
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			Window.ClientSizeChanged += (sender, e) =>
			{
				graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
				graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
				if (OnResolutionChange != null)
					OnResolutionChange(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
			};
			Krypton.Initialize();
            base.Initialize();
		}

		protected override void LoadContent()
		{
			base.LoadContent();
			spriteBatch = new SpriteBatch(GraphicsDevice);

			// Load textues and fonts
			Krypton.Initialize();
			ContentLoader.Load(this);

			World = new GameWorld(this);
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			Input.Update(gameTime);

			Krypton.Update(gameTime);
            if (World != null)
				World.Update(gameTime);

			GuiManager.Update(gameTime);

			Console.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{

            if (World != null)
            {
                World.Draw(spriteBatch);
            }
            else
                GraphicsDevice.Clear(Color.Black);

			GuiManager.Draw(spriteBatch);

			if (Console.Enabled)
				Console.Draw(spriteBatch);
        }
	}
}

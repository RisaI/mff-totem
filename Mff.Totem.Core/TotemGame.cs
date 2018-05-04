﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Penumbra;
using System.Collections.Generic;
using System.IO;

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

		public HUD Hud
		{
			get;
			private set;
		}

		public Penumbra.Penumbra Lighting
        {
            get;
            private set;
        }

        protected SpriteBatch spriteBatch;
        protected DeveloperConsole Console;

		private GameStateEnum _gameState = GameStateEnum.Game;
		public GameStateEnum GameState
		{
			get { return _gameState; }
			set {
				if (value == _gameState)
					return;

				_gameState = value;

				switch (value)
				{
					case GameStateEnum.Game:
						// Loading stuff
						break;
					case GameStateEnum.Menu:
						MenuState.CurrentState = "main";
						break;
				}
			}
		}

		public MenuStateManager MenuState
		{
			get;
			private set;
		}

		public event Action<int, int> OnResolutionChange;

        /// <summary>
        /// Returns screen size as a Vector2
        /// </summary>
        /// <value>The resolution.</value>
        public Vector2 Resolution
        {
            get { return new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight); }
        }

        public GameSession Session
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
			Hud = new HUD(this);
			Lighting = new Penumbra.Penumbra(this) { Debug = false, SpriteBatchTransformEnabled = true };
			IsMouseVisible = true;
			MenuState = new MenuStateManager(this);
		}

		protected override void Initialize()
		{
			Window.ClientSizeChanged += (sender, e) =>
			{
				graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
				graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
				if (OnResolutionChange != null)
					OnResolutionChange(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
				ContentLoader.RefreshShaders(this);

			};
			base.Initialize();
		}

		protected override void LoadContent()
		{
			base.LoadContent();
			spriteBatch = new SpriteBatch(GraphicsDevice);

			// Load textues and fonts
			ContentLoader.Load(this);
			ContentLoader.RefreshShaders(this);
			Lighting.Initialize();

			LoadNewGame();
		}

		public void SaveSession(string filename)
		{
			if (Session == null)
				return;
			
			using (FileStream file = new FileStream(filename, FileMode.Create))
			using (BinaryWriter writer = new BinaryWriter(file))
			{
				Session.Serialize(writer);
			}
		}

		public bool LoadSession(string filename)
		{
			if (!File.Exists(filename))
				return false;

			using (FileStream file = new FileStream(filename, FileMode.Open))
			{
				Session = GameSession.LoadGame(this, file);
			}
			return true;
		}

		public void LoadNewGame()
		{
			Session = GameSession.CreateNewGame(this);
			GameState = GameStateEnum.Game;
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			Input.Update(gameTime);


			switch (_gameState)
			{
				case GameStateEnum.Game:
					if (Input.GetInput(Inputs.QuickLoad, InputState.Pressed))
					{
						if (LoadSession("quicksave.sav"))
						{
							Hud.Chat("Loading from quick save");
						}
					}
					else if (Input.GetInput(Inputs.QuickSave, InputState.Pressed))
					{
						SaveSession("quicksave.sav");
						Hud.Chat("Quick saving...");
					}

					if (Session != null)
						Session.Update(gameTime);

					Hud.Update(gameTime);


					if (Input.GetInput(Inputs.Pause, InputState.Pressed))
					{
						var g = GuiManager.GetGuiOfType<Gui.MiniMenu>();
						if (g == null)
							GuiManager.Add(new Gui.MiniMenu());
						else
							g.Closing = true;
					}

					GuiManager.Update(gameTime);
					break;
				case GameStateEnum.Menu:
					MenuState.CurrentMenu.Update(gameTime);
					ContentLoader.Shaders["menu"].Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
					break;
			}

			Console.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{

			switch (_gameState)
			{
				case GameStateEnum.Game:
					if (Session != null)
						Session.Draw(spriteBatch);
					else
						GraphicsDevice.Clear(Color.Black);

					Hud.Draw(spriteBatch);
					GuiManager.Draw(spriteBatch);
					break;
				case GameStateEnum.Menu:
					GraphicsDevice.Clear(Color.Black);
					foreach (EffectPass pass in ContentLoader.Shaders["menu"].Techniques[0].Passes)
					{
						pass.Apply();
						GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, new VertexPositionColor[]
						{ new VertexPositionColor(new Vector3(-1, 1,0), Color.White),
						  new VertexPositionColor(new Vector3(1, 1,0), Color.White),
						  new VertexPositionColor(new Vector3(-1,-1,0), Color.White),
						  new VertexPositionColor(new Vector3(-1,-1,0), Color.White),
						  new VertexPositionColor(new Vector3(1,1,0), Color.White),
						  new VertexPositionColor(new Vector3(1,-1,0), Color.White)}, 0, 2);
					}
					MenuState.CurrentMenu.Draw(spriteBatch);
					break;
			}

			if (Console.Enabled)
				Console.Draw(spriteBatch);
        }

		public class MenuStateManager
		{
			string _state = "main";
			public string CurrentState
			{
				get { return _state; }
				set
				{
					if (Menus.ContainsKey(value))
						_state = value;
				}
			}

			public Menu CurrentMenu
			{
				get { return Menus.ContainsKey(CurrentState) ? Menus[CurrentState] : null; }
			}

			public Dictionary<string, Menu> Menus;

			public MenuStateManager(TotemGame game)
			{
				Menus = new Dictionary<string, Menu>()
				{
					{ "main", new Menu(game) }
				};

				// Building main menu
				Menus["main"].Controls.Add(new MenuButton(game) { OnClick = () => { game.LoadNewGame(); }, Text = "New Game" });
				Menus["main"].Controls.Add(new MenuButton(game) { OnClick = () => { game.Exit(); }, Text = "Exit", Position = new Vector2(0, 64) });
			}
		}
	}

	public enum GameStateEnum
	{
		Menu,
		Game
	}
}

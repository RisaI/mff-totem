using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriterDotNet.MonoGame;

namespace Mff.Totem.Core
{
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

		public TotemGame Game
		{
			get;
			private set;
		}

		public MenuStateManager(TotemGame game)
		{
			Game = game;
			Menus = new Dictionary<string, Menu>()
				{
					{ "main", new Menu(game) }
				};

			// Building main menu
			Menus["main"].Controls.Add(new MenuButton(game)
			{
				OnClick = () => { game.LoadNewGame(); },
				Text = "New Game",
				Position = new ResolutionResolver(game, new Vector2(0, .9f), new Vector2(12, -64 * 2))
			});
			Menus["main"].Controls.Add(new MenuButton(game)
			{
				OnClick = () => { game.Exit(); },
				Text = "Exit",
				Position = new ResolutionResolver(game, new Vector2(0, .9f), new Vector2(12, -64))
			});
		}

		MonoGameAnimator TotemAnimator;
		IResolver<Vector2> TotemPositionResolver;
		IResolver<float> TotemScaleResolver;
		public void Initialize()
		{
			CurrentState = "main";
			if (TotemAnimator == null)
			{
				TotemAnimator = ContentLoader.BoneSprites["Totem"].GetAnimator();
				TotemAnimator.Speed = 0.125f;
				TotemPositionResolver = new ResolutionResolver(Game, new Vector2(0.5f, 1), new Vector2(0, -24));
				TotemScaleResolver = new YResScaleResolver(Game, 560f);
			}
		}

		public void Update(GameTime gameTime)
		{
			CurrentMenu.Update(gameTime);
			ContentLoader.Shaders["menu"].Parameters["Time"].SetValue((float)gameTime.TotalGameTime.TotalSeconds);
			TotemAnimator.Update((float)gameTime.ElapsedGameTime.TotalMilliseconds);
			TotemAnimator.Position = TotemPositionResolver.Resolve();
			TotemAnimator.Scale = new Vector2(TotemScaleResolver.Resolve());
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			Game.GraphicsDevice.Clear(Color.Black);
			spriteBatch.Begin();
			TotemAnimator.Draw(spriteBatch);
			spriteBatch.End();
			CurrentMenu.Draw(spriteBatch);
		}

		class YResScaleResolver : IResolver<float>
		{
			TotemGame Game;
			float ItemScale;
			public YResScaleResolver(TotemGame game, float itemScale)
			{
				Game = game;
				ItemScale = itemScale;
			}

			public float Resolve()
			{
				return Game.Resolution.Y / ItemScale;
			}
		}
	}
}

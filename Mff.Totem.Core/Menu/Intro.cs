using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public static class Intro
	{
		const float TOTAL_TIME = 6,
					TRANSITION_TIME = 1;

		static float Time = 0;
		static TotemGame Game;
		static IResolver<Vector2> NamePosition;
		public static void Initialize(TotemGame game)
		{
			Time = 0;
			Game = game;
			NamePosition = new ResolutionResolver(game, new Vector2(.5f), Vector2.Zero);
			if (Stars == null)
			{
				System.Threading.Tasks.Task.Run(() =>
				{
					GenerateTexture();
				});
			}
		}

		public static void Update(GameTime gameTime)
		{
			Time += (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (Time >= TOTAL_TIME || Game.Input.GetKeyState(Microsoft.Xna.Framework.Input.Keys.Space) == InputState.Pressed)
				Game.GameState = GameStateEnum.Menu;
		}

		public static Texture2D Stars;
		public static void Draw(SpriteBatch spriteBatch)
		{
			Game.GraphicsDevice.Clear(Color.Black);

			var font = ContentLoader.Fonts["intro"];
			float opacity = Math.Min(1, (-Math.Abs(Time - TOTAL_TIME / 2) + TOTAL_TIME / 2) / TRANSITION_TIME);
			spriteBatch.Begin();

			if (Stars != null)
			{
				SpriteEffects[] effects = { SpriteEffects.None, SpriteEffects.FlipVertically, SpriteEffects.FlipHorizontally };
				int width = ((int)Game.Resolution.X) / Stars.Width + 1,
					height = ((int)Game.Resolution.Y) / Stars.Height + 1;
				for (int x = 0; x < width; ++x)
				{
					for (int y = 0; y < height; ++y)
					{
						spriteBatch.Draw(Stars, new Vector2(x, y) * Stars.Width, null, Color.White, 0, Vector2.Zero, Vector2.One, effects[(x + y) % 3], 0.01f);
					}
				}
			}

			spriteBatch.DrawString(font,
								   Constants.ProductionName,
								   NamePosition.Resolve(),
								   Color.Lerp(Color.Transparent, Color.White, opacity),
								   0f,
								   font.MeasureString(Constants.ProductionName) / 2,
								   1,
			                       SpriteEffects.None,
								   1f
			                      );
			spriteBatch.End();
		}

		static void GenerateTexture()
		{
			int size = 256;
			var sky = new Texture2D(Game.GraphicsDevice, size, size);
			Color[] colorMap = new Color[size * size];
			for (int i = 0; i < size * size; ++i)
			{
				if (TotemGame.Random.Next(280) != 0)
					continue;

				float intensity = 0.3f + 0.7f * (float)TotemGame.Random.NextDouble();
				Color main = Color.Lerp(Color.Transparent, Color.White, intensity),
					secondary = Color.Lerp(main, Color.Transparent, 0.75f);

				colorMap[i] = main;
				if (i >= size)
					colorMap[i - size] = secondary;
				if (i < size * (size - 1))
					colorMap[i + size] = secondary;
				int mod = i % size;
				if (mod > 0)
					colorMap[i - 1] = secondary;
				if (mod < size - 1)
					colorMap[i + 1] = secondary;
			}
			sky.SetData<Color>(colorMap);
			Stars = sky;
		}
	}
}

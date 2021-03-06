﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public abstract class Background
	{
		public BackgroundComponent Parent
		{
			get;
			private set;
		}

		public GameWorld World
		{
			get { return Parent.World; }
		}

		public Color ClearColor
		{
			get;
			protected set;
		}

		public Background(Color clearColor)
		{
			ClearColor = clearColor;
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			World.Game.GraphicsDevice.Clear(ClearColor);
			OnDraw(spriteBatch);
		}

		public Background Attach(BackgroundComponent component)
		{
			Parent = component;
			return this;
		}

		public abstract void Update(GameTime gameTime);
		protected abstract void OnDraw(SpriteBatch spriteBatch);
	}

	namespace Backgrounds
	{
		public class OutsideBG : Background
		{
			public Color SkyColor = Color.LightSkyBlue;
			Color SkyTintColor;
			float SkyTint, MovableOffset = 0;

			public OutsideBG() : base(Color.LightSkyBlue)
			{
				Parallax = ContentLoader.Parallaxes["standard"];
			}

			Vector2 Resolution
			{
				get { return World.Game.Resolution; }
			}

			public override void Update(GameTime gameTime)
			{
				// SkyTintColor = Color.Lerp(SkyTintColor, World.Weather.SkyTintColor, 0.05f);
				// SkyTint = MathHelper.Lerp(SkyTint, World.Weather.SkyTint, 0.07f);
				ClearColor = Color.Lerp(Color.Black, SkyColor, 1f - World.NightTint(World.Session.UniverseTime.TimeOfDay.TotalHours));
				MovableOffset += (float)gameTime.ElapsedGameTime.TotalSeconds * World.TimeScale / 60f;
				MovableOffset = MovableOffset % 1f;
			}

			protected override void OnDraw(SpriteBatch spriteBatch)
			{
				var matrix = //Matrix.CreateTranslation(-World.Game.Resolution.X / 2, -World.Game.Resolution.Y / 2, 0) *
								   Matrix.CreateRotationZ(World.Camera.Rotation) *
				                   Matrix.CreateTranslation(World.Game.Resolution.X / 2, World.Game.Resolution.Y / 2, 0);

				spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, matrix);
				double hour = World.Session.UniverseTime.TimeOfDay.TotalHours;
				float nightTint = World.NightTint(hour);

				//Background color
				//spriteBatch.Draw(ContentLoader.Pixel, Vector2.Zero, null, ClearColor, 0, Vector2.Zero, World.Game.Resolution, SpriteEffects.None, 0f);
				{
					if (nightTint > 0.1f && World?.Planet?.NightSky != null)
					{
						Color stars = Color.Lerp(Color.Transparent, Color.White, nightTint);
						Texture2D starTexture = World?.Planet?.NightSky;
						int width = ((int)World.Game.Resolution.X) / starTexture.Width + 1,
							height = ((int)World.Game.Resolution.Y) / starTexture.Height + 1;
						for (int x = 0; x < width; ++x)
						{
							for (int y = 0; y < height; ++y)
							{
								spriteBatch.Draw(starTexture, new Vector2(x, y) * 256 - Resolution / 2, null, stars, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.01f);
							}
						}
						/*if (World?.Planet?.NightSkyNebulae != null)
							spriteBatch.Draw(World.Planet.NightSkyNebulae, -Resolution / 2, null, stars,
						                 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0.01f);*/
					}
				}

				//Sun and moon
				if (hour > 4 && hour < 20)
				{
					float angle = MathHelper.PiOver2 - (float)(hour - 12) / 16 * MathHelper.Pi;
					Texture2D sunTexture = ContentLoader.Textures["sun"];
					spriteBatch.Draw(sunTexture, new Vector2(1.3f, 1) * Helper.AngleToDirection(angle) * World.Game.Resolution / 2, null,
									 Color.White, 0, sunTexture.Size() / 2, Vector2.One, SpriteEffects.None, 0f);
				}
				if (hour > 16 || hour < 8)
				{
					hour -= 12;
					if (hour < 0)
						hour += 24;
					float angle_moon = MathHelper.PiOver2 - (float)(hour - 12) / 16 * MathHelper.Pi;
					Texture2D moonTexture = ContentLoader.Textures["moon"];
					spriteBatch.Draw(moonTexture, new Vector2(1.3f, 1) * Helper.AngleToDirection(angle_moon) * World.Game.Resolution / 2, null,
									 Color.White, 0, moonTexture.Size() / 2, Vector2.One * 1f, SpriteEffects.None, 0f);
				}
				spriteBatch.Draw(ContentLoader.Pixel, Vector2.Zero, null, Color.Lerp(Color.Transparent, SkyTintColor, SkyTint), 0, Vector2.Zero, World.Game.Resolution, SpriteEffects.None, 0.1f);
				DrawParallax(spriteBatch, nightTint);
				spriteBatch.End();
			}

			public Parallax Parallax
			{
				get;
				set;
			}

            protected void DrawParallax(SpriteBatch spriteBatch, float nightTint)
            {
                if (Parallax == null)
                    return;

                Color clr = Color.Lerp(Color.White, new Color(25, 25, 25, 255), nightTint);
				for (int i = 0; i < Parallax.Textures.Length; ++i)
                {
					var texture = Parallax.Textures[i];
                    float scale = World.Game.Resolution.Y / texture.Height;
                    float width = scale * texture.Width;
					int count = Math.Max(3, (int)(Math.Sqrt(2) * World.Game.Resolution.X / width) + 1);

					float offsetX = Helper.NegModulo((int)(World.Camera.Position.X / (float)Math.Pow(2, 2 + i) + (Parallax.Offsetable[i] ? MovableOffset * width : 0)), (int)width),
                        offsetY = -((World.Camera.Position.Y) / (float)Math.Pow(2, 9 + i));

                    for (int c = 0; c < count; ++c)
                    {
                        spriteBatch.Draw(texture, new Vector2(-offsetX + c * width, Math.Max(0, offsetY)) - Resolution / 2, null, clr, 0, Vector2.Zero, new Vector2(scale), SpriteEffects.None, 0.5f - 0.01f * i);
                        //spriteBatch.Draw(texture, new Vector2(-offsetX + c * width, Math.Max(0, offsetY) + texture.Height * scale) - Resolution / 2, null, clr, 0, Vector2.Zero, new Vector2(scale), SpriteEffects.FlipVertically, 0.5f - 0.01f * i);
                    }
                }
            }
		}
	}
}

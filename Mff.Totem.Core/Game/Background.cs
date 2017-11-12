using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public abstract class Background
	{
		public GameWorld World
		{
			get;
			private set;
		}

		public Color ClearColor
		{
			get;
			protected set;
		}

		public Background(GameWorld world, Color clearColor)
		{
			World = world;
			ClearColor = clearColor;
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			World.Game.GraphicsDevice.Clear(ClearColor);
			OnDraw(spriteBatch);
		}


		public abstract void Update(GameTime gameTime);
		protected abstract void OnDraw(SpriteBatch spriteBatch);
	}

	namespace Backgrounds
	{
		public class BlankOutsideBG : Background
		{
            Color skyTint;

			public BlankOutsideBG(GameWorld world) : base(world, Color.LightSkyBlue)
			{

			}

			public override void Update(GameTime gameTime)
			{
				skyTint = Color.Lerp(skyTint, Color.LightSkyBlue, 0.05f);
				ClearColor = Color.Lerp(Color.Black, Color.Lerp(Color.LightSkyBlue, skyTint, 0.6f), 1f - NightTint(World.WorldTime.TimeOfDay.TotalHours));
			}

			protected override void OnDraw(SpriteBatch spriteBatch)
			{
                spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
				double hour = World.WorldTime.TimeOfDay.TotalHours;
				float nightTint = NightTint(World.WorldTime.TimeOfDay.TotalHours);

				//Sun and moon
				if (hour > 4 && hour < 20)
				{
					float angle = MathHelper.PiOver2 - (float)(hour - 12) / 16 * MathHelper.Pi;
					Texture2D sunTexture = ContentLoader.Pixel; // TODO: add a texture for the sun
					spriteBatch.Draw(sunTexture, World.Game.Resolution / 2 + new Vector2(1.3f, 1) * Helper.AngleToDirection(angle) * World.Game.Resolution / 2, null,
					                 Color.Yellow, 0, sunTexture.Size() / 2, Vector2.One * 50, SpriteEffects.None, 0f);
				}
				if (hour > 16 || hour < 8)
				{
					hour -= 12;
					if (hour < 0)
						hour += 24;
					float angle_moon = MathHelper.PiOver2 - (float)(hour - 12) / 16 * MathHelper.Pi;
					Texture2D moonTexture = ContentLoader.Pixel; // TODO: add a texture for the moon
					spriteBatch.Draw(moonTexture, World.Game.Resolution / 2 + new Vector2(1.3f, 1) * Helper.AngleToDirection(angle_moon) * World.Game.Resolution / 2, null,
					                 Color.White, 0, moonTexture.Size() / 2, Vector2.One * 50, SpriteEffects.None, 0f);
				}
				spriteBatch.End();
			}

			private float NightTint(double hour)
			{
				return hour <= 4 || hour >= 20 ? 1 : (float)(hour > 8 && hour < 16 ? 0 : 1f - Math.Pow(Math.Cos(Math.PI * (hour / 8 - 1)), 2));
			}
		}
	}
}

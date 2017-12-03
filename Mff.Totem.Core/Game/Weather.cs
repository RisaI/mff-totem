using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public class Weather : ICloneable<Weather>
	{
		public virtual float SkyTint
		{
			get;
			set;
		}

		public virtual Color SkyTintColor
		{
			get;
			set;
		}

		public static Weather DefaultWeather
		{
			get { return new Weather() { SkyTint = 0f, SkyTintColor = Color.White }; }
		}

		public Weather()
		{

		}

		public virtual void Update(GameWorld world, GameTime gameTime) { }
		public virtual void DrawWeatherEffects(GameWorld world, SpriteBatch spriteBatch) { }

		public virtual Weather Clone()
		{
			return new Weather();
		}
	}

	public class RainWeather : Weather
	{
		const int X_DELTA = 12, Y_DELTA = 480;

		public RainWeather()
		{
			SkyTint = 0.5f;
			SkyTintColor = Color.LightGray;
		}

		float Time;
		public override void Update(GameWorld world, GameTime gameTime)
		{
			base.Update(world, gameTime);
			Time += (float)gameTime.ElapsedGameTime.TotalSeconds * world.TimeScale;
		}

		public override void DrawWeatherEffects(GameWorld world, SpriteBatch spriteBatch)
		{
			int x = (int)world.Camera.BoundingBox.Left;
			x -= Helper.NegModulo(x, X_DELTA);
			for (int dx = 0; x + dx < world.Camera.BoundingBox.Right; dx += X_DELTA)
			{
				float height = world.Terrain.HeightMap(x + dx);
				float offset = Y_DELTA * ( 1 - ((Time + (float)Math.Sin(Helper.Hash(x + dx))) % 1f));

				for (int i = 0; i < Math.Max(height - world.Camera.BoundingBox.Top, 0) / Y_DELTA; ++i)
				{
					spriteBatch.Draw(ContentLoader.Pixel, new Vector2(x + dx, height - offset - i * Y_DELTA),
									 null, Color.Blue, 0, new Vector2(0.5f, 1f), new Vector2(1, 8f), SpriteEffects.None, 0f);
				}
			}
		}
	}
}

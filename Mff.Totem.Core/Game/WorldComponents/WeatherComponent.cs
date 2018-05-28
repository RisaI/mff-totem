using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	[Serializable("weather_wcomponent")]
	public class WeatherComponent : WorldComponent, IUpdatable, IDrawable
	{
		public Weather CurrentWeather;

		public WeatherComponent()
		{
		}

		public override void Deserialize(BinaryReader reader)
		{
			
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (CurrentWeather != null)
			{
				spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, 
				                  World.Camera != null ? World.Camera.ViewMatrix : Matrix.Identity);
				CurrentWeather.DrawWeatherEffects(World, spriteBatch);
				spriteBatch.End();
			}
		}

		public override void Initialize()
		{
			
		}

		public override void Serialize(BinaryWriter writer)
		{
			
		}

		public override void SetActiveArea(Vector2 pos)
		{
			
		}

		public void Update(GameTime gameTime)
		{
			if (World.DrawLayer != 10)
				return;

			if (CurrentWeather != null)
				CurrentWeather.Update(World, gameTime);
		}
	}
}

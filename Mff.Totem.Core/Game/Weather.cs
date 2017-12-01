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

		public virtual void Update(GameTime gameTime) { }
		public virtual void DrawWeatherEffects(SpriteBatch spriteBatch) { }

		public virtual Weather Clone()
		{
			return new Weather();
		}
	}
}

using System;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public class PlanetInfo
	{
		public int TerrainSeed;
		public float Gravity = 9.81f;
		public Color SkyColor = Color.LightSkyBlue, 
					 SoilTint = Color.White;
		public TimeSpan DayLength = new TimeSpan(24,0,0);

		public Color BackgroundSoilTint
		{
			get { return Color.Lerp(SkyColor, Color.Black, 0.5f); }
		}

		public PlanetInfo()
		{
			
		}

		int Seed = 0;
		public void Randomize(int seed)
		{
			Seed = seed;
			var random = new Random(seed);

			TerrainSeed = random.Next() * Math.Sign(random.NextDouble() - 0.5);
			Gravity = (float)random.NextDouble() * 4f + 7.5f;
			DayLength = new TimeSpan(12 + (int)(random.NextDouble() * 30), (int)(random.NextDouble() * 60), (int)(random.NextDouble() * 60));
			SoilTint = MultiLerp((float)random.NextDouble(), Color.White, Color.Purple, Color.Green, Color.BlanchedAlmond);
			SkyColor = MultiLerp((float)random.NextDouble(), Color.LightSkyBlue, Color.Red);
		}

		public static Color MultiLerp(float a, params Color[] colors)
		{
			if (colors.Length <= 1)
				return colors[0];
			
			float step = 1f / (colors.Length - 1);
			return Color.Lerp(colors[(int)Math.Floor(a / step)], colors[(int)Math.Floor(a / step + 1)], (a % step) / step);
		}
	}
}

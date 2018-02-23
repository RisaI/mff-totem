using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

		public Texture2D NightSky
		{
			get;
			private set;
		}

		public Texture2D NightSkyNebulae
		{
			get;
			private set;
		}

		public void GenerateTextures(GameWorld world)
		{
			var random = new Random(Seed);
			var noise = new OpenSimplexNoise(Seed);

			{
				int size = 256;
				var sky = new Texture2D(world.Game.GraphicsDevice, size, size);
				Color[] colorMap = new Color[size * size];
				for (int i = 0; i < size * size; ++i)
				{
					if (random.Next(280) != 0)
						continue;

					float intensity = 0.3f + 0.7f * (float)Core.TotemGame.Random.NextDouble();
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

				if (NightSky != null)
				{
					lock (NightSky)
					{
						NightSky.Dispose();
					}
				}
				NightSky = sky;
			}


			{
				Color nebulaColor = MultiLerp((float)random.NextDouble(), Color.Purple, Color.Pink, Color.PaleGreen, Color.Red, Color.Yellow);

				var nebulae = new Texture2D(world.Game.GraphicsDevice, (int)world.Game.Resolution.X, (int)world.Game.Resolution.Y);
				Color[] colorMap = new Color[nebulae.Width * nebulae.Height];
				for (int i = 0; i < colorMap.Length; ++i)
				{
					float x = (i % nebulae.Width) / 128f,
						  y = (i / nebulae.Width) / 128f;
					Color main = Color.Lerp(Color.Transparent, nebulaColor, 
					                        Math.Max((float)noise.Evaluate(x,y ) - 0.3f, 0));
					colorMap[i] = main;
				}
				nebulae.SetData<Color>(colorMap);

				if (NightSkyNebulae != null)
				{
					lock (NightSkyNebulae)
					{
						NightSkyNebulae.Dispose();
					}
				}
				NightSkyNebulae = nebulae;
			}
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

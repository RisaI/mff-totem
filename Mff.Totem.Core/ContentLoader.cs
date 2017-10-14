using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Mff.Totem
{
	public static class ContentLoader
	{
		/// <summary>
		/// 1x1 texture of a white pixel.
		/// </summary>
		/// <value>Pixel.</value>
		public static Texture2D Pixel
		{
			get;
			private set;
		}

		public static Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
		public static Dictionary<string, SpriteFont> Fonts = new Dictionary<string, SpriteFont>();


		public static void Load(Game game)
		{
			// Generate a pixel texture
			Pixel = new Texture2D(game.GraphicsDevice, 1, 1);
			Pixel.SetData<Color>(new Color[] { Color.White });

			//TODO: Texture loading

			// Load SpriteFonts
			Fonts.Add("console", game.Content.Load<SpriteFont>("fonts/console"));
		}
	}
}

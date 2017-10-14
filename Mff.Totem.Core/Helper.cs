using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem
{
	public static class Helper
	{
		/// <summary>
		/// Converts an angle to direction.
		/// </summary>
		public static Vector2 AngleToDirection(float angle)
		{
			return new Vector2((float)Math.Cos(angle), (float)-Math.Sin(angle));
		}

		/// <summary>
		/// Converts a direction to an angle.
		/// </summary>
		public static float DirectionToAngle(Vector2 dir)
		{
			return (float)Math.Atan2(-dir.Y, dir.X);
		}

		/// <summary>
		/// Returns the size of a texture as Vector2.
		/// </summary>
		public static Vector2 Size(this Texture2D texture)
		{
			return new Vector2(texture.Width, texture.Height);
		}


		/// <summary>
		/// Negative division with a reverse behavior on negative numbers
		/// </summary>
		public static int NegDivision(int a, int b)
		{
			return a < 0 ? a / b - 1 : a / b;
		}

		/// <summary>
		/// Draws a rectangle using a SpriteBatch instance.
		/// </summary>
		/// <param name="spriteBatch">Sprite batch.</param>
		/// <param name="area">Area.</param>
		/// <param name="color">Color.</param>
		/// <param name="depth">SpriteBatch depth.</param>
		public static void DrawRectangle(this SpriteBatch spriteBatch, Rectangle area, Color color, float depth)
		{
			spriteBatch.Draw(ContentLoader.Pixel, area, null, color, 0, Vector2.Zero, SpriteEffects.None, depth);
		}
	}
}

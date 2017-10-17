using System;
using System.IO;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace Mff.Totem
{
	public static class Helper
	{
		public const float EPSILON = 0.00001f;

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

		/// <summary>
		/// Read a Vector2 from JObject
		/// </summary>
		/// <returns>A vector.</returns>
		/// <param name="obj">JObject.</param>
		public static Vector2 JTokenToVector2(Newtonsoft.Json.Linq.JToken obj)
		{
			var clrs = ((string)obj).Split(';');
			return new Vector2(float.Parse(clrs[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(clrs[1], NumberStyles.Any, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Read a color from JObject.
		/// </summary>
		/// <returns>A color.</returns>
		/// <param name="obj">JObject.</param>
		public static Color JTokenToColor(Newtonsoft.Json.Linq.JToken obj)
		{
			var clrs = ((string)obj).Split(';');
			return new Color(byte.Parse(clrs[0]), byte.Parse(clrs[1]), byte.Parse(clrs[2]), (clrs.Length >= 4 ? byte.Parse(clrs[3]) : (byte)255));
		}


		#region Extensions
		public static void Write(this BinaryWriter writer, Vector2 vector)
		{
			writer.Write(vector.X);
			writer.Write(vector.Y);
		}

		public static void Write(this BinaryWriter writer, Guid guid)
		{
			writer.Write(guid.ToByteArray());
		}

		public static void Write(this BinaryWriter writer, Color color)
		{
			writer.Write(color.PackedValue);
		}

		public static void Write(this BinaryWriter writer, Core.ISerializable serializable)
		{
			serializable.Serialize(writer);
		}

		public static Vector2 ReadVector2(this BinaryReader reader)
		{
			return new Vector2(reader.ReadSingle(), reader.ReadSingle());
		}

		public static Guid ReadGuid(this BinaryReader reader)
		{
			return new Guid(reader.ReadBytes(16));
		}

		public static Color ReadColor(this BinaryReader reader)
		{
			return new Color(reader.ReadUInt32());
		}

		public static void WriteVector2(this JsonWriter writer, Vector2 vector)
		{
			writer.WriteValue(vector.X + ";" + vector.Y);
		}

		public static void WriteColor(this JsonWriter writer, Color color)
		{
			writer.WriteValue(color.R + ";" + color.G + ";" + color.B + ";" + color.A);
		}
		#endregion
	}
}

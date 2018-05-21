using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Mff.Totem.Core;
using System.Collections.Generic;
using Physics2D.Common;
using ClipperLib;

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
		/// Returns the size of a texture as Vector2.
		/// </summary>
		public static Vector2 Size(this Microsoft.Xna.Framework.Rectangle rect)
		{
			return new Vector2(rect.Width, rect.Height);
		}

		/// <summary>
		/// Negative division with a reverse behavior on negative numbers
		/// </summary>
		public static int NegDivision(int a, int b)
		{
			return a < 0 ? a / b - 1 : a / b;
		}

		/// <summary>
		/// Negative division with a reverse behavior on negative numbers
		/// </summary>
		public static int NegModulo(int a, int b)
		{
			return a - NegDivision(a,b) * b;
		}

		/// <summary>
		/// Negative division with a reverse behavior on negative numbers
		/// </summary>
		public static long NegDivision(long a, long b)
		{
			return a < 0 ? a / b - 1 : a / b;
		}

		/// <summary>
		/// Negative division with a reverse behavior on negative numbers
		/// </summary>
		public static long NegModulo(long a, long b)
		{
			return a - NegDivision(a, b) * b;
		}

		/// <summary>
		/// Draws a rectangle using a SpriteBatch instance.
		/// </summary>
		/// <param name="spriteBatch">Sprite batch.</param>
		/// <param name="area">Area.</param>
		/// <param name="color">Color.</param>
		/// <param name="depth">SpriteBatch depth.</param>
		public static void DrawRectangle(this SpriteBatch spriteBatch, Microsoft.Xna.Framework.Rectangle area, Color color, float depth)
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
		/// Read a Rectangle from JObject
		/// </summary>
		/// <returns>A rectangle.</returns>
		/// <param name="obj">JObject.</param>
		public static Microsoft.Xna.Framework.Rectangle JTokenToRectangle(Newtonsoft.Json.Linq.JToken obj)
		{
			var clrs = ((string)obj).Split(';');
			return new Microsoft.Xna.Framework.Rectangle(int.Parse(clrs[0], NumberStyles.Any, CultureInfo.InvariantCulture), 
			                   int.Parse(clrs[1], NumberStyles.Any, CultureInfo.InvariantCulture),
							   int.Parse(clrs[2], NumberStyles.Any, CultureInfo.InvariantCulture),
			                   int.Parse(clrs[3], NumberStyles.Any, CultureInfo.InvariantCulture));
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

		public static void Write(this BinaryWriter writer, Rectangle rect)
		{
			writer.Write(rect.X);
			writer.Write(rect.Y);
			writer.Write(rect.Width);
			writer.Write(rect.Height);
		}

		public static void Write(this BinaryWriter writer, List<IntPoint> polygon)
		{
			writer.Write(polygon.Count);
			polygon.ForEach(p =>
			{
				writer.Write(p.X);
				writer.Write(p.Y);
			});
		}

		public static void Write(this BinaryWriter writer, Guid guid)
		{
			writer.Write(guid.ToByteArray());
		}

		public static void Write(this BinaryWriter writer, Color color)
		{
			writer.Write(color.PackedValue);
		}

		public static void Write(this BinaryWriter writer, ISerializable serializable)
		{
			serializable.Serialize(writer);
		}

		public static List<IntPoint> ReadPolygon(this BinaryReader reader)
		{
			var c = reader.ReadInt32();
			List<IntPoint> polygon = new List<IntPoint>(c);
			for (int i = 0; i < c; ++i)
			{
				polygon.Add(new IntPoint(reader.ReadInt64(), reader.ReadInt64()));
			}
			return polygon;
		}

		public static Vector2 ReadVector2(this BinaryReader reader)
		{
			return new Vector2(reader.ReadSingle(), reader.ReadSingle());
		}

		public static Rectangle ReadRectangle(this BinaryReader reader)
		{
			return new Rectangle(reader.ReadInt32(), reader.ReadInt32(), 
			                     reader.ReadInt32(), reader.ReadInt32());
		}

		public static Guid ReadGuid(this BinaryReader reader)
		{
			return new Guid(reader.ReadBytes(16));
		}

		public static Color ReadColor(this BinaryReader reader)
		{
			return new Color() { PackedValue = reader.ReadUInt32() };
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

		public static Point ToPoint(this Vector2 vec)
		{
			return new Point((int)vec.X, (int)vec.Y);
		}

		public static Vector2 ToVector2(this Point p)
		{
			return new Vector2(p.X, p.Y);
		}

		public static uint Hash(int i)
		{
			uint a = (uint)i;
			return ((a * 2654435761) % (uint.MaxValue));
		}

		public static float Max(params float[] values)
		{
			return values.Max();
		}

		public static float Min(params float[] values)
		{
			return values.Min();
		}

		public static List<IntPoint> CreateRectangle(int x, int y, int width, int height)
		{
			List<IntPoint> result = new List<IntPoint>(4) { 
				new IntPoint(x, y),
				new IntPoint(x + width, y),
				new IntPoint(x + width, y + width),
				new IntPoint(x, y + width)
			};
			return result;
		}

		/// <summary>
		/// Returns a random point inside this rectangle
		/// </summary>
		/// <returns>The point.</returns>
		/// <param name="rect">Rect.</param>
		public static Vector2 RandomPoint(this Microsoft.Xna.Framework.Rectangle rect)
		{
			return new Vector2(rect.X + (float)(TotemGame.Random.NextDouble() * rect.Width), 
			                   rect.Y + (float)(TotemGame.Random.NextDouble() * rect.Height));
		}
	}

	public static class DrawHelper
	{
		public static float Fit(this SpriteFont font, string text, Vector2 box)
		{
			return Fit(font.MeasureString(text), box);
		}

		public static float Fit(Vector2 size, Vector2 box)
		{
			return Math.Min(box.X / size.X, box.Y / size.Y);
		}
	}

	public class RectangleF : ICloneable<RectangleF>
	{
		public float Left, Right, Top, Bottom;

		public float Width
		{
			get { return Right - Left; }
		}

		public float Height
		{
			get { return Bottom - Top; }
		}

		public RectangleF(float l, float r, float t, float b)
		{
			Left = l;
			Right = r;
			Top = t;
			Bottom = b;
		}

		public void Scale(float scale)
		{
			Left *= scale;
			Right *= scale;
			Top *= scale;
			Bottom *= scale;
		}

		public void Translate(Vector2 t)
		{
			Left += t.X;
			Right += t.X;
			Top += t.Y;
			Bottom += t.Y;
		}

		public RectangleF Clone()
		{
			return new RectangleF(Left, Right, Top, Bottom);
		}
	}
}

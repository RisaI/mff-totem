using System;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public class Camera
	{
		public Vector2 Position;
		public float Zoom = 1;

		public TotemGame Game
		{
			get;
			private set;
		}

		public Camera(TotemGame game)
		{
			Game = game;
			Game.OnResolutionChange += (x, y) => { RecalculateBoundingBox(); };
			RecalculateBoundingBox();
		}

		/// <summary>
		/// Linearly interpolate camear position to a new value.
		/// </summary>
		/// <param name="position">Position.</param>
		/// <param name="lerp">Lerp.</param>
		public void MoveTo(Vector2 position, float lerp)
		{
			Position = Vector2.Lerp(Position, position, lerp);
		}

		public Vector2 ToScreenSpace(Vector2 worldSpace)
		{
			return Vector2.Transform(worldSpace, ViewMatrix);
		}

		public Vector2 ToWorldSpace(Vector2 screenSpace)
		{
			return Vector2.Transform(screenSpace, Matrix.Invert(ViewMatrix));
		}

		float _rotation;
		public float Rotation
		{
			get { return _rotation; }
			set
			{
				_rotation = value;
				RecalculateBoundingBox();
			}
		}

		void RecalculateBoundingBox()
		{
			Vector2 corner0 = new Vector2(Game.Resolution.X / 2, Game.Resolution.Y / 2),
			corner1 = new Vector2(corner0.X, -corner0.Y);
			Matrix rot = Matrix.CreateRotationZ(Rotation);
			corner0 = Vector2.Transform(corner0, rot);
			corner1 = Vector2.Transform(corner1, rot);
			var maxX = Math.Max(Math.Abs(corner0.X), Math.Abs(corner1.X));
			var maxY = Math.Max(Math.Abs(corner0.Y), Math.Abs(corner1.Y));

			_bounds = new RectangleF(-maxX, maxX,-maxY, maxY);
		}

		RectangleF _bounds;
		public RectangleF BoundingBox
		{
			get
			{
				RectangleF b = _bounds.Clone();
				b.Scale(1 / Zoom);
				b.Translate(Position);
				return b;
			}
		}

		public Matrix ViewMatrix
		{
			get
			{
				return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
							 Matrix.CreateRotationZ(Rotation) *
					         Matrix.CreateScale(Zoom, Zoom, 1) * 
					         Matrix.CreateTranslation(Game.Resolution.X / 2, Game.Resolution.Y / 2, 0);
			}
		}

		public Matrix GetScaledTranslation(float x, float y)
		{
			return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
						 Matrix.CreateRotationZ(Rotation) *
				         Matrix.CreateScale(Zoom, Zoom, 1) *
				         Matrix.CreateScale(x, y, 1) *
						 Matrix.CreateTranslation(Game.Resolution.X / 2, Game.Resolution.Y / 2, 0);
		}
	}
}

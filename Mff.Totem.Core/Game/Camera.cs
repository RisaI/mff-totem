using System;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public class Camera
	{
		public Vector2 Position;
		public float Zoom = 1f, Rotation = 0;

		public TotemGame Game
		{
			get;
			private set;
		}

		public Camera(TotemGame game)
		{
			Game = game;
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

		public float Left
		{
			get { return Position.X - (Game.Resolution.X / 2) / Zoom; }
		}

		public float Top
		{
			get { return Position.Y - (Game.Resolution.Y / 2) / Zoom; }
		}

		public float Right
		{
			get { return Position.X + (Game.Resolution.X / 2) / Zoom; }
		}

		public Matrix ViewMatrix
		{
			get
			{
				return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
					         Matrix.CreateScale(Zoom, Zoom, 1) * 
					         Matrix.CreateRotationZ(Rotation) *
					         Matrix.CreateTranslation(Game.Resolution.X / 2, Game.Resolution.Y / 2, 0);
			}
		}

		public Matrix GetScaledTranslation(float x, float y)
		{
			return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
				         Matrix.CreateScale(Zoom, Zoom, 1) *
						 Matrix.CreateRotationZ(Rotation) *
				         Matrix.CreateScale(x, y, 1) *
						 Matrix.CreateTranslation(Game.Resolution.X / 2, Game.Resolution.Y / 2, 0);
		}
	}
}

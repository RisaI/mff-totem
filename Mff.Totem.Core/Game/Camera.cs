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

		public Matrix ViewMatrix
		{
			get
			{
				return Matrix.CreateScale(Zoom, Zoom, 1) * 
					         Matrix.CreateRotationZ(Rotation) *
					         Matrix.CreateTranslation(Game.Resolution.X / 2 - Position.X, Game.Resolution.Y / 2 - Position.Y, 0);
			}
		}
	}
}

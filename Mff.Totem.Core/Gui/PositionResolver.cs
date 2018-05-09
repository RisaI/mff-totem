using System;
using Microsoft.Xna.Framework;
namespace Mff.Totem.Core
{
	public interface IResolver<T>
	{
		T Resolve();
	}

	public class SimpleResolver : IResolver<Vector2>
	{
		public Vector2 Position;

		public SimpleResolver(Vector2 position)
		{
			Position = position;
		}

		public Vector2 Resolve()
		{
			return Position;
		}
	}

	public class ResolutionResolver : IResolver<Vector2>
	{
		public Vector2 Offset;
		public Vector2 RelativeOrigin;
		public TotemGame Game
		{
			get;
			set;
		}

		public ResolutionResolver(TotemGame game, Vector2 relOrigin, Vector2 offset)
		{
			Game = game;
			RelativeOrigin = relOrigin;
			Offset = offset;
		}

		public Vector2 Resolve()
		{
			return Game.Resolution * RelativeOrigin + Offset;
		}
	}
}

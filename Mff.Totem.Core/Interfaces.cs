using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public interface IUpdatable
	{
		void Update(GameTime gameTime);
	}

	public interface IDrawable
	{
		void Draw(SpriteBatch spriteBatch);
	}

	public interface ICloneable<T>
	{
		T Clone();
	}
}

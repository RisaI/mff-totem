using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public class Menu
	{
		public List<UiControl> Controls;

		public TotemGame Game
		{
			get;
			private set;
		}

		public Menu(TotemGame game)
		{
			Game = game;
			Controls = new List<UiControl>();
		}

		public void Update(GameTime gameTime)
		{
			for (int i = 0; i < Controls.Count; ++i)
				Controls[i].Update(gameTime);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
			for (int i = 0; i < Controls.Count; ++i)
				Controls[i].Draw(spriteBatch);
			spriteBatch.End();
		}
	}
}

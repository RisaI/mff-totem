using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public class HUD : IUpdatable, IDrawable
	{
		public Entity Observed;
		public SpriteFont Font
		{
			get { return ContentLoader.Fonts["menu"]; }
		}

		public TotemGame Game
		{
			get;
			private set;
		}

		public HUD(TotemGame game)
		{
			Game = game;
		}

		public void Update(GameTime gameTime)
		{

		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (Observed == null)
				return;
			
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
			spriteBatch.DrawString(Font, "Test", new Vector2(64), Color.White);
			spriteBatch.End();

			Observed = null;
		}
	}
}

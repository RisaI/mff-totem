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
			get { return ContentLoader.Fonts["console"]; }
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

			var pointer = Game.Input.GetPointerInput(0);
			var l = Observed.GetComponent<CharacterComponent>();

			if (l != null)
			{
				var hpRect = new Rectangle(10, 10, 192, 16);
				spriteBatch.DrawRectangle(hpRect, Color.DarkRed, 0f);
				spriteBatch.DrawRectangle(new Rectangle(hpRect.X, hpRect.Y, (int)(hpRect.Width * (l.HP / (float)l.MaxHP)), hpRect.Height), Color.Red, 0.1f);
				if (hpRect.Contains(pointer.Position.ToPoint()))
				{
					var text = l.HP + "/" + l.MaxHP;
					spriteBatch.DrawString(Font, text, new Vector2(hpRect.X + hpRect.Width / 2, hpRect.Y + hpRect.Height / 2), Color.White, 0, Font.MeasureString(text) / 2, hpRect.Height / Font.MeasureString(text).Y, SpriteEffects.None, 1f);
				}

				var stamRect = new Rectangle(hpRect.X + hpRect.Width, hpRect.Y, 64, hpRect.Height);
				//spriteBatch.DrawRectangle(stamRect, Color.Lerp(Color.Black, Color.Yellow, 0.8f), 0f);
				spriteBatch.DrawRectangle(new Rectangle(stamRect.X, stamRect.Y, (int)(stamRect.Width * (l.Stamina / l.MaxStamina)), stamRect.Height), Color.Yellow, 0.1f);
				if (stamRect.Contains(pointer.Position.ToPoint()))
				{
					var text = l.Stamina + "/" + l.MaxStamina;
					var size = Font.MeasureString(text);
					spriteBatch.DrawRectangle(new Rectangle(stamRect.X + stamRect.Width / 2 - (int)size.X / 2, stamRect.Y + stamRect.Height / 2 - (int)size.Y / 2, (int)size.X, (int)size.Y), Color.Lerp(Color.White, Color.Transparent, 0.6f), 0f);
					spriteBatch.DrawString(Font, text, new Vector2(stamRect.X + stamRect.Width / 2, stamRect.Y + stamRect.Height / 2), Color.Gray, 0, size / 2, stamRect.Height / size.Y, SpriteEffects.None, 1f);
				}
			}
			spriteBatch.End();

			Observed = null;
		}
	}
}

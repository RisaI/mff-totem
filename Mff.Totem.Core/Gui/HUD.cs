using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public class HUD : IUpdatable, IDrawable
	{
		const int CHAT_WIDTH = 320;

		public Entity Observed;
		public SpriteFont Font
		{
			get { return ContentLoader.Fonts["console"]; }
		}

		private List<ChatLine> ChatLines = new List<ChatLine>();

		public TotemGame Game
		{
			get;
			private set;
		}

		public HUD(TotemGame game)
		{
			Game = game;
		}

		float _currentTime;
		public void Update(GameTime gameTime)
		{
			_currentTime = (float)gameTime.TotalGameTime.TotalSeconds;
			ChatLines.RemoveAll(cl => cl.TimeAdded + 5 <= gameTime.TotalGameTime.TotalSeconds);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (Observed != null)
			{
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

			// Chat
			if (ChatLines.Count > 0)
			{
				float offset = 0;
				spriteBatch.Begin();
				for (int i = 0; i < ChatLines.Count; ++i)
				{
					var line = ChatLines[ChatLines.Count - 1 - i];
					var text = line.Text;
					Vector2 size = Font.MeasureString(line.Text);

					offset += ((int)size.X - 1) / CHAT_WIDTH + 1; ;
					int localCounter = 1;
					for (int s = 0; s < text.Length; ++s)
					{
						if (Font.MeasureString(text.Substring(0, s)).X > CHAT_WIDTH)
						{
							spriteBatch.DrawString(Font, text.Substring(0, s - 1), new Vector2(8, Game.Resolution.Y - 8 - (offset - localCounter)* Font.LineSpacing),
							                       Color.Lerp(Color.Transparent, line.Color, Math.Min(1, 5f - (_currentTime - line.TimeAdded))),
							                       0, new Vector2(0, Font.LineSpacing), 1, SpriteEffects.None, 1f);
							localCounter++;
							text = text.Substring(s - 1);
							s = 1;
						}
						else if (s == text.Length - 1)
						{
							spriteBatch.DrawString(Font, text, new Vector2(8, Game.Resolution.Y - 8 - (offset - localCounter) * Font.LineSpacing),
												   Color.Lerp(Color.Transparent, line.Color, Math.Min(1, 5f - (_currentTime - line.TimeAdded))),
												   0, new Vector2(0, Font.LineSpacing), 1, SpriteEffects.None, 1f);
							break;
						}
					}

					if (offset >= 16)
						break;
				}
				spriteBatch.End();
			}

		}

		public void Chat(string text, Color? color = null)
		{
			ChatLines.Add(new ChatLine() { Text = text, Color = color != null ? color.Value : Color.White, TimeAdded = _currentTime });
		}

		class ChatLine
		{
			public string Text;
			public Color Color;

			public float TimeAdded;
		}
	}
}

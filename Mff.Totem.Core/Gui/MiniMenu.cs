using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Gui
{
	public class MiniMenu : Gui
	{
		const int Width = 240, Padding = 2;
		const int ButtonHeight = 64;

		public MiniMenu() : base(new Vector2(Width, 4 * Padding + 3 * ButtonHeight + 1))
		{
			WindowName = "Mini Menu";
			BarVisible = false;
		}

		string[] buttons = new string[] { "Resume", "Options", "Exit To Menu" };
		protected override void CustomDraw(SpriteBatch spriteBatch)
		{
			var font = ContentLoader.Fonts["menu"];
			for (int i = 0; i < buttons.Length; ++i)
			{
				var area = new Rectangle(Padding, Padding + i * (ButtonHeight + Padding), Width - Padding * 2, ButtonHeight);
				spriteBatch.DrawRectangle(area, Color.Gray, 0.1f);
				spriteBatch.DrawString(font, buttons[i], area.Center.ToVector2(), Color.White, 0, font.MeasureString(buttons[i]) / 2, Math.Min(0.5f, font.Fit(buttons[i], area.Size())), SpriteEffects.None, 1f);
			}
		}

		protected override void OnUpdate(GameTime gameTime)
		{
			Position = Manager.Game.Resolution / 2 - Size / 2;
		}

		protected override void OnInput(Core.PointerInput input)
		{
			if (input.State == Core.InputState.Pressed)
			{
				for (int i = 0; i < buttons.Length; ++i)
				{
					Rectangle area = new Rectangle(Padding, Padding + i * (ButtonHeight + Padding), Width - Padding * 2, ButtonHeight);
					if (area.Contains(input.Position.ToPoint()))
					{
						switch (i)
						{
							case 0:
								Closing = true;
								break;
							case 2:
								Manager.Game.GameState = Core.GameStateEnum.Menu;
								Closing = true;
								break;
						}
					}
				}
			}
		}
	}
}

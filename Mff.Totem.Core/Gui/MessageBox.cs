using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace Mff.Totem.Gui
{
    public class MessageBox : GuiManager.Gui
	{
		public string Text;
		public MessageBox(string text, GuiManager manager) : base(manager, new Vector2(60), new Vector2(320, 128))
		{
			WindowName = "Message Box";
			Text = text;
		}

		protected override void OnUpdate(GameTime gameTime)
		{

		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			var font = ContentLoader.Fonts["menu"];
			spriteBatch.GraphicsDevice.ScissorRectangle = Area;
			spriteBatch.Begin(SpriteSortMode.FrontToBack, null, null, null, scissorRasterizer);
			DrawBody(spriteBatch);
			var size = font.MeasureString(Text);
			spriteBatch.DrawString(font, Text, Area.Center.ToVector2() + new Vector2(0, ScaledBarHeight / 2), Color.White, 0, size / 2, Math.Min(1, Size.X / size.X), SpriteEffects.None, 1f);
			spriteBatch.End();
		}
	}
}

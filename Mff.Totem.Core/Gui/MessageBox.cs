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
    public class MessageBox : Gui
	{
		public string Text;
		public MessageBox(string text) : base(new Vector2(320, 128))
		{
			WindowName = "Message Box";
			Text = text;
		}

		protected override void OnUpdate(GameTime gameTime)
		{

		}

		protected override void CustomDraw(SpriteBatch spriteBatch)
		{
			var font = ContentLoader.Fonts["menu"];
			var size = font.MeasureString(Text);
			spriteBatch.DrawString(font, Text, Size / 2, Color.White, 0, size / 2, Math.Min(1, font.Fit(Text, Size)), SpriteEffects.None, 1f);
		}
	}
}

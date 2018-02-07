using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public abstract class UiControl
	{
		public UiControl Parent;
		public TotemGame Game
		{
			get;
			private set;
		}

		public UiControl(TotemGame game, UiControl parent)
		{
			Game = game;
			Parent = parent;
		}

		public Vector2 Position;
		public abstract Vector2 Size
		{
			get;
			set;
		}

		public abstract void Update(GameTime gameTime);
		public abstract void Draw(SpriteBatch spriteBatch);
	}

	public class Container : UiControl
	{
		private RenderTarget2D RenderTarget;

		private Vector2 _size;
		public override Vector2 Size
		{
			get { return _size; }
			set
			{
				_size = value;
				if (RenderTarget != null)
					RenderTarget.Dispose();
				RenderTarget = new RenderTarget2D(Game.GraphicsDevice, (int)value.X, (int)value.Y);
			}
		}

		public UiControl Child;

		public Container(TotemGame game, UiControl parent = null) : base(game, parent) { }

		public override void Draw(SpriteBatch spriteBatch)
		{
			
		}

		public override void Update(GameTime gameTime)
		{
			
		}
	}

	/*public abstract class Layout : UiControl
	{
		public float Padding, Spacing;
		public List<UiControl> Children = new List<UiControl>();
	}*/

	public class MenuButton : UiControl
	{
		const int Width = 240, Height = 48;

		public override Vector2 Size
		{
			get
			{
				return new Vector2(Width, Height);
			}

#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
			set
#pragma warning restore RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
			{
				return;
			}
		}

		public MenuButton(TotemGame game, UiControl parent = null) : base(game, parent) { }

		public string Text;
		public Action OnClick;

		public Rectangle Area
		{
			get { return new Rectangle((int)Position.X, (int)Position.Y, Width, Height); }
		}

		public SpriteFont Font
		{
			get { return ContentLoader.Fonts["menu"]; }
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			spriteBatch.DrawRectangle(Area, Game.Input.InputInsideRectangle(Area, InputState.Down) ? Color.Gray : Color.Black, 0);
			if (Text != null)
			{
				var size = Font.MeasureString(Text);
				spriteBatch.DrawString(Font, Text, Area.Center.ToVector2(), Color.White, 0, size / 2, Font.Fit(Text, new Vector2(Width - 32, Height - 8)), SpriteEffects.None, 1);
			}
		}
		public override void Update(GameTime gameTime)
		{
			if (Game.Input.InputInsideRectangle(Area, InputState.Released) && OnClick != null)
				OnClick.Invoke();
		}
	}
}

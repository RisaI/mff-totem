using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mff.Totem.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace Mff.Totem.Gui
{
	public class GuiManager
	{
		private List<Gui> Guis = new List<Gui>();
		public Gui this[int index]
		{
			get { return Guis[index]; }
		}

		public int Count
		{
			get { return Guis.Count; }
		}

		public TotemGame Game
		{
			get;
			private set;
		}

		public GuiManager(TotemGame game)
		{
			Game = game;
		}

		public void Update(GameTime gameTime)
		{
			Guis.ForEach(g => g.Update(gameTime));
			Guis.RemoveAll(g => g.Closing);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			Guis.ForEach(g =>
			{
				g.Draw(spriteBatch);
			});
		}

		public void Add(Gui gui)
		{
			if (!Guis.Contains(gui))
				Guis.Add(gui);
			gui.Attach(this);
			BringToFront(gui);
		}

		public T GetGuiOfType<T>() where T : Gui
		{
			return (T)Guis.Find(g => g is T);
		}

		public Gui GetGuiById(string id)
		{
			return Guis.Find(g => g.WindowID == id);
		}

		public Gui GuiAt(Vector2 point)
		{
			Gui f = null;
			Guis.ForEach(g => { if (g.Area.Contains(point.ToPoint()) && (f == null || f.Layer < g.Layer)) { f = g; } });
			return f;
		}

		/// <summary>
		/// Sort the gui list according to layers.
		/// </summary>
		public void SortGuis()
		{
			Guis.Sort((x, y) => x.Layer.CompareTo(y.Layer));
		}

		public void BringToFront(Gui gui)
		{
			if (!Guis.Contains(gui))
				return;
			Guis.ForEach(g =>
			{
				if (g.Layer > gui.Layer)
					--g.Layer;
			});
			gui.Layer = Guis.Count - 1;
			SortGuis();	
		}

		public void BringToBack(Gui gui)
		{
			if (!Guis.Contains(gui))
				return;
			Guis.ForEach(g =>
			{
				if (g.Layer < gui.Layer)
					++g.Layer;
			});
			gui.Layer = 0;
			SortGuis();
		}
	}

	public class Gui
	{
		protected static RasterizerState scissorRasterizer = new RasterizerState() { ScissorTestEnable = true };
		public const int BarHeight = 24;

		public GuiManager Manager
		{
			get;
			private set;
		}

		public string WindowID = string.Empty;
		protected string WindowName = string.Empty;
		public int Layer
		{
			get;
			set;
		}

		private Vector2 _position, _size;
		public Vector2 Position
		{
			get { return _position; }
			set
			{
				_position = value;
			}
		}
		public Vector2 Size
		{
			get { return _size; }
			set
			{
				_size = value;
			}
		}

		public Rectangle Area
		{
			get { return new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y + (BarVisible ? BarHeight : 0)); }
		}

		public Rectangle CloseButton
		{
			get
			{
				Rectangle rect = new Rectangle();
				rect.Width = rect.Height = BarHeight - 4;
				rect.X = (int)Size.X - BarHeight + 2;
				rect.Y = -rect.Width - 2;
				return rect;
			}
		}

		private bool _closing = false;
		public bool Closing
		{
			get { return _closing; }
			set
			{
				if (value)
				{
					_closing = true;
					BringToFront();
				}
			}
		}

		public bool BarVisible
		{
			get;
			protected set;
		}

		public Gui(Vector2 size)
		{
			Size = size;
			BarVisible = true;
		}

		public void Attach(GuiManager manager)
		{
			Manager = manager;
			BringToFront();
		}

		public void BringToFront()
		{
			Manager.BringToFront(this);
		}

		public void BringToBack()
		{
			Manager.BringToBack(this);
		}

		private Vector2 dragDelta;
		private int draggingId = -1;
		public virtual void Update(GameTime gameTime)
		{
			if (draggingId >= 0)
			{
				PointerInput input = Manager.Game.Input.GetPointerInput((byte)draggingId);
				switch (input.State)
				{
					case InputState.Up:
					case InputState.Released:
						draggingId = -1;
						break;
					case InputState.Down:
						Position = input.Position - dragDelta;
						break;
				}
			}

			OnUpdate(gameTime);
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			Matrix view = Matrix.CreateTranslation(Position.X, Position.Y, 0);
			spriteBatch.GraphicsDevice.ScissorRectangle = Area;
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, scissorRasterizer, null, view);

			var font = ContentLoader.Fonts["menu"];
			spriteBatch.Draw(ContentLoader.Pixel, new Rectangle(0, BarVisible ? BarHeight : 0, (int)Size.X, (int)Size.Y), null, Color.Black, 0, Vector2.Zero, SpriteEffects.None, 0f);

			// Draw bar
			if (BarVisible)
			{
				spriteBatch.Draw(ContentLoader.Pixel, new Rectangle(0, 0, (int)Size.X, BarHeight), null, Color.Gray, 0, Vector2.Zero, SpriteEffects.None, 0.5f);
				var closeSize = font.MeasureString("X");
				spriteBatch.DrawString(font, "X", CloseButton.Center.ToVector2() + new Vector2(0, BarHeight), Color.LightGray, 0f, closeSize / 2,
									   new Vector2(CloseButton.Width, CloseButton.Height) / closeSize, SpriteEffects.None, 1f);
				if (!string.IsNullOrWhiteSpace(WindowName))
				{
					var size = font.MeasureString(WindowName);
					spriteBatch.DrawString(font, WindowName, new Vector2(2, BarHeight / 2),
										   Color.White, 0, new Vector2(0, size.Y / 2), BarHeight / size.Y, SpriteEffects.None, 1f);
				}
			}
			spriteBatch.End();

			// Draw custom content
			if (BarVisible)
				view = Matrix.CreateTranslation(Position.X, Position.Y + BarHeight, 0);
			if (BarVisible)
				spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle((int)Position.X, (int)Position.Y + BarHeight, (int)Size.X, (int)Size.Y);
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, scissorRasterizer, null, view);
			CustomDraw(spriteBatch);
			spriteBatch.End();
		}

		public void Input(PointerInput input)
		{
			// Translate into inner coordinates
			if (BarVisible)
			{
				input.Position -= Position + new Vector2(0, BarHeight);
				switch (input.State)
				{
					case InputState.Pressed:
						if (CloseButton.Contains(input.Position.ToPoint()))
						{
							Closing = true;
							return;
						}
						BringToFront();
						if (input.Position.Y - Position.Y < BarHeight)
						{
							draggingId = input.ID;
							dragDelta = input.Position + new Vector2(0, BarHeight);
						}
						break;
				}
			}
			else
				input.Position -= Position;

			OnInput(input);
		}

		protected virtual void OnUpdate(GameTime gameTime) { }
		protected virtual void OnInput(PointerInput input) { }
		protected virtual void CustomDraw(SpriteBatch spriteBatch) { }
	}
}

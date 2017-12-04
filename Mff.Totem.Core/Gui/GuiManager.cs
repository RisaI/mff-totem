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
		private List<Gui> Guis
		{
			get;
			set;
		}

		public TotemGame Game
		{
			get;
			private set;
		}

		private float _scale;
		public float GuiScale
		{
			get { return _scale; }
		}

		public GuiManager(TotemGame game)
		{
			Game = game;
			CalculateScale();
			Guis = new List<Gui>();
			Game.OnResolutionChange += (x,y) => { CalculateScale(); Guis.ForEach(g => g.RecalculateArea()); };
		}

		private void CalculateScale()
		{
			_scale = 1; //Game.GuiScale > 1 ? (float)Math.Sqrt(Game.GuiScale) : Game.GuiScale * Game.GuiScale;
		}

		public void Update(GameTime gameTime)
		{
			Guis.ForEach(g => g.Update(gameTime));
			Guis.RemoveAll(g => g.Remove);
		}

		public void SortGuis()
		{
			Guis.Sort((x, y) => x.Layer.CompareTo(y.Layer));
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			Guis.ForEach(g =>
			{
				g.Draw(spriteBatch);
			});
		}

		public T GetGuiOfType<T>() where T : Gui
		{
			return (T)Guis.Find(g => g is T);
		}

		public Gui IsPointInGui(Vector2 point)
		{
			Gui f = null;
			Guis.ForEach(g => { if (g.Area.Contains(point) && (f == null || f.Layer < g.Layer)) { f = g; } });
			return f;
		}

		public abstract class Gui
		{
			protected static RasterizerState scissorRasterizer = new RasterizerState() { ScissorTestEnable = true };
			public const int BarHeight = 24;

			protected string WindowName = string.Empty;
			public int Layer
			{
				get;
				private set;
			}

			private Vector2 _position, _size;
			public Vector2 Position
			{
				get { return _position; }
				set
				{
					_position = value;
					RecalculateArea();
				}
			}
			public Vector2 Size
			{
				get { return _size; }
				set
				{
					_size = value;
					RecalculateArea();
				}
			}

			private Rectangle _area;
			public Rectangle Area
			{
				get { return _area; }
			}
			public Rectangle CloseButton
			{
				get
				{
					Rectangle rect = Area;
					rect.X += rect.Width - ScaledBarHeight + 2;
					rect.Y += 2;
					rect.Width = rect.Height = ScaledBarHeight - 4;
					return rect;
				}
			}

			public int ScaledBarHeight
			{
				get { return (int)(BarHeight * Manager.GuiScale); }
			}

			private bool _remove = false;
			public bool Remove
			{
				get { return _remove; }
				set
				{
					if (value)
					{
						_remove = true;
						BringToFront();
					}
				}
			}

			public GuiManager Manager
			{
				get;
				private set;
			}

			public Gui(GuiManager manager, Vector2 position, Vector2 size)
			{
				Layer = manager.Guis.Count;
				manager.Guis.Add(this);
				_position = position;
				_size = size;
				Manager = manager;
				RecalculateArea();
			}

			private Vector2 dragDelta;
			private int draggingId = -1;
			public void Update(GameTime gameTime)
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
			protected abstract void OnUpdate(GameTime gameTime);
			public abstract void Draw(SpriteBatch spriteBatch);

			public virtual void OnInput(PointerInput input)
			{
				switch (input.State)
				{
					case InputState.Pressed:
						if (CloseButton.Contains(input.Position))
						{
							Remove = true;
							return;
						}
						BringToFront();
						if (input.Position.Y - Position.Y < ScaledBarHeight)
						{
							draggingId = input.ID;
							dragDelta = input.Position - Position;
						}
						break;
						/*case InputState.Released:
							if (draggingId == input.ID)
								draggingId = -1;
							break;
						case InputState.Down:
							if (draggingId == input.ID)
								Position = input.Position - dragDelta;
							break;*/
				}
			}

			public void BringToFront()
			{
				Manager.Guis.ForEach(g =>
				{
					if (g.Layer > Layer)
						--g.Layer;
				});
				Layer = Manager.Guis.Count - 1;
				Manager.SortGuis();
			}

			public void BringToBack()
			{
				Manager.Guis.ForEach(g =>
				{
					if (g.Layer < Layer)
						++g.Layer;
				});
				Layer = 0;
				Manager.SortGuis();
			}

			public virtual void RecalculateArea()
			{
				_area = new Rectangle(Position.ToPoint(), (Size * Manager.GuiScale).ToPoint());
			}

			protected void DrawBody(SpriteBatch spriteBatch, bool drawBar = true)
			{
				var font = ContentLoader.Fonts["menu"];
				spriteBatch.Draw(ContentLoader.Pixel, Area, null, Color.Black, 0, Vector2.Zero, SpriteEffects.None, 0f);
				if (drawBar)
				{
					spriteBatch.Draw(ContentLoader.Pixel, new Rectangle(Area.X, Area.Y, Area.Width, ScaledBarHeight), null, Color.Gray, 0, Vector2.Zero, SpriteEffects.None, 0.99f);
					var closeSize = font.MeasureString("X");
					spriteBatch.DrawString(font, "X", CloseButton.Center.ToVector2(), Color.LightGray, 0f, closeSize / 2, CloseButton.Size.ToVector2() / closeSize, SpriteEffects.None, 1f);
					if (!string.IsNullOrWhiteSpace(WindowName))
					{
						var size = font.MeasureString(WindowName);
						spriteBatch.DrawString(font, WindowName, Position + new Vector2(2, ScaledBarHeight / 2), Color.White, 0, new Vector2(0, size.Y / 2), ScaledBarHeight / size.Y, SpriteEffects.None, 1f);
					}
				}
			}
		}
	}
}

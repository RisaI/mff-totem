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
    class InventoryScreen : Gui
    {
        const int ITEM_HEIGHT_CLOSED = 24,
            ITEM_HEIGHT_OPENED = 64;
        static Vector2 _lastPos = new Vector2(50);

        public Entity Player
        {
            get;
            private set;
        }

		public InventoryComponent Inventory
		{
			get { return Player.GetComponent<InventoryComponent>(); }
		}

        private int selectedItem = -1, selectedEquip = -1;
        public InventoryScreen(Entity player) : base(new Vector2(840, 640))
        {
            WindowName = "Inventory";
            Player = player;
        }

        protected override void OnInput(PointerInput input)
        {
            base.OnInput(input);

            switch (input.State)
            {
                case InputState.Pressed:
					if (ItemArea.Contains(input.Position.ToPoint()))
                    {
                        int offset = 0;
                        bool selected = false;
                        for (int i = 0; i < Inventory.Items.Count; ++i)
                        {
                            var rect = new Rectangle(ItemArea.X + 2, ItemArea.Y + 2 + offset, ItemArea.Width - 4, selectedItem == i ? ITEM_HEIGHT_OPENED : ITEM_HEIGHT_CLOSED);
							if (rect.Contains(input.Position.ToPoint()))
                            {
                                selected = true;
                                if (selectedItem == i)
                                {
                                    var buttonSize = new Point((rect.Width / 3 - 12), (int)(ITEM_HEIGHT_OPENED - ITEM_HEIGHT_CLOSED - 8));
									if (new Rectangle(rect.Right - 2 - buttonSize.X, rect.Bottom - 2 - buttonSize.Y, buttonSize.X, buttonSize.Y).Contains(input.Position.ToPoint()))
                                    {
                                        //Drop
                                        Inventory.DropItem(i);
                                        selectedItem = -1;
                                    }
									else if (Inventory.Items[i].Usable && new Rectangle(rect.Center.X - buttonSize.X / 2, rect.Bottom - 2 - buttonSize.Y, buttonSize.X, buttonSize.Y).Contains(input.Position.ToPoint()))
                                    {
                                        //Use

                                    }
									else if (Inventory.Items[i].Slot != EquipSlot.None && new Rectangle(rect.Left + 2, rect.Bottom - 2 - buttonSize.Y, buttonSize.X, buttonSize.Y).Contains(input.Position.ToPoint()))
                                    {
                                        //Equip
                                        Inventory.EquipItem(i);
                                        selectedItem = -1;
                                    }
                                }
                                else
                                {
                                    selectedItem = i;
                                }
                                break;
                            }
                            offset += 2 + (selectedItem == i ? ITEM_HEIGHT_OPENED : ITEM_HEIGHT_CLOSED);
                        }
                        if (!selected)
                            selectedItem = -1;
                    }
                    else
                    {
                        bool selected = false;
						for (int i = 0; i < Inventory.Equip.Length; ++i)
                        {
							if (EquipArea(i).Contains(input.Position.ToPoint()))
                            {
                                selected = true;
                                if (Inventory.Equip[i] != null)
                                {
                                    selectedEquip = i;
                                }
                                else
                                {
                                    selectedEquip = -1;
                                }
                            }
                        }

                        if (!selected && selectedEquip >= 0)
                        {
							if (EquipButtons(0).Contains(input.Position.ToPoint()))
                            {
                                Inventory.UnequipItem(selectedEquip);
                            }
                            /*else if (Player.Inventory.Equip[selectedEquip].Usable && equipButtons[1].Contains(input.Position))
                            {
                                //Player.Inventory.UseItemFromInventory
                            }*/
							else if (EquipButtons(2).Contains(input.Position.ToPoint()))
                            {
                                //Inventory.DropEquip(selectedEquip);
                            }
                            selectedEquip = -1;
                        }
                    }
                    break;
            }
        }

		private Rectangle ItemArea
		{
			get { return new Rectangle((int)Size.X / 2 + 3, 3, (int)Size.X / 2 - 6, (int)Size.Y - 6); }
		}

		private Rectangle EquipArea(int index)
		{
			var box = ItemArea.Width / 6;
			return new Rectangle(ItemArea.X - (int)Size.X / 2, ItemArea.Y + index * (2 + box), box, box);
		}

		private Rectangle UseArea(int index)
		{
			var box = ItemArea.Width / 6;
			return new Rectangle(ItemArea.X - (int)Size.X / 2 + index * (box + 2), ItemArea.Y + ItemArea.Height - box - 2, box, box);
		}

		private Rectangle EquipButtons(int index)
		{
			return new Rectangle(4 + index * ((int)Size.X / 6 - 12), (int)Size.Y - 4 - ITEM_HEIGHT_CLOSED, (int)Size.X / 6 - 16, ITEM_HEIGHT_CLOSED);
		}

        protected override void OnUpdate(GameTime gameTime)
        {
            _lastPos = Position;
        }

		public Texture2D ItemSheet
		{
			get { return ContentLoader.Textures["items0"]; }
		}
		const int IconSize = 32, SheetWidth = 10;

        protected override void CustomDraw(SpriteBatch spriteBatch)
        {
            var font = ContentLoader.Fonts["menu"];

			for (int i = 0; i < Inventory.Equip.Length; ++i)
            {
				spriteBatch.DrawRectangle(EquipArea(i), selectedEquip == i ? Color.DarkGray : Color.Gray, 0.5f);
                var item = Inventory.Equip[i];
                if (item != null)
                {
                    var text = Inventory.Equip[i].Count.ToString();
                    var size = font.MeasureString(text);
					spriteBatch.Draw(ItemSheet, EquipArea(i).Location.ToVector2(), new Rectangle(IconSize * (item.TextureID % SheetWidth),
					                                                                             IconSize * (item.TextureID / SheetWidth),
					                                                                             IconSize, IconSize),
					                 Color.White, 0, Vector2.Zero, (float)EquipArea(i).Width / IconSize, SpriteEffects.None, 0.6f);
					spriteBatch.DrawString(font, text, EquipArea(i).Location.ToVector2() + new Vector2(EquipArea(i).Width, EquipArea(i).Height),
					                       Color.White, 0, size, Vector2.One / 2, SpriteEffects.None, 0.6f);
                }

                if (i == selectedEquip)
                {
					DrawButton(spriteBatch, font, EquipButtons(0), "Unequip", 0.5f);
                    /*if (item.Usable)
                        DrawButton(spriteBatch, font, equipButtons[1], "Use", 0.5f);
                    DrawButton(spriteBatch, font, equipButtons[2], "Drop", 0.5f);*/
                }
            }

			for (int i = 0; i < Inventory.UseItems.Length; ++i)
			{
				spriteBatch.DrawRectangle(UseArea(i), selectedEquip == i ? Color.DarkGray : Color.Gray, 0.5f);
				var item = Inventory.UseItems[i];
				if (item != null)
				{
					var text = item.Count.ToString();
					var size = font.MeasureString(text);
					spriteBatch.Draw(ItemSheet, UseArea(i).Location.ToVector2(), new Rectangle(IconSize * (item.TextureID % SheetWidth),
																								 IconSize * (item.TextureID / SheetWidth),
																								 IconSize, IconSize),
					                 Color.White, 0, Vector2.Zero, (float)UseArea(i).Width / IconSize, SpriteEffects.None, 0.6f);
					spriteBatch.DrawString(font, text, UseArea(i).Location.ToVector2() + new Vector2(UseArea(i).Width, UseArea(i).Height),
										   Color.White, 0, size, Vector2.One / 2, SpriteEffects.None, 0.6f);
				}

				/*if (i == selectedEquip)
				{
					DrawButton(spriteBatch, font, EquipButtons(0), "Unequip", 0.5f);
					/*if (item.Usable)
                        DrawButton(spriteBatch, font, equipButtons[1], "Use", 0.5f);
                    DrawButton(spriteBatch, font, equipButtons[2], "Drop", 0.5f);
				}*/
			}

			// Draw Item Area
            float lineh = font.MeasureString("I").Y;
            int offset = 0;
            //Item list
            {
                spriteBatch.DrawRectangle(ItemArea, Color.DarkSlateGray, 0.1f);
                for (int i = 0; i < Inventory.Items.Count; ++i)
                {
                    var item = Inventory.Items[i];
                    var iArea = new Rectangle(ItemArea.X + 2, ItemArea.Y + 2 + offset, ItemArea.Width - 4, selectedItem == i ? ITEM_HEIGHT_OPENED : ITEM_HEIGHT_CLOSED);
                    spriteBatch.DrawRectangle(new Rectangle(ItemArea.X + 2, ItemArea.Y + 2 + offset, ItemArea.Width - 4, selectedItem == i ? ITEM_HEIGHT_OPENED : ITEM_HEIGHT_CLOSED),
                        selectedItem == i ? Color.DarkGray : Color.Gray, 0.2f);

					// Draw Item Icon
					spriteBatch.Draw(ItemSheet, new Vector2(ItemArea.X + 6, ItemArea.Y + 4 + offset), new Rectangle(IconSize * (item.TextureID % SheetWidth),
																								 IconSize * (item.TextureID / SheetWidth),
																								 IconSize, IconSize),
					                 Color.White, 0, Vector2.Zero, (float)(ITEM_HEIGHT_CLOSED - 4) / IconSize, SpriteEffects.None, 0.3f);

					// Draw Item Text
					spriteBatch.DrawString(font, item.ID + " (x" + item.Count + ")",
					                       new Vector2(ItemArea.X + 8 + ITEM_HEIGHT_CLOSED, ItemArea.Y + 4 + offset),
					                       Color.White, 0f, Vector2.Zero, Math.Min(1f, ITEM_HEIGHT_CLOSED / lineh), SpriteEffects.None, 0.5f);
					
                    if (selectedItem == i)
                    {
                        var buttonSize = new Point((iArea.Width / 3 - 12), (int)(ITEM_HEIGHT_OPENED - ITEM_HEIGHT_CLOSED - 8));
                        DrawButton(spriteBatch, font, new Rectangle(iArea.Right - 2 - buttonSize.X, iArea.Bottom - 2 - buttonSize.Y, buttonSize.X, buttonSize.Y), "Drop", 0.5f);

                        if (Inventory.Items[i].Usable)
                        {
                            DrawButton(spriteBatch, font, new Rectangle(iArea.Center.X - buttonSize.X / 2, iArea.Bottom - 2 - buttonSize.Y, buttonSize.X, buttonSize.Y), "Use", 0.5f);
                        }

						if (Inventory.Items[i].Slot != EquipSlot.None)
                        {
                            DrawButton(spriteBatch, font, new Rectangle(iArea.Left + 2, iArea.Bottom - 2 - buttonSize.Y, buttonSize.X, buttonSize.Y), "Equip", 0.5f);
                        }
                    }

                    offset += 2 + (selectedItem == i ? ITEM_HEIGHT_OPENED : ITEM_HEIGHT_CLOSED);
                }
            }
        }

        private void DrawButton(SpriteBatch spriteBatch, SpriteFont font, Rectangle area, string text, float depth)
        {
            spriteBatch.DrawRectangle(area, Color.Black, depth);
            var size = font.MeasureString(text);
            spriteBatch.DrawString(font, text, area.Center.ToVector2(), Color.White, 0, size / 2, Math.Min(1f, area.Height / size.Y), SpriteEffects.None, depth + 0.01f);
        }
    }
}
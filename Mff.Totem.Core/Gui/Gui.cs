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
    class InventoryScreen : GuiManager.Gui
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
        public InventoryScreen(Entity player, GuiManager manager) : base(manager, _lastPos, new Vector2(840, 640))
        {
            WindowName = "Inventory";
            Player = player;
			equipArea = new Rectangle[Inventory.Equip.Length];
			RecalculateArea();
        }

        public override void OnInput(PointerInput input)
        {
            base.OnInput(input);

            switch (input.State)
            {
                case InputState.Pressed:
					if (itemArea.Contains(input.Position.ToPoint()))
                    {
                        int offset = 0;
                        bool selected = false;
                        for (int i = 0; i < Inventory.Items.Count; ++i)
                        {
                            var rect = new Rectangle(itemArea.X + 2, itemArea.Y + 2 + offset, itemArea.Width - 4, selectedItem == i ? ITEM_HEIGHT_OPENED : ITEM_HEIGHT_CLOSED);
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
                        for (int i = 0; i < equipArea.Length; ++i)
                        {
							if (equipArea[i].Contains(input.Position.ToPoint()))
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
                            if (equipButtons[0].Contains(input.Position.ToPoint()))
                            {
                                Inventory.UnequipItem(selectedEquip);
                            }
                            /*else if (Player.Inventory.Equip[selectedEquip].Usable && equipButtons[1].Contains(input.Position))
                            {
                                //Player.Inventory.UseItemFromInventory
                            }*/
                            else if (equipButtons[2].Contains(input.Position.ToPoint()))
                            {
                                //Inventory.DropEquip(selectedEquip);
                            }
                            selectedEquip = -1;
                        }
                    }
                    break;
            }
        }

        public override void RecalculateArea()
        {
            base.RecalculateArea();
            itemArea = new Rectangle(Area.Center.X + 3, Area.Y + ScaledBarHeight + 3, Area.Width / 2 - 6, Area.Height - ScaledBarHeight - 6);
            var box = itemArea.Width / 6;
            for (int i = 0; i < equipArea.Length; ++i)
            {
                equipArea[i] = new Rectangle(itemArea.X - Area.Width / 2, itemArea.Y + i * (2 + box), box, box);
            }

            for (int i = 0; i < equipButtons.Length; ++i)
            {
                equipButtons[i] = new Rectangle(Area.Left + 4 + i * (Area.Width / 6 - 12), Area.Bottom - 4 - ITEM_HEIGHT_CLOSED, Area.Width / 6 - 16, ITEM_HEIGHT_CLOSED);
            }
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            _lastPos = Position;
        }

		Rectangle[] equipArea = new Rectangle[0], equipButtons = new Rectangle[3];
        Rectangle itemArea;
        public override void Draw(SpriteBatch spriteBatch)
        {
            var font = ContentLoader.Fonts["menu"];

            //Draw body
            spriteBatch.GraphicsDevice.ScissorRectangle = Area;
            spriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp, null, scissorRasterizer);
            DrawBody(spriteBatch);
            for (int i = 0; i < equipArea.Length; ++i)
            {
                spriteBatch.DrawRectangle(equipArea[i], selectedEquip == i ? Color.DarkGray : Color.Gray, 0.5f);
                var item = Inventory.Equip[i];
                if (item != null)
                {
                    var text = Inventory.Equip[i].Count.ToString();
                    var size = font.MeasureString(text);
					spriteBatch.DrawString(font, text, equipArea[i].Location.ToVector2() + new Vector2(equipArea[i].Width, equipArea[i].Height), Color.White, 0, size, Vector2.One, SpriteEffects.None, 0.6f);
                }

                if (i == selectedEquip)
                {
                    DrawButton(spriteBatch, font, equipButtons[0], "Unequip", 0.5f);
                    /*if (item.Usable)
                        DrawButton(spriteBatch, font, equipButtons[1], "Use", 0.5f);*/
                    DrawButton(spriteBatch, font, equipButtons[2], "Drop", 0.5f);
                }
            }
            spriteBatch.End();

            //Draw item area
            spriteBatch.GraphicsDevice.ScissorRectangle = itemArea;
            spriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp, null, scissorRasterizer);
            float lineh = font.MeasureString("I").Y;
            int offset = 0;
            //Item list
            {
                spriteBatch.DrawRectangle(itemArea, Color.DarkSlateGray, 0.1f);
                for (int i = 0; i < Inventory.Items.Count; ++i)
                {
                    var item = Inventory.Items[i];
                    var iArea = new Rectangle(itemArea.X + 2, itemArea.Y + 2 + offset, itemArea.Width - 4, selectedItem == i ? ITEM_HEIGHT_OPENED : ITEM_HEIGHT_CLOSED);
                    spriteBatch.DrawRectangle(new Rectangle(itemArea.X + 2, itemArea.Y + 2 + offset, itemArea.Width - 4, selectedItem == i ? ITEM_HEIGHT_OPENED : ITEM_HEIGHT_CLOSED),
                        selectedItem == i ? Color.DarkGray : Color.Gray, 0.2f);
					spriteBatch.DrawString(font, item.ID + " (x" + item.Count + ")",
                        new Vector2(itemArea.X + 4, itemArea.Y + 4 + offset), Color.White, 0f, Vector2.Zero, Math.Min(1f, ITEM_HEIGHT_CLOSED / lineh), SpriteEffects.None, 0.5f);
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
            spriteBatch.End();
        }

        private void DrawButton(SpriteBatch spriteBatch, SpriteFont font, Rectangle area, string text, float depth)
        {
            spriteBatch.DrawRectangle(area, Color.Black, depth);
            var size = font.MeasureString(text);
            spriteBatch.DrawString(font, text, area.Center.ToVector2(), Color.White, 0, size / 2, Math.Min(1f, area.Height / size.Y), SpriteEffects.None, depth + 0.01f);
        }
    }
}
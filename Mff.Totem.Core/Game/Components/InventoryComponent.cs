using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("component_inventory")]
	public class InventoryComponent : EntityComponent, IUpdatable
	{
		public const int EquipSlots = 5;

		public int Size = 24;
		public List<Item> Items = new List<Item>();

		public Item[] Equip = new Item[0];

		public InventoryComponent()
		{
		}

		public Item AddItem(Item item)
		{
			int origCount = item.Count;
			for (int i = 0; i < Items.Count; ++i)
			{
				var invItem = Items[i];
				if (invItem.ID == item.ID && invItem.Count < invItem.MaxStack)
				{
					var count = Math.Min(item.Count, invItem.MaxStack - invItem.Count);
					item.Count -= count;
					invItem.Count += count;
					if (item.Count <= 0)
					{
						if (Parent.Tags.Contains("player") && World?.Game?.Hud != null)
						{
							World.Game.Hud.Chat("Acquired " + (origCount - item.Count) + " of " + item.ID);
						}
						return item;
					}
				}
			}

			if (Items.Count < Size)
			{
				Items.Add(item);
				item = item.Clone();
				item.Count = 0;
			}

			if (Parent.Tags.Contains("player") && World?.Game?.Hud != null)
			{
				World.Game.Hud.Chat("Acquired " + (origCount - item.Count) + " of " + item.ID);
			}

			return item;
		}

        public bool Use(EquipSlot slot)
        {
            if (Equip?.Length != 0 && Equip[(int)slot] != null)
            {
                Equip[(int)slot].Use(Parent);
                return true;
            }
            return false;
        }

		public bool EquipItem(int invSlot)
		{
			if (Equip.Length <= 0 || invSlot < 0 || invSlot >= Items.Count)
				return false;

			var item = Items[invSlot];

			if (item.Slot == EquipSlot.None)
				return false;
			else
			{
				var eq = Equip[(int)item.Slot];
				Equip[(int)item.Slot] = item;
				Items.Remove(item);
				if (eq != null)
					Items.Add(eq);
				return true;
			}
		}

		public bool UnequipItem(int equipSlot)
		{
			if (equipSlot < 0 || equipSlot >= Equip.Length || Equip[equipSlot] == null || Items.Count >= Size)
				return false;

			Items.Add(Equip[equipSlot]);
			Equip[equipSlot] = null;
			return false;
		}

		public void DropItem(int invSlot)
		{
			if (invSlot < 0 || invSlot >= Items.Count)
				return;

			if (Parent.Position.HasValue)
			{
				var bag = World.CreateEntity("itembag");
				bag.GetComponent<BodyComponent>().Position = Parent.Position.Value;
				bag.GetComponent<ItemComponent>().AddItem(Items[invSlot]);
			}
			Items.RemoveAt(invSlot);
		}

		public float HPMultiplier()
		{
			float total = 1;
			for (int i = 0; i < Equip.Length; ++i)
			{
				if (Equip[i] != null)
					total *= Equip[i].HPMultiplier;
			}
			return total;
		}

		public float StaminaMultiplier()
		{
			float total = 1;
			for (int i = 0; i < Equip.Length; ++i)
			{
				if (Equip[i] != null)
					total *= Equip[i].StaminaMultiplier;
			}
			return total;
		}

		public float SpeedMultiplier()
		{
			float total = 1;
			for (int i = 0; i < Equip.Length; ++i)
			{
				if (Equip[i] != null)
					total *= Equip[i].SpeedMultiplier;
			}
			return total;
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			writer.WritePropertyName("size");
			writer.WriteValue(Size);
			writer.WritePropertyName("equip");
			writer.WriteValue(Equip.Length > 0);
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			Size = obj["size"] != null ? (int)obj["size"] : 24;
			if (obj["equip"] != null && (bool)obj["equip"])
			{
				Equip = new Item[EquipSlots];
			}
			else
			{
				Equip = new Item[0];
			}
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(Size);
			writer.Write(Items.Count);
			for (int i = 0; i < Items.Count; ++i)
			{
				Items[i].Serialize(writer);
			}
			writer.Write(Equip.Length);
			for (int i = 0; i < Equip.Length; ++i)
			{
				writer.Write(Equip[i] != null);
				if (Equip[i] != null)
					Equip[i].Serialize(writer);
			}
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			base.OnDeserialize(reader);
			Size = reader.ReadInt32();
			int a = reader.ReadInt32();
			for (int i = 0; i < a; ++i)
			{
				var item = (Item)DeserializationRegister.CreateInstance(reader.ReadString());
				item.Deserialize(reader);
				Items.Add(item);
			}
			Equip = new Item[reader.ReadInt32()];
			for (int i = 0; i < Equip.Length; ++i)
			{
				if (reader.ReadBoolean())
				{
					var item = (Item)DeserializationRegister.CreateInstance(reader.ReadString());
					item.Deserialize(reader);
					Equip[i] = item;
				}
			}
		}

		public override EntityComponent Clone()
		{
			var items = new List<Item>();
			Items.ForEach(i => items.Add(i.Clone()));

			var equip = new Item[Equip.Length];
			for (int i = 0; i < equip.Length; ++i)
			{
				if (Equip[i] != null)
					equip[i] = Equip[i].Clone();
			}

			return new InventoryComponent() { Size = Size, Items = items, Equip = equip };
		}

		public void Update(GameTime gameTime)
		{
			for (int i = 0; i < Equip.Length; ++i)
			{
				if (Equip[i] != null)
					Equip[i].Update(Parent, gameTime);
			}
		}
	}
}

using System;
using System.IO;

namespace Mff.Totem.Core
{
	[Serializable("component_item")]
	public class ItemComponent : EntityComponent, IInteractive
	{
		public ItemComponent()
		{
			
		}

		public override EntityComponent Clone()
		{
			return new ItemComponent();
		}

		public void Interact(Entity ent)
		{
			var inventory = Parent.GetComponent<InventoryComponent>();
			var targetInventory = ent.GetComponent<InventoryComponent>();

			if (inventory != null && targetInventory != null)
			{
				for (int i = inventory.Items.Count - 1; i >= 0; --i)
				{
					var added = targetInventory.AddItem(inventory.Items[i]);
					inventory.Items[i] = added;
					if (added.Count <= 0)
					{
						inventory.Items.RemoveAt(i);
					}
					else
					{
						break;
					}
				}

				if (inventory.Items.Count <= 0)
					Parent.Remove = true;
			}
		}

		public void AddItem(Item i)
		{
			var inventory = Parent.GetComponent<InventoryComponent>();
			if (inventory != null)
				inventory.AddItem(i);
		}

		public override void Serialize(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public override void Deserialize(BinaryReader reader)
		{
			throw new NotImplementedException();
		}
	}
}

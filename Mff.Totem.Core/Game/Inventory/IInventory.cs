using System;
namespace Mff.Totem.Core
{
	public interface IInventory
	{
		int ItemCount();
		Item GetItem(int index);
		bool AddItem(Item i);
		float HPMultiplier();
		float AgilityMultiplier();
	}

	public enum EquipSlot
	{
		Head = 0,
		Torso = 1,
		Legs = 2,
		Accessory = 3,
		Left = 4,
		Right = 5,
		None = 255
	}
}

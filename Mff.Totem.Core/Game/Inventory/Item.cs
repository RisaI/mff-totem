using System;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	[Serializable("item")]
	public class Item : ICloneable<Item>, IJsonSerializable, ISerializable
	{
		public string ID;
		public int MaxStack = 1;
		public bool Usable;
		public EquipSlot Slot = EquipSlot.None;

		public float SpeedMultiplier = 1, HPMultiplier = 1, StaminaMultiplier = 1;

		int _count = 1;
		public int Count
		{
			get { return _count; }
			set { _count = (int)MathHelper.Clamp(value, 0, MaxStack); }
		}

		public virtual void Use(Entity ent) { }

		public virtual Item Clone()
		{
			return new Item() { ID = ID, MaxStack = MaxStack, Usable = Usable, _count = _count, Slot = Slot, SpeedMultiplier = SpeedMultiplier, HPMultiplier = HPMultiplier, StaminaMultiplier = StaminaMultiplier };
		}

		public void ToJson(JsonWriter writer)
		{
			writer.WriteStartObject();
			DeserializationRegister.ObjectClassToJson(writer, this);

			writer.WritePropertyName("id");
			writer.WriteValue(ID);
			writer.WritePropertyName("maxstack");
			writer.WriteValue(MaxStack);
			writer.WritePropertyName("usable");
			writer.WriteValue(Usable);
			writer.WritePropertyName("slot");
			writer.WriteValue((int)Slot);

			writer.WritePropertyName("hpmul");
			writer.WriteValue(HPMultiplier);
			writer.WritePropertyName("stamul");
			writer.WriteValue(StaminaMultiplier);
			writer.WritePropertyName("spdmul");
			writer.WriteValue(SpeedMultiplier);

			OnToJson(writer);
			writer.WriteEndObject();
		}

		public virtual void OnToJson(JsonWriter writer) { }

		public void FromJson(JObject obj)
		{
			ID = obj["id"] != null ? (string)obj["id"] : "";
			MaxStack = obj["maxstack"] != null ? (int)obj["maxstack"] : 1;
			Usable = obj["usable"] != null ? (bool)obj["usable"] : false;
			Slot = obj["slot"] != null ? (EquipSlot)((int)obj["slot"]) : EquipSlot.None;

			HPMultiplier = obj["hpmul"] != null ? (float)obj["hpmul"] : 1;
			StaminaMultiplier = obj["stamul"] != null ? (float)obj["stamul"] : 1;
			SpeedMultiplier = obj["spdmul"] != null ? (float)obj["spdmul"] : 1;

			OnFromJson(obj);
		}

		public virtual void OnFromJson(JObject obj) { }

		public virtual void Serialize(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.Write(MaxStack);
			writer.Write(Usable);
			writer.Write(Count);
			writer.Write((int)Slot);

			writer.Write(HPMultiplier);
			writer.Write(StaminaMultiplier);
			writer.Write(SpeedMultiplier);
		}

		public virtual void Deserialize(BinaryReader reader)
		{
			ID = reader.ReadString();
			MaxStack = reader.ReadInt32();
			Usable = reader.ReadBoolean();
			Count = reader.ReadInt32();
			Slot = (EquipSlot)reader.ReadInt32();

			HPMultiplier = reader.ReadSingle();
			StaminaMultiplier = reader.ReadSingle();
			SpeedMultiplier = reader.ReadSingle();
		}
	}
}

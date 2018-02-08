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
		public int MaxStack = 1, TextureID = 0;
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
            return new Item().CopyData(this);
		}

        public Item CopyData(Item i)
        {
            ID = i.ID;
            MaxStack = i.MaxStack;
			TextureID = i.TextureID;
            Usable = i.Usable;
            _count = i._count;
            Slot = i.Slot;
            SpeedMultiplier = i.SpeedMultiplier;
            HPMultiplier = i.HPMultiplier;
            StaminaMultiplier = i.StaminaMultiplier;

            return this;
        }

		public void ToJson(JsonWriter writer)
		{
			writer.WriteStartObject();
			DeserializationRegister.ObjectClassToJson(writer, this);

			writer.WritePropertyName("id");
			writer.WriteValue(ID);
			writer.WritePropertyName("texid");
			writer.WriteValue(TextureID);
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
			TextureID = obj["texid"] != null ? (int)obj["texid"] : 0;
			MaxStack = obj["maxstack"] != null ? (int)obj["maxstack"] : 1;
			Usable = obj["usable"] != null ? (bool)obj["usable"] : false;
			Slot = obj["slot"] != null ? (EquipSlot)Enum.Parse(typeof(EquipSlot),(string)obj["slot"]) : EquipSlot.None;

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
			writer.Write(TextureID);
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
			TextureID = reader.ReadInt32();
			Usable = reader.ReadBoolean();
			Count = reader.ReadInt32();
			Slot = (EquipSlot)reader.ReadInt32();

			HPMultiplier = reader.ReadSingle();
			StaminaMultiplier = reader.ReadSingle();
			SpeedMultiplier = reader.ReadSingle();
		}
	}

    [Serializable("item_bow")]
    public class Bow : Item
    {
        public override void Use(Entity ent)
        {
            var character = ent.GetComponent<CharacterComponent>();
            if (character != null)
            {
                var direction = character.Target;
                direction.Normalize();
                direction *= 3000f;
                ent.World.CreateEntity("arrow_projectile").GetComponent<ProjectileBody>().SetProjectileData(ent, direction);
            }
        }

        public override Item Clone()
        {
            return new Bow().CopyData(this);
        }
    }
}

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

		public float SpeedMultiplier, HPMultiplier, StaminaMultiplier;

		int _count = 1;
		public int Count
		{
			get { return _count; }
			set { _count = (int)MathHelper.Clamp(value, 0, MaxStack); }
		}

		public virtual void Use(Entity ent) { }

		public virtual Item Clone()
		{
			return new Item() { ID = ID, MaxStack = MaxStack, Usable = Usable, _count = _count };
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

			OnToJson(writer);
			writer.WriteEndObject();
		}

		public virtual void OnToJson(JsonWriter writer) { }

		public void FromJson(JObject obj)
		{
			ID = obj["id"] != null ? (string)obj["id"] : "";
			MaxStack = obj["maxstack"] != null ? (int)obj["maxstack"] : 1;
			Usable = obj["usable"] != null ? (bool)obj["usable"] : false;
			OnFromJson(obj);
		}

		public virtual void OnFromJson(JObject obj) { }

		public virtual void Serialize(BinaryWriter writer)
		{
			writer.Write(ID);
			writer.Write(MaxStack);
			writer.Write(Usable);
			writer.Write(Count);
		}

		public virtual void Deserialize(BinaryReader reader)
		{
			ID = reader.ReadString();
			MaxStack = reader.ReadInt32();
			Usable = reader.ReadBoolean();
			Count = reader.ReadInt32();
		}
	}
}

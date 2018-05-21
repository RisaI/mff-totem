using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("component_character")]
	public class CharacterComponent : DamagableComponent
	{
		public List<string> TargetedTags = new List<string>();

		protected float _baseSpeed, _baseMaxStamina;
		public float MaxStamina
		{
			get {
				var inv = Parent.GetComponent<InventoryComponent>();
				return _baseMaxStamina * (inv != null ? inv.StaminaMultiplier() : 1); }
		}

		protected float _stamina;
		public float Stamina
		{
			get { return _stamina; }
			set
			{
				_stamina = MathHelper.Clamp(value, 0, MaxStamina);
			}
		}

		public float Speed
		{
			get {
				var inv = Parent.GetComponent<InventoryComponent>();
				return _baseSpeed * (inv != null ? inv.SpeedMultiplier() : 1); }
		}

        public Vector2 Target = Vector2.Zero;

		public CharacterComponent()
		{
			
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			base.ReadFromJson(obj);
			if (obj["speed"] != null)
				_baseSpeed = (float)obj["speed"];
			if (obj["stamina"] != null)
				_baseMaxStamina = _stamina = (float)obj["stamina"];
			if (obj["expReward"] != null)
				_expReward = (int)obj["expReward"];

			if (obj["targetedTags"] != null)
			{
				var tags = (Newtonsoft.Json.Linq.JArray)obj.GetValue("targetedTags");
				for (int i = 0; i < tags.Count; ++i)
				{
					TargetedTags.Add((string)tags[i]);
				}
			}
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			base.WriteToJson(writer);
			writer.WritePropertyName("speed");
			writer.WriteValue(_baseSpeed);
			writer.WritePropertyName("stamina");
			writer.WriteValue(_baseMaxStamina);
			writer.WritePropertyName("expReward");
			writer.WriteValue(_expReward);
			writer.WritePropertyName("targetedTags");
			writer.WriteStartArray(); // Array of components
			TargetedTags.ForEach(t => writer.WriteValue(t));
			writer.WriteEndArray();
		}

		public override void Serialize(System.IO.BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write(_baseSpeed);
			writer.Write(_baseMaxStamina);
			writer.Write(_stamina);
			writer.Write(_expReward);

			// Tags
			writer.Write(TargetedTags.Count);
			TargetedTags.ForEach(t => writer.Write(t));
		}

		public override void Deserialize(System.IO.BinaryReader reader)
		{
			base.Deserialize(reader);
			_baseSpeed = reader.ReadSingle();
			_baseMaxStamina = reader.ReadSingle();
			_stamina = reader.ReadSingle();
			_expReward = reader.ReadInt32();

			// Tags
			var tCount = reader.ReadInt32();
			for (int i = 0; i < tCount; ++i)
			{
				TargetedTags.Add(reader.ReadString());
			}
		}

		public override EntityComponent Clone()
		{
			List<string> tags = new List<string>();
			TargetedTags.ForEach(t => tags.Add(t));
			return new CharacterComponent()
			{
				_hp = _hp,
				_stamina = _stamina,
				_baseMaxHp = _baseMaxHp,
				_baseMaxStamina = _baseMaxStamina,
				_baseSpeed = _baseSpeed,
				_expReward = _expReward,
				TargetedTags = tags
			};
		}

		protected int _expReward = 0;
		protected override void Death(object source)
		{
			base.Death(source);
			var ent = source as Entity;
			if (ent != null)
			{
				var player = ent.GetComponent<PlayerComponent>();
				if (player != null)
				{
					player.TechExperience += _expReward;
				}
			}
		}
	}
}

using System;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("component_character")]
	public class CharacterComponent : DamagableComponent
	{
		float _baseSpeed, _baseMaxStamina;
		public float MaxStamina
		{
			get {
				var inv = Parent.GetComponent<InventoryComponent>();
				return _baseMaxStamina * (inv != null ? inv.StaminaMultiplier() : 1); }
		}

		float _stamina;
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
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			base.WriteToJson(writer);
			writer.WritePropertyName("speed");
			writer.WriteValue(_baseSpeed);
			writer.WritePropertyName("stamina");
			writer.WriteValue(_baseMaxStamina);
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(_baseSpeed);
			writer.Write(_baseMaxStamina);
			writer.Write(_stamina);
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			base.OnDeserialize(reader);
			_baseSpeed = reader.ReadSingle();
			_baseMaxStamina = reader.ReadSingle();
			_stamina = reader.ReadSingle();
		}

		public override EntityComponent Clone()
		{
			return new CharacterComponent() { _hp = _hp, _stamina = _stamina, _baseMaxHp = _baseMaxHp, 
				_baseMaxStamina = _baseMaxStamina, _baseSpeed = _baseSpeed };
		}
	}
}

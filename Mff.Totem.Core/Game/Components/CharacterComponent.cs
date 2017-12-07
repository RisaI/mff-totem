using System;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("character_component")]
	public class CharacterComponent : EntityComponent
	{
		int _baseMaxHp;
		float _baseSpeed, _baseMaxStamina;

		public int MaxHP
		{
			get { return _baseMaxHp; }
		}

		int _hp;
		public int HP
		{
			get { return _hp; }
			set
			{
				_hp = (int)MathHelper.Clamp(value, 0, MaxHP);
			}
		}

		public float MaxStamina
		{
			get { return _baseMaxStamina; }
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
			get { return _baseSpeed; }
		}

		public bool Alive
		{
			get { return _hp > 0; }
		}

		public CharacterComponent()
		{
			
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			base.ReadFromJson(obj);
			if (obj["maxhp"] != null)
				_hp = _baseMaxHp = (int)obj["maxhp"];
			if (obj["speed"] != null)
				_baseSpeed = (float)obj["speed"];
			if (obj["stamina"] != null)
				_baseMaxStamina = _stamina = (float)obj["stamina"];
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			base.WriteToJson(writer);
			writer.WritePropertyName("maxhp");
			writer.WriteValue(_baseMaxHp);
			writer.WritePropertyName("speed");
			writer.WriteValue(_baseSpeed);
			writer.WritePropertyName("stamina");
			writer.WriteValue(_baseMaxStamina);
		}

		public override EntityComponent Clone()
		{
			return new CharacterComponent() { _hp = _hp, _stamina = _stamina, _baseMaxHp = _baseMaxHp, 
				_baseMaxStamina = _baseMaxStamina, _baseSpeed = _baseSpeed };
		}
	}
}

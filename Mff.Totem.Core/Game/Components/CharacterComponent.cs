using System;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("damagable_component")]
	public class DamagableComponent : EntityComponent
	{
		protected int _baseMaxHp;
		public int MaxHP
		{
			get
			{
				var inv = Parent.GetComponent<InventoryComponent>();
				return (int)(_baseMaxHp * (inv != null ? inv.HPMultiplier() : 1));
			}
		}

		protected int _hp;
		public int HP
		{
			get { return _hp; }
			set
			{
				_hp = (int)MathHelper.Clamp(value, 0, MaxHP);
				if (_hp == 0)
					Death();
			}
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			base.ReadFromJson(obj);
			if (obj["maxhp"] != null)
				_hp = _baseMaxHp = (int)obj["maxhp"];
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			base.WriteToJson(writer);
			writer.WritePropertyName("maxhp");
			writer.WriteValue(_baseMaxHp);
		}
	
		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(_baseMaxHp);
			writer.Write(_hp);
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			base.Deserialize(reader);
			_baseMaxHp = reader.ReadInt32();
			_hp = reader.ReadInt32();
		}

		public virtual void Damage(object source, int damage)
		{
			HP -= damage;
		}

		public override EntityComponent Clone()
		{
			return new DamagableComponent() { _baseMaxHp = _baseMaxHp, _hp = _hp };
		}

		protected virtual void Death() { }
	}

	[Serializable("character_component")]
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

		public bool Alive
		{
			get { return _hp > 0; }
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

		public override EntityComponent Clone()
		{
			return new CharacterComponent() { _hp = _hp, _stamina = _stamina, _baseMaxHp = _baseMaxHp, 
				_baseMaxStamina = _baseMaxStamina, _baseSpeed = _baseSpeed };
		}
	}

	[Serializable("tree_component")]
	public class TreeComponent : DamagableComponent, IUpdatable
	{
		bool _falling;
		public void Update(GameTime gameTime)
		{
			var body = Parent.GetComponent<BodyComponent>();
			if (_falling && body != null)
			{
				body.Rotation += (float)gameTime.ElapsedGameTime.TotalSeconds * MathHelper.PiOver4;
				if (body.Rotation > MathHelper.PiOver2)
				{
					Parent.Remove = true;
				}
			}
		}

		protected override void Death()
		{
			base.Death();
			_falling = true;
		}

		public override EntityComponent Clone()
		{
			return new TreeComponent() { _baseMaxHp = _baseMaxHp, _hp = _hp };
		}
	}
}

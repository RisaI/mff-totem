using System;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("component_damagable")]
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
				if (_hp > 0 && value <= 0)
					Death();
				_hp = (int)MathHelper.Clamp(value, 0, MaxHP);
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

	[Serializable("component_tree")]
	public class TreeComponent : DamagableComponent, IUpdatable
	{
		Rectangle leafArea;
		float _shake;
		bool _falling;

		public Rectangle LeafArea
		{
			get
			{
				var pos = Parent.Position.HasValue ? Parent.Position.Value : Vector2.Zero;
				return new Rectangle((int)pos.X + leafArea.X, (int)pos.Y + leafArea.Y, leafArea.Width, leafArea.Height);
			}
		}

		public void Update(GameTime gameTime)
		{
			var body = Parent.GetComponent<BodyComponent>();
			if (body != null)
			{
				if (_falling)
				{
					body.Rotation += (float)gameTime.ElapsedGameTime.TotalSeconds * Parent.World.TimeScale * MathHelper.PiOver2;
					if (body.Rotation > MathHelper.PiOver2)
					{
						Parent.Remove = true;
					}
				}
				else if (_shake > 0)
				{
					_shake -= (float)gameTime.ElapsedGameTime.TotalSeconds * Parent.World.TimeScale;
					body.Rotation = (float)(Math.Sin(_shake * 8 * MathHelper.Pi) * MathHelper.PiOver4 / 8);
					if (TotemGame.Random.NextDouble() < 0.25)
					{
						World.SpawnParticle("leaf", LeafArea.RandomPoint());
					}
				}
				else
				{
					body.Rotation = 0;
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
			return new TreeComponent() { _baseMaxHp = _baseMaxHp, _hp = _hp, leafArea = leafArea };
		}

		public override void Damage(object source, int damage)
		{
			_shake = 0.25f;
			base.Damage(source, damage);
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			base.ReadFromJson(obj);
			if (obj["leaf_area"] != null)
				leafArea = Helper.JTokenToRectangle(obj["leaf_area"]);
		}
	}
}

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
					Death(null);
				_hp = (int)MathHelper.Clamp(value, 0, MaxHP);
			}
		}

		public bool Alive
		{
			get { 
				return _hp > 0; }
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

		public override void Serialize(System.IO.BinaryWriter writer)
		{
			writer.Write(_baseMaxHp);
			writer.Write(_hp);
		}

		public override void Deserialize(System.IO.BinaryReader reader)
		{
			_baseMaxHp = reader.ReadInt32();
			_hp = reader.ReadInt32();
		}

		public virtual void Damage(object source, int damage)
		{
			if (_hp > 0 && damage >= _hp)
				Death(source);
			_hp = (int)MathHelper.Clamp(_hp - damage, 0, MaxHP);
		}

		public override EntityComponent Clone()
		{
			return new DamagableComponent() { _baseMaxHp = _baseMaxHp, _hp = _hp };
		}

		protected virtual void Death(object source) { }
	}
}

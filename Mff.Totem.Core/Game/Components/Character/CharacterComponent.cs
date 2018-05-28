using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("component_character")]
	public class CharacterComponent : DamagableComponent, IUpdatable
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

		public bool IsCrouching, IsJumping, IsWalking;

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

		List<CharacterAction> Actions = new List<CharacterAction>(8);

		public void Update(GameTime gameTime)
		{
			if (Actions.Count > 0)
			{
				Actions[0].Update(gameTime);
				if (Actions[0].Remove)
				{
					Actions.RemoveAt(0);
					if (Actions.Count > 0)
						Actions[0].Start(this);
				}
			}
		}

		public bool QueueEmpty
		{
			get { return Actions.Count <= 0; }
		}

		public void AddToQueue(CharacterAction action)
		{
			if (Actions.Count < 8)
			{
				Actions.Add(action);
				if (Actions.Count == 1)
					action.Start(this);
			}
		}

		public bool QueueContains<T>() where T : CharacterAction
		{
			return Actions.Any(a => a is T);
		}
	}

	public abstract class CharacterAction
	{
		public CharacterComponent ParentComponent;
		public Entity Parent
		{
			get { return ParentComponent?.Parent; }
		}

		public GameWorld World
		{
			get { return Parent?.World; }
		}

		public void Start(CharacterComponent parent)
		{
			ParentComponent = parent;
			Initialize();
		}

		protected abstract void Initialize();
		public abstract void Update(GameTime gameTime);
		public abstract bool Remove
		{
			get;
		}
	}
}

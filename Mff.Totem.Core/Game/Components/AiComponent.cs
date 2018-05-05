using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Mff.Totem.Core;
using Mff.Totem.Ai;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	[Serializable("component_ai")]
	public class AiComponent : EntityComponent, IUpdatable
	{
		public List<AiElement> Elements;
		public bool StopMovementOnDeath = true;

		public AiComponent() : this(new List<AiElement>())
		{
			
		}

		public AiComponent(List<AiElement> elems)
		{
			Elements = elems;
		}

		public AiComponent(params AiElement[] elems) : this(elems.ToList())
		{
			
		}

		public override EntityComponent Clone()
		{
			List<AiElement> c = new List<AiElement>();
			Elements.ForEach(e => c.Add(e.Clone()));
			return new AiComponent(c) { StopMovementOnDeath = StopMovementOnDeath };
		}

		protected override void WriteToJson(JsonWriter writer)
		{
			writer.WritePropertyName("elements");
			writer.WriteStartArray();
			Elements.ForEach(el => DeserializationRegister.ObjectToJson(writer, el));
			writer.WriteEndArray();
			writer.WritePropertyName("stopMovementOnDeath");
			writer.WriteValue(StopMovementOnDeath);
		}

		protected override void ReadFromJson(JObject obj)
		{
			if (obj["elements"] != null)
			{
				var elemArray = (JArray)obj["elements"];
				for (int i = 0; i < elemArray.Count; ++i)
				{
					Elements.Add(DeserializationRegister.ObjectFromJson<AiElement>((JObject)elemArray[i]));
				}
			}
			if (obj["stopMovementOnDeath"] != null)
			{
				StopMovementOnDeath = (bool)obj["stopMovementOnDeath"];
			}
		}

		protected override void OnSerialize(BinaryWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(StopMovementOnDeath);
			writer.Write(Elements.Count);
			for (int i = 0; i < Elements.Count; ++i)
			{
				DeserializationRegister.WriteObject(writer, Elements[i]);
			}
		}

		protected override void OnDeserialize(BinaryReader reader)
		{
			base.OnDeserialize(reader);
			StopMovementOnDeath = reader.ReadBoolean();
			int a = reader.ReadInt32();
			for (int i = 0; i < a; ++i)
			{
				Elements.Add(DeserializationRegister.ReadObject<AiElement>(reader));
			}
		}

		public void Update(GameTime gameTime)
		{
			var d = Parent.GetComponent<DamagableComponent>();
			if (d != null && !d.Alive)
			{
				if (StopMovementOnDeath)
				{
					var body = Parent.GetComponent<BodyComponent>();
					if (body != null)
					{
						body.Move(Vector2.Zero);
					}
				}
				return;
			}

			for (int i = 0; i < Elements.Count; ++i)
			{
				if (Elements[i].Stimulated(Parent))
				{
					Elements[i].Update(Parent, gameTime);
					break;
				}
			}
		}
	}
}

namespace Mff.Totem.Ai
{
	public abstract class AiElement : ICloneable<AiElement>, IJsonSerializable, ISerializable
	{
		public abstract AiElement Clone();

		public abstract void Serialize(BinaryWriter writer);
		public abstract void Deserialize(BinaryReader reader);

		public abstract void FromJson(JObject obj);
		public abstract void ToJson(JsonWriter writer);

		public abstract bool Stimulated(Entity ent);
		public abstract void Update(Entity ent, GameTime gameTime);
	}

	[Core.Serializable("ai_attack")]
	public class AttackAiElement : AiElement
	{
		public float Range;
		public List<string> TargetedTags;
		public Entity Target;

		public AttackAiElement()
		{
			TargetedTags = new List<string>();
		}

		public override AiElement Clone()
		{
			List<string> tt = new List<string>();
			TargetedTags.ForEach(t => tt.Add(t));
			return new AttackAiElement() { Range = Range, TargetedTags = tt };
		}

		public override void Serialize(BinaryWriter writer)
		{
			writer.Write(Range);
			writer.Write(TargetedTags.Count);
			TargetedTags.ForEach(tt => writer.Write(tt));
		}

		public override void Deserialize(BinaryReader reader)
		{
			Range = reader.ReadSingle();
			var tcount = reader.ReadInt32();
			for (int i = 0; i < tcount; ++i)
			{
				TargetedTags.Add(reader.ReadString());
			}
		}

		public override void ToJson(JsonWriter writer)
		{
			writer.WritePropertyName("range");
			writer.WriteValue(Range);
			writer.WriteEnd();

			writer.WritePropertyName("targeted");
			writer.WriteStartArray();
			TargetedTags.ForEach(tt => writer.WriteValue(tt));
			writer.WriteEndArray();
		}

		public override void FromJson(JObject obj)
		{
			if (obj["range"] != null)
				Range = (float)obj["range"];
			if (obj["targeted"] != null)
			{
				var tarray = (JArray)obj["targeted"];
				for (int i = 0; i < tarray.Count; ++i)
					TargetedTags.Add((string)tarray[i]);
			}
		}

		public override bool Stimulated(Entity ent)
		{
			var pos = ent.Position;
			if (Target != null)
			{
				var tpos = Target.Position;
				if (tpos.HasValue && pos.HasValue && (Math.Abs(pos.Value.X - tpos.Value.X) > Range || Math.Abs(pos.Value.Y - tpos.Value.Y) > Range))
					Target = null;
				else
					return true;
			}

			if (pos.HasValue)
			{
				Entity candidate = null;
				ent.World.EntitiesInRange(pos.Value, Range, (eval) =>
				{
					if (eval == ent || !eval.Tags.Any(t => TargetedTags.Contains(t)))
						return true;

					if (candidate == null || (pos.Value - eval.Position.Value).LengthSquared() < (pos.Value - candidate.Position.Value).LengthSquared())
						candidate = eval;
					
					return true;
				});

				if (candidate != null)
					Target = candidate;
			}

			return Target != null;
		}

		public override void Update(Entity ent, GameTime gameTime)
		{
			var body = ent.GetComponent<BodyComponent>();
			if (body != null)
			{
				var dir = Target.Position.Value - body.Position;
				dir.Normalize();
				dir.Y = 0;
				dir.X = Math.Sign(dir.X) * 100;
				body.Move(dir);
			}	
		}
	}
}

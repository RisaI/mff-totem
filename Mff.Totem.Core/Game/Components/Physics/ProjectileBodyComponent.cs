using System;
using System.Linq;

using Microsoft.Xna.Framework;
using Physics2D.Collision;
using Physics2D.Dynamics;
using Physics2D.Dynamics.Joints;
//using Physics2D.Factories;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Mff.Totem.Core
{
	[Serializable("component_projectile_body")]
	public class ProjectileBody : BodyComponent, IUpdatable
	{
		public Guid OwnerID;

		public override Vector2 Position
		{
			get;
			set;
		}

		public override Vector2 LegPosition
		{
			get
			{
				return Position;
			}
			set { Position = value; }
		}

		private float _lastRot = 0;
		public override float Rotation
		{
			get
			{
				if (LinearVelocity != Vector2.Zero)
					_lastRot = -Helper.DirectionToAngle(LinearVelocity);
				return _lastRot;
			}
			set
			{
				_lastRot = value;
				LinearVelocity = LinearVelocity.Length() * Helper.AngleToDirection(_lastRot);
			}
		}

		public override Vector2 LinearVelocity
		{
			get;
			set;
		}

		public bool Gravity = false;
		public float Friction = 0;
		public int Damage;

		public override Rectangle BoundingBox
		{
			get
			{
				return new Rectangle((int)Position.X, (int)Position.Y, 1, 1);
			}

			set
			{
				Position = new Vector2(value.X, value.Y);
			}
		}

		public ProjectileBody()
		{

		}

		public void Update(GameTime gameTime)
		{
			if (LinearVelocity.LengthSquared() >= 1e-7)
			{
				var step = LinearVelocity * World.TimeScale * (float)gameTime.ElapsedGameTime.TotalSeconds;
				bool stopped = false;
				var rf = this;
				World.Physics.RayCast((Fixture arg1, Vector2 arg2, Vector2 arg3, float arg4) =>
				{
					if (arg1.Body.Tag is Terrain)
					{
						Position = arg2 * 64f;
						LinearVelocity = Vector2.Zero;
						stopped = true;
					}
					else if (arg1.Body.Tag is Entity)
					{
						var ent = arg1.Body.Tag as Entity;
						if (ent.Tags.Any(t => TargetedTags.Contains(t)))
						{
							var ch = ent.GetComponent<CharacterComponent>();
							if (ch != null)
							{
								ch.Damage(World.GetEntity(OwnerID), Damage);
								Position = arg2 * 64f;
								LinearVelocity = Vector2.Zero;
								stopped = true;
								Parent.AddComponent(new BindedBodyComponent().BindTo(ent, Position, Rotation));
								rf.Remove = true;
								return 0;
							}
						}
					}
					return arg4;
				}, Position / 64f, (Position + step) / 64f);
				if (!stopped)
					Position += step;
				else
					Gravity = false;
			}
			if (Gravity)
				LinearVelocity += new Vector2(0, World.Physics.Gravity.Y * 64f * 
				                              (float)gameTime.ElapsedGameTime.TotalSeconds * World.TimeScale);
			if (Friction > 0)
				LinearVelocity /= Friction;
		}

		public override void Move(Vector2 direction)
		{
			return;
		}

		public override EntityComponent Clone()
		{
			List<string> tags = new List<string>();
			TargetedTags.ForEach(t => tags.Add(t));
			return new ProjectileBody()
			{
				Rotation = Rotation,
				Position = Position,
				Gravity = Gravity,
				Friction = Friction,
				TargetedTags = tags
			};
		}

		List<string> TargetedTags = new List<string>();
		public void SetProjectileData(Entity owner, Vector2 speed, int damage)
		{
			OwnerID = owner.UID;
			Position = owner.GetComponent<BodyComponent>().Position;
			LinearVelocity = speed;
			Damage = damage;

			var ch = owner.GetComponent<CharacterComponent>();
			if (ch != null)
				ch.TargetedTags.ForEach(t => TargetedTags.Add(t));
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			if (obj["gravity"] != null)
				Gravity = (bool)obj["gravity"];
			if (obj["friction"] != null)
				Friction = (float)obj["friction"];

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
			writer.WritePropertyName("gravity");
			writer.WriteValue(Gravity);
			writer.WritePropertyName("friction");
			writer.WriteValue(Friction);
			writer.WritePropertyName("targetedTags");
			writer.WriteStartArray(); // Array of components
			TargetedTags.ForEach(t => writer.WriteValue(t));
			writer.WriteEndArray();
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			writer.Write(Position);
			writer.Write(Rotation);
			writer.Write(LinearVelocity);

			writer.Write(Gravity);
			writer.Write(Friction);
			writer.Write(OwnerID);
			writer.Write(Damage);

			// Tags
			writer.Write(TargetedTags.Count);
			TargetedTags.ForEach(t => writer.Write(t));
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			Position = reader.ReadVector2();
			Rotation = reader.ReadSingle();
			LinearVelocity = reader.ReadVector2();

			Gravity = reader.ReadBoolean();
			Friction = reader.ReadSingle();
			OwnerID = reader.ReadGuid();
			Damage = reader.ReadInt32();

			// Tags
			var tCount = reader.ReadInt32();
			for (int i = 0; i < tCount; ++i)
			{
				TargetedTags.Add(reader.ReadString());
			}
		}
	}
}

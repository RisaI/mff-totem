using System;

using Microsoft.Xna.Framework;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework.Graphics;

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

		public Vector2 LinearVelocity;

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
				World.Physics.RayCast((Fixture arg1, Vector2 arg2, Vector2 arg3, float arg4) =>
				{
					if (arg1.Body.UserData is Terrain)
					{
						Position = arg2 * 64f;
						LinearVelocity = Vector2.Zero;
						stopped = true;
					}
					return arg4;
				}, Position / 64f, (Position + step) / 64f);
				if (!stopped)
					Position += step;
				else
					Gravity = false;
			}
			if (Gravity)
				LinearVelocity.Y += World.Physics.Gravity.Y * 64f * 
					(float)gameTime.ElapsedGameTime.TotalSeconds * World.TimeScale;
			if (Friction > 0)
				LinearVelocity /= Friction;
		}

		public override void Move(Vector2 direction)
		{
			return;
		}

		public override EntityComponent Clone()
		{
			return new ProjectileBody()
			{
				Rotation = Rotation,
				Position = Position,
				Gravity = Gravity,
				Friction = Friction
			};
		}

		public void SetProjectileData(Entity owner, Vector2 speed)
		{
			OwnerID = owner.UID;
			Position = owner.GetComponent<BodyComponent>().Position;
			LinearVelocity = speed;
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			if (obj["gravity"] != null)
				Gravity = (bool)obj["gravity"];
			if (obj["friction"] != null)
				Friction = (float)obj["friction"];
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			writer.WritePropertyName("gravity");
			writer.WriteValue(Gravity);
			writer.WritePropertyName("friction");
			writer.WriteValue(Friction);
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			writer.Write(Position);
			writer.Write(Rotation);
			writer.Write(LinearVelocity);

			writer.Write(Gravity);
			writer.Write(Friction);
			writer.Write(OwnerID);
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			Position = reader.ReadVector2();
			Rotation = reader.ReadSingle();
			LinearVelocity = reader.ReadVector2();

			Gravity = reader.ReadBoolean();
			Friction = reader.ReadSingle();
			OwnerID = reader.ReadGuid();
		}
	}
}

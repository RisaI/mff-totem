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
	public class ProjectileBody : BodyComponent
	{
		public Body MainBody;
		public float Radius = 0.01f;
		private Vector2? FuturePosition;

		public Guid OwnerID;

		public ProjectileBody()
		{

		}

		public ProjectileBody(float radius)
		{
			Radius = radius;
		}

		public override Vector2 Position
		{
			get
			{
				return MainBody != null ? MainBody.Position * 64f : (FuturePosition != null ? (Vector2)FuturePosition : Vector2.Zero);
			}
			set
			{
				if (MainBody != null)
				{
					MainBody.Position = value / 64f;
				}
				else
					FuturePosition = value;
			}
		}

		public override Vector2 LegPosition
		{
			get
			{
				return Position;
			}
			set { Position = value; }
		}

		public override float Rotation
		{
			get
			{
				return MainBody != null ? MainBody.Rotation : 0;
			}
			set
			{
				if (MainBody != null)
					MainBody.Rotation = value;
				else
					spawnInfo.FRotation = value;
			}
		}

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

		void CreateBody()
		{
			MainBody = BodyFactory.CreateCircle(World.Physics, Radius, 1f, Vector2.Zero, BodyType.Dynamic, Parent);
			//MainBody.IsBullet = true;

			MainBody.OnCollision += (fixtureA, fixtureB, contact) =>
			{
				if (fixtureB.Body.UserData is Terrain)
				{
					MainBody.BodyType = BodyType.Static;
					return true;
				}
				else if (fixtureB.Body.UserData is Entity)
				{

				}

				return false;
			};

			if (FuturePosition != null)
				Position = (Vector2)FuturePosition;

			MainBody.LinearVelocity = spawnInfo.VVelocity;
			MainBody.Rotation = spawnInfo.FRotation;
		}

		protected override void OnEntityAttach(Entity entity)
		{
			if (MainBody != null)
			{
				MainBody.UserData = entity;
			}
		}

		public override void Initialize()
		{
			if (MainBody != null)
			{
				FuturePosition = MainBody.Position * 64f;
				MainBody.Dispose();
			}
			CreateBody();
		}

		public override void Destroy()
		{
			if (MainBody != null)
			{
				World.Physics.RemoveBody(MainBody);
			}
		}

		public override void Move(Vector2 direction)
		{
			return;
		}

		public override EntityComponent Clone()
		{
			return new ProjectileBody(Radius) { Rotation = Rotation, Position = Position };
		}

		public void SetProjectileData(Entity owner, Vector2 direction)
		{
			OwnerID = owner.UID;
			Position = owner.GetComponent<BodyComponent>().Position;
			Rotation = -Helper.DirectionToAngle(direction);

			if (MainBody != null)
				MainBody.LinearVelocity = direction / 64f;
			else
				spawnInfo.VVelocity = direction / 64f;

		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			if (obj["radius"] != null)
				Radius = (float)obj["radius"];
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			writer.WritePropertyName("radius");
			writer.WriteValue(Radius);
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			writer.Write(Radius);
			writer.Write(Position);

			writer.Write(MainBody.LinearVelocity);
			writer.Write(MainBody.Rotation);
		}

		private BodySpawnInfo spawnInfo;
		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			Radius = reader.ReadSingle();
			Position = reader.ReadVector2();

			//TODO: Load linear velocities
			spawnInfo = new BodySpawnInfo(reader.ReadVector2(), reader.ReadSingle());
		}

		struct BodySpawnInfo
		{
			public Vector2 VVelocity;
			public float FRotation;

			public BodySpawnInfo(Vector2 vv, float frot)
			{
				VVelocity = vv;
				FRotation = frot;
			}
		}
	}
}

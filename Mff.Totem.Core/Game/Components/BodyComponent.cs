using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public abstract class BodyComponent : EntityComponent
	{
		public abstract Vector2 Position
		{
			get;
			set;
		}

		public abstract Vector2 LegPosition
		{
			get;
			set;
		}

		public abstract float Rotation
		{
			get;
			set;
		}

		public abstract Rectangle BoundingBox
		{
			get;
			set;
		}

		public abstract void Move(Vector2 direction);
	}

	[Serializable("humanoid_body")]
	public class HumanoidBody : BodyComponent
	{
		const float IDLE_FRICTION = 16f;

		public Body MainBody, ControllerBody;
		public RevoluteJoint BodyJoint;

		public float Width = 0.5f, Height = 1.25f;

		private Vector2? FuturePosition;
		private bool CanJump;
		private Fixture LastTerrainFixture;

		public HumanoidBody()
		{

		}

		public HumanoidBody(float width, float height)
		{
			Width = width;
			Height = height;
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
					ControllerBody.Position = value / 64f + new Vector2(0, Height / 2);
				}
				else
					FuturePosition = value;
			}
		}

		public override Vector2 LegPosition
		{
			get
			{
				return Position + new Vector2(0, 32f * (Height + Width / 2));
			}
			set { Position = value - new Vector2(0, 32f * (Height + Width)); }
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
			}
		}

		public override Rectangle BoundingBox
		{
			get
			{
				int x = (int)((MainBody.Position.X - Width / 2) * 64f);
				int y = (int)(LegPosition.Y - Height * 64f);
				return new Rectangle(x, y, (int)(Width * 64f), (int)(Height * 64f));
			}

			set
			{
				return;
			}
		}

		void CreateBody()
		{
			MainBody = BodyFactory.CreateRectangle(World.Physics, Width, Height - Width / 2, 1f, Vector2.Zero, 0, BodyType.Dynamic, Parent);
			MainBody.FixedRotation = true;
			MainBody.BodyType = BodyType.Dynamic;

			ControllerBody = BodyFactory.CreateCircle(World.Physics, Width / 2, 1f, new Vector2(0, Height / 2 - Width / 4), BodyType.Dynamic, Parent);
			ControllerBody.BodyType = BodyType.Dynamic;
			ControllerBody.Friction = IDLE_FRICTION;

			ControllerBody.OnCollision += (fixtureA, fixtureB, contact) =>
			{
				if (fixtureB.UserData is Terrain)
				{
					CanJump = true;
					LastTerrainFixture = fixtureB;
				}

				return true;
			};
			ControllerBody.OnSeparation += (fixtureA, fixtureB) =>
			{
				/*if (fixtureB == LastTerrainFixture && fixtureB.UserData is Terrain)
				{
					CanJump = false;
				}*/
			};

			BodyJoint = JointFactory.CreateRevoluteJoint(World.Physics, MainBody, ControllerBody, Vector2.Zero);
			BodyJoint.MotorEnabled = true;
			BodyJoint.MaxMotorTorque = 0;
			BodyJoint.LimitEnabled = true;

			if (FuturePosition != null)
				Position = (Vector2)FuturePosition;

			if (spawnInfo != null)
			{
				MainBody.LinearVelocity = spawnInfo.MVelocity;
				ControllerBody.LinearVelocity = spawnInfo.CVelocity;
				ControllerBody.AngularVelocity = spawnInfo.CAngVelocity;
				ControllerBody.Rotation = spawnInfo.CRot;
				spawnInfo = null;
			}
		}

		protected override void OnEntityAttach(Entity entity)
		{
			if (MainBody != null)
			{
				MainBody.UserData = entity;
				ControllerBody.UserData = entity;
				BodyJoint.UserData = entity;
			}
		}

		public override void Initialize()
		{
			if (MainBody != null)
			{
				FuturePosition = MainBody.Position * 64f;
				MainBody.Dispose();
				ControllerBody.Dispose();
			}
			CreateBody();
		}

		public override void Destroy()
		{
			if (MainBody != null)
			{
				World.Physics.RemoveBody(MainBody);
				World.Physics.RemoveBody(ControllerBody);
			}
		}

		public override void Move(Vector2 direction)
		{
			// Convert the horizontal part from pixel per second to radians per second
			var horizontal = (direction.X / 32f) / Width;
			if (Math.Abs(horizontal) > 0.5f)
			{
				BodyJoint.LimitEnabled = false;
				BodyJoint.MaxMotorTorque = float.MaxValue;
				BodyJoint.MotorSpeed = horizontal;
			}
			else
			{
				BodyJoint.LimitEnabled = true;
				BodyJoint.MaxMotorTorque = 0;
			}

			// Jumping
			if (direction.Y < -0.5f && CanJump)
			{
				var jumpDir = new Vector2(0, (float)-Math.Sqrt(-direction.Y * World.Physics.Gravity.Y / 32f));
				MainBody.LinearVelocity += jumpDir;
				ControllerBody.LinearVelocity += jumpDir;
				CanJump = false;
			}
		}

		public override EntityComponent Clone()
		{
			return new HumanoidBody(Width, Height) { Rotation = Rotation, Position = Position };
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			if (obj["width"] != null)
				Width = (float)obj["width"];
			if (obj["height"] != null)
				Height = (float)obj["height"];
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			writer.WritePropertyName("width");
			writer.WriteValue(Width);
			writer.WritePropertyName("height");
			writer.WriteValue(Height);
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			writer.Write(Width);
			writer.Write(Height);
			writer.Write(Position);

			writer.Write(MainBody.LinearVelocity);
			writer.Write(ControllerBody.LinearVelocity);
			writer.Write(ControllerBody.AngularVelocity);
			writer.Write(ControllerBody.Rotation);
		}

		private BodySpawnInfo spawnInfo;
		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			Width = reader.ReadSingle();
			Height = reader.ReadSingle();
			Position = reader.ReadVector2();

			//TODO: Load linear velocities
			spawnInfo = new BodySpawnInfo(reader.ReadVector2(), reader.ReadVector2(), reader.ReadSingle(), reader.ReadSingle());
		}

		class BodySpawnInfo
		{
			public Vector2 MVelocity, CVelocity;
			public float CAngVelocity, CRot;

			public BodySpawnInfo(Vector2 mv, Vector2 cv, float cang, float crot)
			{
				MVelocity = mv;
				CVelocity = cv;
				CAngVelocity = cang;
				CRot = crot;
			}
		}
	}

	[Serializable("static_body")]
	public class StaticBody : BodyComponent
	{
		public override Vector2 LegPosition
		{
			get { return Position; }
			set { Position = value; }
		}

		public override Vector2 Position
		{
			get;
			set;
		}

		public override float Rotation
		{
			get;
			set;
		}

		public override Rectangle BoundingBox
		{
			get
			{
				return new Rectangle((int)(Position.X - Size.X /2), (int)(Position.Y - Size.Y), (int)Size.X, (int)Size.Y);
			}
			set
			{
				Position = value.Center.ToVector2() + new Vector2(0, value.Height / 2);
				Size = new Vector2(value.Width, value.Height);
			}
		}

		public Vector2 Size;

		public override EntityComponent Clone()
		{
			return new StaticBody() { Position = Position, Rotation = Rotation, Size = Size };
		}

		public override void Move(Vector2 direction)
		{
			Position += direction;
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			base.ReadFromJson(obj);
			if (obj["size"] != null)
				Size = Helper.JTokenToVector2(obj["size"]);
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			base.WriteToJson(writer);
			writer.WritePropertyName("size");
			writer.WriteValue(Size);
			writer.WriteEnd();
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(Position);
			writer.Write(Rotation);
			writer.Write(Size);
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			base.OnDeserialize(reader);
			Position = reader.ReadVector2();
			Rotation = reader.ReadSingle();
			Size = reader.ReadVector2();
		}
	}


	[Serializable("projectile_body")]
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

            MainBody.OnCollision += (fixtureA, fixtureB, contact) => {
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

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

		public abstract void Move(Vector2 direction);
	}

	[Serializable("humanoid_body")]
	public class HumanoidBody : BodyComponent
	{
		const float IDLE_FRICTION = 8f;

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
				return Position + new Vector2(0, 32f * (Height + Width));
			}
			set { Position = value - new Vector2(0, 32f * (Height + Height)); }
		}

		void CreateBody()
		{
			MainBody = BodyFactory.CreateRectangle(World.Physics, Width, Height, 1f, Vector2.Zero, 0, BodyType.Dynamic, Parent);
			MainBody.FixedRotation = true;
			MainBody.BodyType = BodyType.Dynamic;

			ControllerBody = BodyFactory.CreateCircle(World.Physics, Width / 2, 1f, new Vector2(0, Height / 2), BodyType.Dynamic, Parent);
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
			return new HumanoidBody(Width, Height);
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
}

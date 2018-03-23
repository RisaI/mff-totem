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
	[Serializable("component_humanoid_body")]
	public class HumanoidBody : BodyComponent
	{
		const float IDLE_FRICTION = 16f;

		public Body MainBody, ControllerBody;
		public RevoluteJoint BodyJoint;

		public float Width = 0.5f, Height = 1.25f;

		private Vector2? FuturePosition;
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
			if (direction.Y < -0.5f && JumpAvailable())
			{
				var jumpDir = new Vector2(0, (float)-Math.Sqrt(-direction.Y * World.Physics.Gravity.Y / 32f));
				MainBody.LinearVelocity += jumpDir;
				ControllerBody.LinearVelocity += jumpDir;
			}
		}

		public bool JumpAvailable()
		{
			Vector2 lg = LegPosition / 64f;
			Vector2[] positions = new Vector2[] { lg, lg - new Vector2(Width / 2, 0), lg + new Vector2(Width / 2, 0) };
			for (int i = 0; i < positions.Length; ++i)
			{
				bool jumpAvailable = false;

				World.Physics.RayCast((Fixture arg1, Vector2 arg2, Vector2 arg3, float arg4) =>
				{
					if (arg1.Body.UserData is Terrain)
					{
						jumpAvailable = true;
						return 0;
					}
					return arg4;
				}, positions[i], positions[i] + new Vector2(0, 3 / 32f));

				if (jumpAvailable)
					return true;
			}
			return false;
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
}

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;

namespace Mff.Totem.Core
{
	public abstract class BodyComponent : EntityComponent
	{
		public abstract Vector2 Position
		{
			get;
			set;
		}

		public abstract void Move(Vector2 direction);
	}

	public class HumanoidBody : BodyComponent
	{
		const float BODY_WIDTH = 0.5f, BODY_HEIGHT = 0.25f;

		public Body MainBody, ControllerBody;
		public RevoluteJoint BodyJoint;

		public override Vector2 Position
		{
			get
			{
				return MainBody != null ? MainBody.Position * 64f : Vector2.Zero;
			}
			set
			{
				if (MainBody != null)
					MainBody.Position = value / 64f;
			}
		}

		void CreateBody()
		{
			MainBody = BodyFactory.CreateRectangle(World.Physics, BODY_WIDTH, BODY_HEIGHT, 1f, Parent);
			MainBody.FixedRotation = true;
			ControllerBody = BodyFactory.CreateCircle(World.Physics, BODY_WIDTH / 2, 1f, Parent);
			ControllerBody.Position = new Vector2(0, BODY_HEIGHT / 2);
			BodyJoint = JointFactory.CreateRevoluteJoint(World.Physics, MainBody, ControllerBody, Vector2.Zero);
			BodyJoint.MotorEnabled = true;
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
			BodyJoint.MotorSpeed = direction.X;
		}
	}
}

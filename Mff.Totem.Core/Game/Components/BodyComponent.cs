﻿using System;
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

	[Serializable("humanoid_body")]
	public class HumanoidBody : BodyComponent
	{
		public Body MainBody, ControllerBody;
		public RevoluteJoint BodyJoint;

		public float Width = 0.5f, Height = 1.25f;

		private Vector2? FuturePosition = null;

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
				return MainBody != null ? MainBody.Position * 64f : Vector2.Zero;
			}
			set
			{
				if (MainBody != null)
					MainBody.Position = value / 64f;
				else
					FuturePosition = value;
			}
		}

		void CreateBody()
		{
			MainBody = BodyFactory.CreateRectangle(World.Physics, Width, Height, 1f, Parent);
			MainBody.FixedRotation = true;
			MainBody.BodyType = BodyType.Dynamic;

			ControllerBody = BodyFactory.CreateCircle(World.Physics, Width / 2, 1f, Parent);
			ControllerBody.Position = new Vector2(0, Height / 2);
			ControllerBody.BodyType = BodyType.Dynamic;

			BodyJoint = JointFactory.CreateRevoluteJoint(World.Physics, MainBody, ControllerBody, Vector2.Zero);
			BodyJoint.MotorEnabled = true;

			if (FuturePosition != null)
				MainBody.Position = (Vector2)FuturePosition;
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
	}
}

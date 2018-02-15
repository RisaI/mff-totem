﻿using System;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("component_prop_body")]
	public class PropBodyComponent : BodyComponent
	{
		public bool FixedRotation;

		// In PX!
		public float Width, Height, Friction;

		public Body Body;

		private Vector2 _valPos;
		public override Vector2 Position
		{

			get
			{
				if (Body != null)
					return Body.Position * 64f;
				else
					return _valPos;
			}

			set
			{
				if (Body != null)
					Body.Position = value / 64f;
				else
					_valPos = value;
			}
		}

		public override Vector2 LegPosition
		{
			get
			{
				return Position;
			}

			set
			{
				Position = value;
			}
		}

		private float _valRot;
		public override float Rotation
		{
			get
			{
				if (Body != null)
					return Body.Rotation;
				else
					return _valRot;
			}

			set
			{
				if (Body != null)
					Body.Rotation = value;
				else
					_valRot = value;
			}
		}

		public override Rectangle BoundingBox
		{
			get
			{
				return new Rectangle((int)(Position.X - Width / 2),
				                     (int)(Position.Y - Height / 2),
				                     (int)Width, (int)Height);
			}

			set
			{
				Position = value.Center.ToVector2();
				Width = value.Width;
				Height = value.Height;
			}
		}

		public PropBodyComponent()
		{
		}

		public override void Initialize()
		{
			base.Initialize();
			CreateBody();
		}

		void CreateBody()
		{
			if (Body != null)
				Parent.World.Physics.RemoveBody(Body);

			Body = BodyFactory.CreateRectangle(Parent.World.Physics, 
			                                   Width / 64f, Height / 64f, 
			                                   1, _valPos / 64f, _valRot, 
			                                   BodyType.Dynamic, Parent);
			Body.FixedRotation = FixedRotation;
			Body.Friction = Friction;
			Body.OnCollision += (fixtureA, fixtureB, contact) =>
			{
				return fixtureB.UserData is Terrain || (fixtureB.UserData is Entity && (fixtureB.UserData as Entity).GetComponent<PropBodyComponent>() != null);
			};
		}

		public override EntityComponent Clone()
		{
			return new PropBodyComponent()
			{
				Width = Width,
				Height = Height,
				FixedRotation = FixedRotation,
				Friction = Friction,
				_valPos = _valPos,
				_valRot = _valRot
			};
		}

		public override void Destroy()
		{
			if (Body != null)
			{
				Parent.World.Physics.RemoveBody(Body);
				Body = null;
			}
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			base.ReadFromJson(obj);
			if (obj["width"] != null)
				Width = (float)obj["width"];
			if (obj["height"] != null)
				Height = (float)obj["height"];
			if (obj["friction"] != null)
				Friction = (float)obj["friction"];
			if (obj["fixedrotation"] != null)
				FixedRotation = (bool)obj["fixedrotation"];
		}

		public override void Move(Vector2 direction)
		{
			return;
		}
	}
}
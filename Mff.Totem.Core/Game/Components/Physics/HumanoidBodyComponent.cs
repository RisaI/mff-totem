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
	public class HumanoidBody : BodyComponent, IUpdatable
	{
		public float Width = 32f, Height = 80f;

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
				return LegPosition - new Vector2(0, Height / 2);
			}
			set
			{
				LegPosition = value + new Vector2(0, Height / 2);
			}
		}

		public override Vector2 LegPosition
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
				return new Rectangle(
					(int)(LegPosition.X - Width / 2),
					(int)(LegPosition.Y - Height),
					(int)Width, (int)Height);
			}

			set
			{
				return;
			}
		}

		public override Vector2 LinearVelocity
		{
			get;
			set;
		}

		private bool _wasGround = false;
		public void Update(GameTime gameTime)
		{
			if (!Parent.Active)
				return;
			
			var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
			LinearVelocity += World.Physics.Gravity * World.TimeScale * delta * 64f;

			var step = LinearVelocity * World.TimeScale * delta;
			var ground = OnGround(_wasGround ? Math.Max(3, step.Y) : step.Y);

			if (step.Y > 0 && ground.HasValue)
			{
				LinearVelocity *= new Vector2(1, step.Y = 0);
				LegPosition = ground.Value + step;
				LinearVelocity *= new Vector2(0.5f, 1);
				_wasGround = true;
			}
			else
			{
				LegPosition += step;
				_wasGround = false;
			}
			if (b != null)
				b.Position = (Position - new Vector2(0, 0.1f * Height)) / 64f;
		}

		public override void Move(Vector2 direction)
		{
			if (OnGround().HasValue)
			{
				var horizontal = direction.X;
				if (Math.Abs(horizontal) > 0.5f)
				{
					LinearVelocity = new Vector2(horizontal, LinearVelocity.Y);
				}

				// Jumping
				if (direction.Y < -0.5f)
				{

					LinearVelocity += new Vector2(0, (float)-Math.Sqrt(-direction.Y * World.Physics.Gravity.Y * 64f) * 1.5f);
				}
			}
		}

		public Vector2? OnGround(float underBody = 3)
		{
			Vector2 lg = LegPosition;
			return RayCast(lg - new Vector2(0, 8), lg + new Vector2(0, underBody));
		}


		public Vector2? RayCast(Vector2 beggining, Vector2 end)
		{
			Vector2 lg = LegPosition / 64f;
			Vector2? groundPos = null;
			float frac = 1;

			World.Physics.RayCast((Fixture arg1, Vector2 arg2, Vector2 arg3, float arg4) =>
			{
				if (arg1.Body.UserData is Terrain && frac > arg4)
				{
					groundPos = arg2;
					frac = arg4;
					return 1;
				}
				return arg4;
			}, beggining / 64, end / 64f);

			return groundPos * 64f;
		}

		Body b;
		public override void Initialize()
		{
			base.Initialize();
			b = BodyFactory.CreateRectangle(
				World.Physics,
				Width / 64f,
				0.8f * Height / 64f,
				1,
				(Position - new Vector2(0, 0.1f * Height)) / 64f,
				0,
				BodyType.Dynamic,
				this.Parent
			);

			// b.Enabled = false;
			b.IsSensor = true;
			b.FixedRotation = true;
			b.IgnoreGravity = true;
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
			writer.Write(LinearVelocity);

		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			Width = reader.ReadSingle();
			Height = reader.ReadSingle();
			Position = reader.ReadVector2();
			LinearVelocity = reader.ReadVector2();
		}
	}
}

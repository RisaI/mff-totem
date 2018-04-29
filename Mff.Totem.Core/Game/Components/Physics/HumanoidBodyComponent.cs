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
			get;
			set;
		}

		public override Vector2 LegPosition
		{
			get { return Position; }
			set { Position = value; }
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
					(int)(Position.X - Width / 2),
					(int)(Position.Y - Height),
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
			/*var gravStep = World.Physics.Gravity * (float)gameTime.ElapsedGameTime.TotalSeconds * 64f;
			Vector2? ground = OnGround();
			if (ground.HasValue)
			{
				Position = ground.Value;
				LinearVelocity = new Vector2(LinearVelocity.X, 0);
			}
			else
			{
				LinearVelocity += new Vector2(0, gravStep.Y);
			}
			Position += new Vector2(LinearVelocity.X, 0) * (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (ground.HasValue)
			{
				LinearVelocity *= new Vector2(0.5f, 1);
			}

			var step = LinearVelocity.Y * (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (step >= 0)
			{
				ground = OnGround(step);
				if (ground.HasValue)
				{
					Position = ground.Value;
					LinearVelocity = new Vector2(LinearVelocity.X, 0);
				}
				else
				{
					Position += new Vector2(0, step);
				}
			}
			else
			{
				Position += new Vector2(0, step);
			}*/
			var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
			LinearVelocity += World.Physics.Gravity * World.TimeScale * delta * 64f;

			var step = LinearVelocity * World.TimeScale * delta;
			var ground = OnGround(_wasGround ? Math.Max(3, step.Y) : step.Y);

			if (step.Y > 0 && ground.HasValue)
			{
				LinearVelocity *= new Vector2(1, step.Y = 0);
				Position = ground.Value + step;
				LinearVelocity *= new Vector2(0.5f, 1);
				_wasGround = true;
			}
			else
			{
				Position += step;
				_wasGround = false;
			}
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

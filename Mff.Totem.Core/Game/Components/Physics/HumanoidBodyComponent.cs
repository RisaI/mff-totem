using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Physics2D.Collision;
using Physics2D.Dynamics;
using Physics2D.Dynamics.Joints;
using Microsoft.Xna.Framework.Graphics;
using Physics2D.Dynamics.Contacts;

namespace Mff.Totem.Core
{
	[Serializable("component_humanoid_body")]
	public class HumanoidBody : BodyComponent, IUpdatable
	{
		public const float fRECTANGLE = 0.7f, fDELTA = 1 - fRECTANGLE;
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
				return b.Position * 64f;
			}
			set
			{
				b.Position = value / 64f;
			}
		}

		public override Vector2 LegPosition
		{
			get { return Position + new Vector2(0, Height * (1 + fDELTA) / 2); }
			set { Position = (value - new Vector2(0, Height * (1 + fDELTA) / 2)); }
		}

		public override float Rotation
		{
			get { return b.Rotation; }
			set { b.Rotation = value; }
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
			get { return b.LinearVelocity * 64f; }
			set { b.LinearVelocity = value / 64f; }
		}

		private bool _wasGround = false;
		public void Update(GameTime gameTime)
		{
			b.Enabled = Parent.Active;

			var ground = OnGround(2);
			if ((b.LinearVelocity.Y >= 0 || !_jumped) && ground.HasValue)
			{
				_jumped = false;
				LegPosition = ground.Value;
				b.LinearVelocity = new Vector2(b.LinearVelocity.X, 0);
				if (_moved)
				{
					b.LinearDamping = 0;
					_moved = false;
				}
				else
				{
					b.LinearDamping = 50f;
				}
			}
			else
			{
				b.LinearDamping = 0;
			}
		}

		private bool _moved = false, _jumped = false;
		public override void Move(Vector2 direction)
		{
			if (Grounded)
			{
				var horizontal = direction.X;
				if (Math.Abs(horizontal) > 0.5f)
				{
					b.LinearVelocity = new Vector2(horizontal / 64f, b.LinearVelocity.Y);
					_moved = true;
				}

				// Jumping
				if (direction.Y < -0.5f)
				{
					b.LinearVelocity = new Vector2(b.LinearVelocity.X, (float)-Math.Sqrt(-direction.Y / 20 * World.Physics.Gravity.Y));
					b.LinearDamping = 0;
					_jumped = true;
				}
			}
		}

		public Vector2? OnGround(float underBody = 3)
		{
			Vector2 lg = LegPosition;
			Vector2[] positions = {
				LegPosition - new Vector2(Width / 2, 0),
				LegPosition,
				LegPosition + new Vector2(Width / 2, 0),
			};
			Vector2? result = null;
			for (int i = 0; i < positions.Length; ++i)
			{
				var nRay = RayCast(positions[i] - new Vector2(0, Height * fDELTA), 
				                   positions[i] + new Vector2(0, underBody));
				if (result == null || (nRay.HasValue && nRay.Value.Y < result.Value.Y))
					result = nRay;
			}
			if (result.HasValue)
				result = new Vector2(LegPosition.X, result.Value.Y);
			return result;
		}


		public Vector2? RayCast(Vector2 beggining, Vector2 end)
		{
			Vector2 lg = LegPosition / 64f;
			Vector2? groundPos = null;
			float frac = 1;

			World.Physics.RayCast((Fixture arg1, Vector2 arg2, Vector2 arg3, float arg4) =>
			{
				if (arg1.Body.Tag is TerrainComponent && frac > arg4)
				{
					groundPos = arg2;
					frac = arg4;
					return 1;
				}
				return arg4;
			}, beggining / 64, end / 64f);

			return groundPos * 64f;
		}

		Body b = new Body();
		public override void Initialize()
		{
			base.Initialize();
			b = World.Physics.CreateRectangle(
				Width / 64f,
				fRECTANGLE * Height / 64f,
				1,
				(Position - new Vector2(0, 0.1f * Height)) / 64f,
				0,
				BodyType.Dynamic
			);

			b.FixedRotation = true;
			b.Tag = Parent;
			b.OnCollision += (sender, other, contact) =>
			{
				return other.Body.Tag is TerrainComponent;
			};
		}

		public override bool Grounded
		{
			get
			{
				return (LinearVelocity.Y >= 0 || !_jumped) && OnGround().HasValue;
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

		public override void Serialize(System.IO.BinaryWriter writer)
		{
			writer.Write(Width);
			writer.Write(Height);
			writer.Write(Position);
			writer.Write(LinearVelocity);

		}

		public override void Deserialize(System.IO.BinaryReader reader)
		{
			Width = reader.ReadSingle();
			Height = reader.ReadSingle();
			Position = reader.ReadVector2();
			LinearVelocity = reader.ReadVector2();
		}
	}
}

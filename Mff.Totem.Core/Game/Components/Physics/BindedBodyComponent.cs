using System;
using System.Linq;

using Microsoft.Xna.Framework;
using FarseerPhysics.Collision;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Mff.Totem.Core
{
	[Serializable("component_binded_body")]
	public class BindedBodyComponent : BodyComponent
	{
		public Entity BindedTo;
		public float Radius, Angle;

		public override Vector2 Position
		{
			get
			{
				var bodyComponent = BindedTo != null ? BindedTo.GetComponent<BodyComponent>() : null;
				if (bodyComponent != null)
				{
					return Helper.AngleToDirection(Angle + bodyComponent.Rotation) * Radius + bodyComponent.Position;
				}
				else
				{
					return Helper.AngleToDirection(Angle) * Radius;
				}
			}
			set
			{
				return;
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
			get;
			set;
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

		public BindedBodyComponent()
		{

		}

		public override void Move(Vector2 direction)
		{
			return;
		}

		public override EntityComponent Clone()
		{
			return new BindedBodyComponent()
			{
				Rotation = Rotation,
				Position = Position
			};
		}

		public BindedBodyComponent BindTo(Entity binded, Vector2 position, float rotation)
		{
			BindedTo = binded;
			var bindedPos = binded.Position;
			Rotation = rotation;
			if (bindedPos != null)
			{
				var rel = position - (Vector2)bindedPos;
				Radius = rel.Length();
				Angle = Helper.DirectionToAngle(rel);
			}

			return this;
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			writer.Write(Position);
			writer.Write(Rotation);
			writer.Write(Radius);
			writer.Write(Angle);
			writer.Write(BindedTo != null);
			if (BindedTo != null)
				writer.Write(BindedTo.UID);
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			Position = reader.ReadVector2();
			Rotation = reader.ReadSingle();
			Radius = reader.ReadSingle();
			Angle = reader.ReadSingle();
			if (reader.ReadBoolean())
			{
				lookup = reader.ReadGuid();
			}
		}

		Guid lookup;
		public override void Initialize()
		{
			base.Initialize();
			if (lookup != default(Guid))
				BindedTo = World.GetEntity(lookup);
		}
	}
}
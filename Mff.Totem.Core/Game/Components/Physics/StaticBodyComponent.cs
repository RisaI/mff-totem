using System;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("component_static_body")]
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
				return new Rectangle((int)(Position.X - Size.X / 2), (int)(Position.Y - Size.Y), (int)Size.X, (int)Size.Y);
			}
			set
			{
				Position = value.Center.ToVector2() + new Vector2(0, value.Height / 2);
				Size = new Vector2(value.Width, value.Height);
			}
		}

		public override Vector2 LinearVelocity
		{
			get
			{
				return default(Vector2);
			}

			set
			{
				return;
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
}

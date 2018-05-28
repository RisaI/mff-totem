using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public abstract class BodyComponent : EntityComponent
	{
		public abstract Vector2 Position
		{
			get;
			set;
		}

		public abstract Vector2 LinearVelocity
		{
			get;
			set;
		}

		public abstract Vector2 LegPosition
		{
			get;
			set;
		}

		public abstract float Rotation
		{
			get;
			set;
		}

		public abstract Rectangle BoundingBox
		{
			get;
			set;
		}

		public abstract void Move(Vector2 direction);
		public abstract bool Grounded
		{
			get;
		}
	}
}

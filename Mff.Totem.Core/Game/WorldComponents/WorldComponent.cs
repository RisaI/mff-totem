using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public abstract class WorldComponent : ISerializable
	{
		public GameWorld World
		{
			get;
			private set;
		}

		public virtual void Attach(GameWorld parent)
		{
			World = parent;
		}

		public abstract void Initialize();
		public abstract void SetActiveArea(Vector2 pos);

		/// <summary>
		/// Serialize component to a binary format.
		/// </summary>
		/// <param name="writer">Writer.</param>
		public abstract void Serialize(BinaryWriter writer);

		/// <summary>
		/// Deserialize component from a binary format.
		/// </summary>
		/// <param name="reader">Reader.</param>
		public abstract void Deserialize(BinaryReader reader);
	}
}

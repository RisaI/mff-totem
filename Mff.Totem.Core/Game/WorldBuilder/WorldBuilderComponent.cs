using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public abstract class WorldBuilderComponent : ISerializable
	{
		public WorldBuilder Parent
		{
			get;
			private set;
		}

		public GameWorld World
		{
			get { return Parent?.World; }
		}

		public virtual void Attach(WorldBuilder parent)
		{
			Parent = parent;
		}

		public abstract void Generate();
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

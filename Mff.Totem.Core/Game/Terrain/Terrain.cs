using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public abstract class Terrain : ISerializable, IUpdatable
	{
		public GameWorld World
		{
			get;
			private set;
		}

		public Terrain(GameWorld world)
		{
			World = world;
		}

		public abstract void ActiveRegion(Vector2 center, bool multithreading = true);
		public abstract float HeightMap(float x);

		public abstract void Serialize(BinaryWriter writer);
		public abstract void Deserialize(BinaryReader reader);

		public abstract void Update(GameTime gameTime);
		public abstract void DrawBackground(SpriteBatch spriteBatch);
		public abstract void DrawForeground(SpriteBatch spriteBatch);
	}
}

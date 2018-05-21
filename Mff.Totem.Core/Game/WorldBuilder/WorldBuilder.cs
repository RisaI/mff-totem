using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public class WorldBuilder : ISerializable
	{
		List<WorldBuilderComponent> Components = new List<WorldBuilderComponent>(32);

		public int Seed
		{
			get;
			private set;
		}

		GameWorld _world;
		public GameWorld World
		{
			get
			{
				return _world;
			}
			set
			{
				_world = value;
				Components.ForEach(c => c.Initialize());
			}
		}

		public WorldBuilder(GameWorld world)
		{
			_world = world;
		}

		public void Generate(int seed)
		{
			Seed = seed;
			Components.ForEach(c => c.Generate());
		}

		public T GetComponent<T>() where T : WorldBuilderComponent
		{
			return Components.Find(c => c is T) as T;
		}

		public WorldBuilder AddComponent(WorldBuilderComponent component)
		{
			if (!Components.Contains(component))
			{
				Components.Add(component);
				component.Attach(this);
				if (World != null)
					component.Initialize();
			}
			return this;
		}

		public void SetActiveArea(Vector2 position)
		{
			Components.ForEach(c => c.SetActiveArea(position));
		}

		public void Update(GameTime gameTime)
		{
			Components.ForEach(c =>
			{
				var upd = c as IUpdatable;
				if (upd != null)
				{
					upd.Update(gameTime);
				}
			});
		}

		public int DrawLayer
		{
			get;
			private set;
		}

		public void Draw(SpriteBatch spriteBatch, int layer)
		{
			DrawLayer = layer;
			Components.ForEach(c =>
			{
				var draw = c as IDrawable;
				if (draw != null)
				{
					draw.Draw(spriteBatch);
				}
			});
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Seed);

			writer.Write((Int16)Components.Count);
			Components.ForEach(c => DeserializationRegister.WriteObject(writer, c));
		}

		public void Deserialize(BinaryReader reader)
		{
			Seed = reader.ReadInt32();

			var count = reader.ReadInt16();
			for (int i = 0; i < count; ++i)
				AddComponent(DeserializationRegister.ReadObject<WorldBuilderComponent>(reader));
		}
	}
}

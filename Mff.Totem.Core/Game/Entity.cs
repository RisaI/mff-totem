using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	public sealed class Entity : IUpdatable, IDrawable, IJsonSerializable, ICloneable<Entity>, ISerializable
	{
		/// <summary>
		/// A unique ID of this entity.
		/// </summary>
		public Guid UID
		{
			get;
			private set;
		}

		/// <summary>
		/// A reference to a GameWorld instance this entity resides in.
		/// </summary>
		public GameWorld World
		{
			get;
			private set;
		}

		bool _active = true;

		/// <summary>
		/// Is this entity in the active region during an update?
		/// </summary>
		/// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
		public bool Active
		{
			get
			{
				return _active;
			}
			set
			{
				if (_active != value)
				{
					Components.ForEach(c => c.ActiveStateChanged());
					_active = value;
				}
			}
		}

		private bool _remove;
		public bool Remove
		{
			get { return _remove; }
			set
			{
				if (_remove != value)
				{
					if (value)
						Destroy();
					_remove = value;
				}
			}
		}

		public Vector2? Position
		{
			get
			{
				var body = GetComponent<BodyComponent>();
				if (body != null)
					return body.Position;
				else
					return null;
			}
		}

		public Vector2? LegPosition
		{
			get
			{
				var body = GetComponent<BodyComponent>();
				if (body != null)
					return body.LegPosition;
				else
					return null;
			}
		}

		public float Rotation
		{
			get
			{
				var body = GetComponent<BodyComponent>();
				if (body != null)
					return body.Rotation;
				else
					return 0;
			}
		}

		public Vector2? Targeting
		{
			get
			{
				var character = GetComponent<CharacterComponent>();
				if (character != null)
					return character.Target;
				else
					return null;
			}
		}

		// Should this entity be saved during GameWorld serialization
		public bool ShouldSave
		{
			get
			{
				return !Components.Any(c => c.DisableEntitySaving);
			}
		}

		/// <summary>
		/// A list of entity components that belong to this entity.
		/// </summary>
		private List<EntityComponent> Components;

		/// <summary>
		/// Entity tags.
		/// </summary>
		public List<string> Tags;

		public Entity()
		{
			UID = Guid.NewGuid();
			Components = new List<EntityComponent>();
			Tags = new List<string>();
		}

		/// <summary>
		/// Adds a component to this entity and attaches it.
		/// </summary>
		/// <param name="component">Component.</param>
		/// <returns>The entity for chaining.</returns>
		public Entity AddComponent(EntityComponent component)
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

		/// <summary>
		/// Get component by type.
		/// </summary>
		/// <returns>The component.</returns>
		/// <typeparam name="T">Type.</typeparam>
		public T GetComponent<T>() where T : EntityComponent
		{
			for (int i = 0; i < Components.Count; ++i)
			{
				if (Components[i] is T)
					return (T)Components[i];
			}
			return null;
		}

		/// <summary>
		/// Get component by serialization string.
		/// </summary>
		/// <returns>The component.</returns>
		/// <typeparam name="T">Type.</typeparam>
		public EntityComponent GetComponent(string serializationId)
		{
			for (int i = 0; i < Components.Count; ++i)
			{
				if (Components[i].GetType().GetCustomAttributes(false)
				    .Any(c => c is SerializableAttribute && 
				         (c as SerializableAttribute).ID == serializationId)) 
					return Components[i];
			}
			return null;
		}

		/// <summary>
		/// Remove all components of a type.
		/// </summary>
		/// <typeparam name="T">Component type.</typeparam>
		public void RemoveComponents<T>() where T : EntityComponent
		{
			Components.ForEach(c =>
			{
				if (c is T)
				{
					c.Remove = true;
				}
			});
		}

		/// <summary>
		/// Initialize this entity in a GameWorld.
		/// </summary>
		/// <param name="world">World.</param>
		/// <returns>The entity for chaining.</returns>
		public Entity Initialize(GameWorld world)
		{
			World = world;
			Components.ForEach(c => c.Initialize());
			return this;
		}

		public void Update(GameTime gameTime)
		{
			for (int i = Components.Count - 1; i >= 0; --i)
			{
				if (Components[i].Remove)
					Components.RemoveAt(i);
				else
				{
					var updatable = Components[i] as IUpdatable;
					if (updatable != null)
						updatable.Update(gameTime);
				}
			}
		}

		public void Draw(SpriteBatch spriteBatch)
		{

			for (int i = 0; i < Components.Count; ++i)
			{
				var drawable = Components[i] as IDrawable;
				if (drawable != null)
					drawable.Draw(spriteBatch);
			}
		}

		public bool Interact(Entity ent)
		{
			bool interacted = false;
			for (int i = 0; i < Components.Count; ++i)
			{
				var drawable = Components[i] as IInteractive;
				if (drawable != null)
				{
					drawable.Interact(ent);
					interacted = true;
				}
			}
			return interacted;
		}

		/// <summary>
		/// Destroy this entity and it's components.
		/// </summary>
		public void Destroy()
		{
			Components.ForEach(c => c.Destroy());
		}

		/// <summary>
		/// Clone this entity and it's components. Does not preserve UID.
		/// </summary>
		/// <returns>The clone.</returns>
		public Entity Clone()
		{
			var entity = new Entity();
			Components.ForEach(c => entity.AddComponent(c.Clone()));
			Tags.ForEach(t => entity.Tags.Add(t));
			return entity;
		}

		/// <summary>
		/// Serialize this entity using a JsonWriter.
		/// </summary>
		/// <param name="writer">Writer.</param>
		public void ToJson(JsonWriter writer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("tags");
			writer.WriteStartArray(); // Array of components
			Tags.ForEach(t => writer.WriteValue(t));
			writer.WriteEndArray();
			writer.WritePropertyName("components");
			writer.WriteStartArray(); // Array of components
			Components.ForEach(c => DeserializationRegister.ObjectToJson(writer, c));
			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		/// <summary>
		/// Deserialize this entity from a JSON object.
		/// </summary>
		/// <param name="obj">Object.</param>
		public void FromJson(JObject obj)
		{
			if (obj["tags"] != null)
			{
				var tags = (JArray)obj.GetValue("tags");
				for (int i = 0; i < tags.Count; ++i)
				{
					Tags.Add((string)tags[i]);
				}
			}
			var components = (JArray)obj.GetValue("components");
			for (int i = 0; i < components.Count; ++i)
			{
				AddComponent(DeserializationRegister.ObjectFromJson<EntityComponent>((JObject)components[i]));
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(UID);
			writer.Write(_active);

			// Tags
			writer.Write(Tags.Count);
			Tags.ForEach(t => writer.Write(t));

			// Components
			writer.Write((byte)Components.Count);
			Components.ForEach(c => c.Serialize(writer));
		}

		public void Deserialize(BinaryReader reader)
		{
			UID = reader.ReadGuid();
			_active = reader.ReadBoolean();

			// Tags
			var tCount = reader.ReadInt32();
			for (int i = 0; i < tCount; ++i)
			{
				Tags.Add(reader.ReadString());
			}

			// Components
			var count = reader.ReadByte();
			for (int i = 0; i < count; ++i)
			{
				var component = EntityComponent.CreateFromBinary(reader);
				AddComponent(component);
			}
		}
	}
}

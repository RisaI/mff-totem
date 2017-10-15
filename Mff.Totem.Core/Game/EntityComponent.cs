using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	public abstract class EntityComponent : ICloneable<EntityComponent>, IJsonSerializable
	{
		public Entity Parent
		{
			get;
			private set;
		}

		public GameWorld World
		{
			get { return Parent != null ? Parent.World : World; }
		}

		/// <summary>
		/// Attach an entity to this component.
		/// </summary>
		/// <param name="ent">Entity.</param>
		public void Attach(Entity ent)
		{
			OnEntityAttach(ent);
			if (Parent != null)
				Parent.RemoveComponent(this);
			Parent = ent;
		}

		/// <summary>
		/// Called when an entity is attached to this component.
		/// </summary>
		/// <param name="entity">Entity.</param>
		protected abstract void OnEntityAttach(Entity entity);

		/// <summary>
		/// Called when an entity is spawned into a world.
		/// </summary>
		public abstract void Initialize();

		/// <summary>
		/// Called when parent entity gets destroyed.
		/// </summary>
		public abstract void Destroy();

		/// <summary>
		/// Clone this instance.
		/// </summary>
		/// <returns>The clone.</returns>
		public abstract EntityComponent Clone();

		/// <summary>
		/// Serialize this component using a JsonWriter.
		/// </summary>
		/// <param name="writer">Writer.</param>
		public void ToJson(JsonWriter writer)
		{
			var attributes = GetType().GetCustomAttributes(typeof(SerializableAttribute), false);
			if (attributes.Length == 0)
				return;

			writer.WriteStartObject();
			writer.WritePropertyName("name");
			writer.WriteValue(((SerializableAttribute)attributes[0]).ID);
			WriteToJson(writer);
			writer.WriteEndObject();
		}

		/// <summary>
		/// Used for custom component serialization from JSON.
		/// </summary>
		/// <param name="writer">Writer.</param>
		protected abstract void WriteToJson(JsonWriter writer);

		/// <summary>
		/// Load this component from JSON.
		/// </summary>
		/// <param name="obj">JObject.</param>
		public void FromJson(JObject obj)
		{
			ReadFromJson(obj);
		}

		/// <summary>
		/// Used for custom component deserialization from JSON.
		/// </summary>
		/// <param name="obj">Object.</param>
		protected abstract void ReadFromJson(JObject obj);

		/// <summary>
		/// Creates a component from a JObject.
		/// </summary>
		/// <returns>A deserialized component.</returns>
		/// <param name="obj">JObject.</param>
		public static EntityComponent CreateFromJSON(JObject obj)
		{
			EntityComponent component = (EntityComponent)DeserializationRegister.CreateInstance((string)obj["name"]);
			component.FromJson(obj);
			return component;
		}
	}
}

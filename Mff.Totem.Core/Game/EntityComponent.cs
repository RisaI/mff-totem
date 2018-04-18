﻿using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	public abstract class EntityComponent : ICloneable<EntityComponent>, IJsonSerializable, ISerializable
	{
		bool _remove;
		public bool Remove
		{
			get
			{
				return _remove;
			}
			set
			{
				_remove = value;
			}
		}

		public Entity Parent
		{
			get;
			private set;
		}

		public GameWorld World
		{
			get { return Parent != null ? Parent.World : World; }
		}

		public virtual bool DisableEntitySaving
		{
			get { return false; }
		}

		/// <summary>
		/// Attach an entity to this component.
		/// </summary>
		/// <param name="ent">Entity.</param>
		public void Attach(Entity ent)
		{
			OnEntityAttach(ent);
			Parent = ent;
		}

		/// <summary>
		/// Called when an entity is attached to this component.
		/// </summary>
		/// <param name="entity">Entity.</param>
		protected virtual void OnEntityAttach(Entity entity) { return; }

		/// <summary>
		/// Called when an entity is spawned into a world.
		/// </summary>
		public virtual void Initialize() { return; }

		/// <summary>
		/// Called when parent entity gets destroyed.
		/// </summary>
		public virtual void Destroy() { return; }

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
			WriteToJson(writer);
		}

		/// <summary>
		/// Used for custom component serialization from JSON.
		/// </summary>
		/// <param name="writer">Writer.</param>
		protected virtual void WriteToJson(JsonWriter writer) { return; }

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
		protected virtual void ReadFromJson(JObject obj) { return; }

		/// <summary>
		/// Creates a component from a JObject.
		/// </summary>
		/// <returns>A deserialized component.</returns>
		/// <param name="obj">JObject.</param>
		public static EntityComponent CreateFromBinary(BinaryReader reader)
		{
			var cc = reader.ReadString();
			EntityComponent component = (EntityComponent)DeserializationRegister.CreateInstance(cc);
			component.Deserialize(reader);
			return component;
		}

		/// <summary>
		/// Serialize component to a binary format.
		/// </summary>
		/// <param name="writer">Writer.</param>
		public void Serialize(BinaryWriter writer)
		{
			var attributes = GetType().GetCustomAttributes(typeof(SerializableAttribute), false);
			if (attributes.Length == 0)
				return;
			writer.Write(((SerializableAttribute)attributes[0]).ID);
			OnSerialize(writer);
		}

		protected virtual void OnSerialize(BinaryWriter writer) { return; }

		/// <summary>
		/// Deserialize component from a binary format.
		/// </summary>
		/// <param name="reader">Reader.</param>
		public void Deserialize(BinaryReader reader)
		{
			OnDeserialize(reader);
		}

		protected virtual void OnDeserialize(BinaryReader reader) { return; }
	}
}

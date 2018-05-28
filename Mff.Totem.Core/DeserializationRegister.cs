using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{

	public static class DeserializationRegister
	{
		static Dictionary<string, Func<object>> Register = new Dictionary<string, Func<object>>();

		/// <summary>
		/// Adds all qualified classes with [Serializable] attribute from a given assembly to the register.
		/// </summary>
		/// <param name="assembly">Assembly.</param>
		public static void ScanAssembly(System.Reflection.Assembly assembly)
		{
			// Iterate through all classes and find all classes with [Serializable]
			foreach (Type t in assembly.GetTypes())
			{
				var l = t.GetCustomAttributes(typeof(SerializableAttribute), false);
				if (l.Length > 0)
				{
					AddToRegister(((SerializableAttribute)l[0]).ID, t);
				}
			}
		}

		/// <summary>
		/// Add a type to the deserialization register.
		/// </summary>
		/// <param name="identifier">Identifier.</param>
		/// <param name="t">Type.</param>
		public static void AddToRegister(string identifier, Type t)
		{
			if (!Register.ContainsKey(identifier) && t.GetConstructor(Type.EmptyTypes) != null) // Check for a default constructor
			{
				// Add a precompiled default constructor expression to the register
				Register.Add(identifier, Expression.Lambda<Func<object>>(Expression.New(t.GetConstructor(Type.EmptyTypes))).Compile());
			}
		}

		/// <summary>
		/// Create an instance of an object with a specified identifier
		/// </summary>
		/// <returns>A new instance.</returns>
		/// <param name="identifier">Identifier.</param>
		public static object CreateInstance(string identifier)
		{
			if (Register.ContainsKey(identifier))
				return Register[identifier]();

			return null;
		}

		/// <summary>
		/// Create an instance of an object with a specified identifier
		/// </summary>
		/// <returns>A new instance.</returns>
		/// <param name="identifier">Identifier.</param>
		public static T CreateInstance<T>(string identifier)
		{
			if (Register.ContainsKey(identifier))
				return (T)Register[identifier]();

			return default(T);
		}

		public static T ObjectFromJson<T>(JObject obj) where T : IJsonSerializable
		{
			var instance = (IJsonSerializable)CreateInstance((string)obj["class"]);
			instance.FromJson(obj);
			return (T)instance;
		}

		public static void ObjectToJson<T>(JsonWriter writer, T obj) where T : IJsonSerializable
		{
			var attributes = obj.GetType().GetCustomAttributes(typeof(SerializableAttribute), false);
			if (attributes.Length == 0)
				return;

			writer.WriteStartObject();
			writer.WritePropertyName("class");
			writer.WriteValue(((SerializableAttribute)attributes[0]).ID);
			obj.ToJson(writer);
			writer.WriteEndObject();
		}

		public static T ReadObject<T>(BinaryReader reader) where T : ISerializable
		{
			var instance = (ISerializable)CreateInstance(reader.ReadString());
			instance.Deserialize(reader);
			return (T)instance;
		}

		public static void WriteObject<T>(BinaryWriter writer, T obj) where T : ISerializable
		{
			var attributes = obj.GetType().GetCustomAttributes(typeof(SerializableAttribute), false);
			if (attributes.Length == 0)
				return;

			writer.Write(((SerializableAttribute)attributes[0]).ID);
			obj.Serialize(writer);
		}
	}
}

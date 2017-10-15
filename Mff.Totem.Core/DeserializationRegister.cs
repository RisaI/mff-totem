﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
	}
}
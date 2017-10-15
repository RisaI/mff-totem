using System;
namespace Mff.Totem.Core
{
	/// <summary>
	/// Attribute to mark a class for serialization/deserialization. Classes with this attribute are automatically added to the deserialization register when their parent assembly is scanned.
	/// </summary>
	public class SerializableAttribute : Attribute
	{
		public string ID
		{
			get;
			private set;
		}

		public SerializableAttribute(string id)
		{
			ID = id;
		}
	}
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	public interface IUpdatable
	{
		void Update(GameTime gameTime);
	}

	public interface IDrawable
	{
		void Draw(SpriteBatch spriteBatch);
	}

	public interface ICloneable<T>
	{
		T Clone();
	}

	public interface IJsonSerializable
	{
		void ToJson(JsonWriter writer);
		void FromJson(JObject obj);
	}

	public interface ISerializable
	{
		void Serialize(System.IO.BinaryWriter writer);
		void Deserialize(System.IO.BinaryReader reader);
	}
}

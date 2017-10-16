using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	public abstract class Particle : IJsonSerializable, IUpdatable, IDrawable
	{
		public abstract Vector2 Position
		{
			get;
			set;
		}

		/// <summary>
		/// Parent world.
		/// </summary>
		/// <value>The world.</value>
		public GameWorld World
		{
			get;
			private set;
		}

		public Particle(GameWorld world)
		{
			World = world;
		}

		/// <summary>
		/// Total lifetime of this particle in seconds.
		/// </summary>
		public float LifeTime
		{
			get;
			private set;
		}

		/// <summary>
		/// Should this particle be removed next update?
		/// </summary>
		public bool Remove;

		public virtual void Update(GameTime gameTime)
		{
			LifeTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
		}

		public abstract void Draw(SpriteBatch spriteBatch);

		public virtual void FromJson(JObject obj) { }

		public virtual void ToJson(JsonWriter writer) 
		{ 
			var attributes = GetType().GetCustomAttributes(typeof(SerializableAttribute), false);
			if (attributes.Length == 0)
				return;
			writer.WritePropertyName("name");
			writer.WriteValue((attributes[0] as SerializableAttribute).ID);
		}
	}
}

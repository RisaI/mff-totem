using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	public abstract class Particle : IJsonSerializable, ICloneable<Particle>, IUpdatable, IDrawable
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

		public void Spawn(GameWorld world)
		{
			World = world;
			OnSpawn();
		}

		protected abstract void OnSpawn();

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

		public abstract Particle Clone();
	}

	public class RainParticle : Particle
	{
		private Vector2 _position;
		public override Vector2 Position
		{
			get { return _position; }
			set
			{
				_position = value;
				RecalculatePath();
			}
		}

		protected override void OnSpawn()
		{
			RecalculatePath();
			World.Physics.RayCast((arg1, arg2, arg3, arg4) => {
				
				return arg4;
			}, _position / 64f, (_position + new Vector2(0, 600)) / 64f);
		}

		private void RecalculatePath()
		{
			if (World == null)
				return;


		}

		public override Particle Clone()
		{
			return new RainParticle();
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			
		}
	}
}

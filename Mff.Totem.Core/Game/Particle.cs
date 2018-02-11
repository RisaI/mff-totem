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

		public float MaxLifeTime
		{
			get;
			protected set;
		}

		/// <summary>
		/// Should this particle be removed next update?
		/// </summary>
		public bool Remove;

		public virtual void Update(GameTime gameTime)
		{
			LifeTime += (float)gameTime.ElapsedGameTime.TotalSeconds * World.TimeScale;

			if (MaxLifeTime > 0 && LifeTime >= MaxLifeTime)
				Remove = true;
		}

		public abstract void Draw(SpriteBatch spriteBatch);

		public virtual void FromJson(JObject obj) {
			if (obj["lifetime"] != null)
				MaxLifeTime = (float)obj["lifetime"];
		}

		public virtual void ToJson(JsonWriter writer) 
		{
			
		}

		public abstract Particle Clone();
	}

	namespace Particles
	{
		[Serializable("particle_falling")]
		public class FallingParticle : Particle
		{
			public override Vector2 Position
			{
				get;
				set;
			}

			public float FallSpeed = 100;
			private string SpriteAsset, Animation;

			public override Particle Clone()
			{
				return new FallingParticle() {
					MaxLifeTime = MaxLifeTime,
					FallSpeed = FallSpeed, 
					SpriteAsset = SpriteAsset, 
					Animation = Animation };
			}

			private Sprite _Sprite;
			public override void Draw(SpriteBatch spriteBatch)
			{
				if (_Sprite != null)
				{
					_Sprite.Draw(spriteBatch, Position, 0, 1);
				}
			}

			public override void Update(GameTime gameTime)
			{
				base.Update(gameTime);
				Position += new Vector2(0, (float)gameTime.ElapsedGameTime.TotalSeconds * World.TimeScale * FallSpeed);
				if (_Sprite != null)
					_Sprite.Update(gameTime, World.TimeScale);
			}

			protected override void OnSpawn()
			{
				if (_Sprite == null && SpriteAsset != null)
				{
					_Sprite = ContentLoader.Sprites[SpriteAsset].Clone();
					_Sprite.PlayAnimationFromRegistry(Animation, true);
				}
			}

			public override void FromJson(JObject obj)
			{
				base.FromJson(obj);
				if (obj["fallspeed"] != null)
					FallSpeed = (float)obj["fallspeed"];
				if (obj["sprite"] != null)
					SpriteAsset = (string)obj["sprite"];
				if (obj["anim"] != null)
					Animation = (string)obj["anim"];
			}
		}
	}
}

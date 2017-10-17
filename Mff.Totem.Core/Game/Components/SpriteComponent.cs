using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	[Serializable("sprite_component")]
	public class SpriteComponent : EntityComponent, IUpdatable, IDrawable
	{
		public Sprite Sprite
		{
			get;
			private set;
		}

		private string _spriteAsset;
		public string SpriteAsset
		{
			get { return _spriteAsset; }
			set
			{
				_spriteAsset = value;
				Sprite = ContentLoader.Sprites[value].Clone();
			}
		}

		protected override void OnEntityAttach(Entity entity)
		{
			body = entity.GetComponent<BodyComponent>();
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (Sprite != null)
				Sprite.Draw(spriteBatch, body != null ? body.LegPosition : Vector2.Zero, 1f);
		}

		private BodyComponent body;
		public void Update(GameTime gameTime)
		{
			if (Sprite != null)
				Sprite.Update(gameTime, World.TimeScale);
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			if (obj["sprite"] != null)
				SpriteAsset = (string)obj["sprite"];
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			writer.WritePropertyName("sprite");
			writer.WriteValue(_spriteAsset);
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			writer.Write(_spriteAsset);
			Sprite.SerializeState(writer);
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			SpriteAsset = reader.ReadString();
			Sprite.DeserializeState(reader);
		}

		public override EntityComponent Clone()
		{
			var sprite = new SpriteComponent() { _spriteAsset = _spriteAsset };
			sprite.Sprite = Sprite.Clone();
			return sprite;
		}

		public void PlayAnim(string anim, bool restartIfPlaying = false)
		{
			if (Sprite != null)
				Sprite.PlayAnimationFromRegistry(anim, restartIfPlaying);
		}

		public SpriteEffects Effect
		{
			get { return Sprite != null ? Sprite.Effect : SpriteEffects.None; }
			set
			{
				if (Sprite != null)
					Sprite.Effect = value;
			}
		}
	}
}

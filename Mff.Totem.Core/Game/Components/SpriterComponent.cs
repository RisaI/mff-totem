using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SpriterDotNet.MonoGame;

namespace Mff.Totem.Core
{
	[Serializable("component_spriter")]
	public class SpriterComponent : EntityComponent, IUpdatable, IDrawable
	{
		public MonoGameAnimator Sprite
		{
			get;
			private set;
		}

		public float Depth = 1f;

		private string _spriterAsset;
		public string SpriterAsset
		{
			get { return _spriterAsset; }
			set
			{
				_spriterAsset = value;
				Sprite = ContentLoader.BoneSprites[value].GetAnimator();
				Sprite.DeltaDepth = -Sprite.DeltaDepth;
				if (Sprite.HasAnimation("idle"))
					Sprite.Play("idle");
				Sprite.Speed = DefaultPlaybackSpeed;
			}
		}

		public Vector2 Scale = Vector2.One;
		public float DefaultPlaybackSpeed = 1f;

		public void Draw(SpriteBatch spriteBatch)
		{
            if (Sprite != null)
            {
				var pos = Parent.LegPosition;
				Sprite.Scale = Scale * new Vector2(
					Effect == SpriteEffects.FlipHorizontally ? -1 : 1,
					Effect == SpriteEffects.FlipVertically ? -1 : 1);
				Sprite.Position = pos != null ? pos.Value : Vector2.Zero;
				Sprite.Rotation = Parent.Rotation;
				Sprite.Depth = Depth - 0.1f;
				Sprite.Draw(spriteBatch);
            }
		}

		public void Update(GameTime gameTime)
		{
			if (Sprite != null)
				Sprite.Update((float)gameTime.ElapsedGameTime.TotalMilliseconds * World.TimeScale);
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			if (obj["sprite"] != null)
				SpriterAsset = (string)obj["sprite"];
			if (obj["depth"] != null)
				Depth = (float)obj["depth"];
			if (obj["scale"] != null)
				Scale = Helper.JTokenToVector2(obj["scale"]);
			if (obj["speed"] != null)
				DefaultPlaybackSpeed = (float)obj["speed"];
		}

		protected override void WriteToJson(Newtonsoft.Json.JsonWriter writer)
		{
			writer.WritePropertyName("sprite");
			writer.WriteValue(_spriterAsset);
			writer.WritePropertyName("depth");
			writer.WriteValue(Depth);
			writer.WritePropertyName("scale");
			writer.WriteValue(Scale);
			writer.WritePropertyName("speed");
			writer.WriteValue(DefaultPlaybackSpeed);
		}

		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			writer.Write(_spriterAsset);
			// Sprite.SerializeState(writer);
			writer.Write(Depth);
			writer.Write(Scale);
			writer.Write(DefaultPlaybackSpeed);
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			SpriterAsset = reader.ReadString();
			// Sprite.DeserializeState(reader);
			Depth = reader.ReadSingle();
			Scale = reader.ReadVector2();
			DefaultPlaybackSpeed = reader.ReadSingle();
		}

		public override EntityComponent Clone()
		{
			var sprite = new SpriterComponent() { 
				_spriterAsset = _spriterAsset, 
				Depth = Depth,
				Scale = Scale,
				DefaultPlaybackSpeed = DefaultPlaybackSpeed,
			};
			sprite.SpriterAsset = _spriterAsset;
			return sprite;
		}

		public void PlayAnim(string anim, bool restartIfPlaying = false, float transition = 0)
		{
			if (Sprite != null)
			{
				if (transition > 0)
				{
					if (restartIfPlaying || (Sprite.NextAnimation?.Name != anim))
					{
						Sprite.Transition(anim, transition);
					}
				}
				else
				{
					if (restartIfPlaying || (Sprite.CurrentAnimation?.Name != anim))
					{
						Sprite.Play(anim);
					}
				}
			}
		}

		public SpriteEffects Effect
		{
			get;
			set;
		}
	}
}

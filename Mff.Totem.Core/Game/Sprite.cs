using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	public class Sprite : ICloneable<Sprite>, IJsonSerializable
	{
		private string _textureAsset;

		/// <summary>
		/// Sprite texture.
		/// </summary>
		/// <value>A texture.</value>
		public Texture2D Texture
		{
			get;
			private set;
		}

		/// <summary>
		/// A registry of animations.
		/// </summary>
		private Dictionary<string, Animation> _animRegistry;

		/// <summary>
		/// The currently active animation.
		/// </summary>
		private Animation _anim;

		/// <summary>
		/// Get or set the current animation.
		/// </summary>
		public Animation Animation
		{
			get { return _anim; }
			set
			{
				_anim = value;
				_timer = 0;
 				CurrentFrame = _anim.HasFlag(Animation.StartReversed) ? _anim.EndFrame : _anim.StartFrame; // Should be started from the end?
				_negDir = _anim.HasFlag(Animation.StartReversed); // Should the animation go backwards?
			}
		}

		/// <summary>
		/// Sprite draw color.
		/// </summary>
		public Color Color = Color.White;

		/// <summary>
		/// The current frame.
		/// </summary>
		public int CurrentFrame = 0;

		/// <summary>
		/// Sprite draw effect.
		/// </summary>
		public SpriteEffects Effect = SpriteEffects.None;

		/// <summary>
		/// Sprite scaling.
		/// </summary>
		public Vector2 Scale = Vector2.One;

		/// <summary>
		/// Relative anchor used for drawing (relative origin, (0,0) is the top-left corner, (1,1) the bottom right corner).
		/// </summary>
		public Vector2 Anchor = new Vector2(0.5f);

		// Internal values
		private int _frameWidth, _frameHeight, _totalFrames, _framesInRow;

		/// <summary>
		/// Create a sprite from JSON.
		/// </summary>
		/// <param name="obj">JObject.</param>
		public Sprite(JObject obj)
		{
			FromJson(obj);
		}

		/// <summary>
		/// Create a sprite from a loaded texture.
		/// </summary>
		/// <param name="textureAsset">Name of a loaded texture.</param>
		/// <param name="frameWidth">Frame width.</param>
		/// <param name="frameHeight">Frame height.</param>
		public Sprite(string textureAsset, int frameWidth, int frameHeight)
		{
			_textureAsset = textureAsset;
			_frameWidth = frameWidth;
			_frameHeight = frameHeight;
			Recalculate();
		}

		/// <summary>
		/// Create a sprite from a loaded texture.
		/// </summary>
		/// <param name="textureAsset">Name of a loaded texture.</param>
		/// <param name="frameWidth">Frame width.</param>
		/// <param name="frameHeight">Frame height.</param>
		/// <param name="anims">Animation registry.</param>
		public Sprite(string textureAsset, int frameWidth, int frameHeight, Dictionary<string, Animation> anims)
			: this(textureAsset, frameWidth, frameHeight)
		{
			SetAnimationRegistry(anims);
		}


		/// <summary>
		/// Recalculate runtime values.
		/// </summary>
		private void Recalculate()
		{
			Texture = ContentLoader.Textures[_textureAsset];
			_totalFrames = (_framesInRow = (Texture.Width / _frameWidth)) * (Texture.Height / _frameHeight);
		}

		/// <summary>
		/// Overwrite the animation registry.
		/// </summary>
		/// <param name="anims">A new anim registry.</param>
		public void SetAnimationRegistry(Dictionary<string, Animation> anims)
		{
			_animRegistry = anims;
		}

		/// <summary>
		/// Get an animation from registry.
		/// </summary>
		/// <returns>An animation.</returns>
		/// <param name="name">Animation name.</param>
		public Animation GetAnimationFromRegistry(string name)
		{
			return _animRegistry[name];
		}

		/// <summary>
		/// Play an animation from registry.
		/// </summary>
		/// <param name="name">Animation name.</param>
		public void PlayAnimationFromRegistry(string name, bool restartIfPlaying)
		{
			if (!_animRegistry.ContainsKey(name))
				return;
			var anim = _animRegistry[name];
			if (restartIfPlaying || _anim != anim)
				Animation = anim;
		}

		/// <summary>
		/// Stop the current animation.
		/// </summary>
		public void StopAnimation()
		{
			_anim.Interval = 0;
		}

		/// <summary>
		/// Source rectangle calculated from constructor values.
		/// </summary>
		public Rectangle SourceRectangle
		{
			get { return new Rectangle((CurrentFrame % _framesInRow) * _frameWidth, (CurrentFrame / _framesInRow) * _frameHeight, _frameWidth, _frameHeight); }
		}

		/// <summary>
		/// Should the animation go backwards?
		/// </summary>
		private bool _negDir;

		/// <summary>
		/// An internal timer.
		/// </summary>
		private float _timer;

		/// <summary>
		/// Update this sprite, calculate frame values.
		/// </summary>
		/// <param name="gameTime">Game time.</param>
		/// <param name="timeScale">Time scale.</param>
		public void Update(GameTime gameTime, float timeScale)
		{
			if (Animation.Interval > 0 && Animation.StartFrame != Animation.EndFrame)
			{
				_timer += (float)gameTime.ElapsedGameTime.TotalSeconds * timeScale;

				if (_timer > Animation.Interval)
				{
					_timer %= Animation.Interval;
					if (_negDir)
					{
						if (CurrentFrame > Animation.StartFrame)
						{
							--CurrentFrame;
						}
						else if (!Animation.HasFlag(Animation.DontRepeat))
						{
							if (Animation.HasFlag(Animation.PingPong))
							{
								// If PingPong active, change the direction
								CurrentFrame = CurrentFrame + 1;
								_negDir = false;
							}
							else
								CurrentFrame = Animation.EndFrame; // Play the anim from the end
						}
					}
					else
					{
						if (CurrentFrame < Animation.EndFrame)
						{
							++CurrentFrame;
						}
						else if (!Animation.HasFlag(Animation.DontRepeat))
						{
							if (Animation.HasFlag(Animation.PingPong))
							{
								// If PingPong active, change the direction
								CurrentFrame = CurrentFrame - 1;
								_negDir = true;
							}
							else
								CurrentFrame = Animation.StartFrame; // Play the anim from the beginning
						}
					}
				}
			}
		}

		/// <summary>
		// Serialize the state of this sprite.
		/// </summary>
		/// <param name="writer">Writer.</param>
		public void SerializeState(System.IO.BinaryWriter writer)
		{
			//Color, effect, scale, anchor
			writer.Write(Color);
			writer.Write((byte)Effect);
			writer.Write(Scale);
			writer.Write(Anchor);
			writer.Write(Animation);
			writer.Write(CurrentFrame);
			writer.Write(_negDir);
			writer.Write(_timer);
		}

		/// <summary>
		/// Deserialize the state of this sprite.
		/// </summary>
		/// <param name="reader">Reader.</param>
		public void DeserializeState(System.IO.BinaryReader reader)
		{
			Color = reader.ReadColor();
			Effect = (SpriteEffects)reader.ReadByte();
			Scale = reader.ReadVector2();
			Anchor = reader.ReadVector2();
			var anim = new Animation();
			anim.Deserialize(reader);
			_anim = anim;
			CurrentFrame = reader.ReadInt32();
			_negDir = reader.ReadBoolean();
			_timer = reader.ReadSingle();
		}

		/// <summary>
		/// Write full sprite info to JSON.
		/// </summary>
		/// <param name="writer">Writer.</param>
		public void ToJson(JsonWriter writer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("asset");
			writer.WriteValue(_textureAsset);
			writer.WritePropertyName("fwidth");
			writer.WriteValue(_frameWidth);
			writer.WritePropertyName("fheight");
			writer.WriteValue(_frameHeight);
			writer.WritePropertyName("scale");
			writer.WriteVector2(Scale);
			writer.WritePropertyName("anchor");
			writer.WriteVector2(Anchor);
			writer.WritePropertyName("color");
			writer.WriteColor(Color);
			writer.WritePropertyName("effect");
			writer.WriteValue(Effect.ToString());
			writer.WritePropertyName("animregistry");
			writer.WriteStartObject();
			_animRegistry.ToList().ForEach(pair =>
			{
				writer.WritePropertyName(pair.Key);
				pair.Value.ToJson(writer);
			});
			writer.WriteEndObject();
			writer.WriteEndObject();
		}

		/// <summary>
		/// Read sprite info from JSON.
		/// </summary>
		/// <param name="obj">JObject.</param>
		public void FromJson(JObject obj)
		{
			_textureAsset = (string)obj["asset"];
			_frameWidth = int.Parse((string)obj["fwidth"]);
			_frameHeight = int.Parse((string)obj["fheight"]);
			Recalculate();
			Scale = Helper.JTokenToVector2(obj["scale"]);
			Anchor = Helper.JTokenToVector2(obj["anchor"]);
			Color = Helper.JTokenToColor(obj["color"]);
			Effect = (SpriteEffects)Enum.Parse(typeof(SpriteEffects), (string)obj["effect"]);
			var registry = (JObject)obj["animregistry"];
			_animRegistry = new Dictionary<string, Animation>();
			foreach (JToken t in registry.Children())
			{
				if (t.Type == JTokenType.Property)
					_animRegistry.Add((t as JProperty).Name, Animation.LoadFromJson((JObject)(t as JProperty).Value));
			}
		}

		/// <summary>
		/// Draw this sprite.
		/// </summary>
		/// <param name="spriteBatch">Sprite batch.</param>
		/// <param name="position">Position.</param>
		/// <param name="depth">Sprite depth.</param>
		public void Draw(SpriteBatch spriteBatch, Vector2 position, float depth)
		{
			spriteBatch.Draw(Texture, position, SourceRectangle, Color, 0f, new Vector2(_frameWidth * Anchor.X, _frameHeight * Anchor.Y), Scale, Effect, depth);
		}

		/// <summary>
		/// Create a clone of this instance.
		/// </summary>
		/// <returns>A clone.</returns>
		public Sprite Clone()
		{
			var s = new Sprite(_textureAsset, _frameWidth, _frameHeight)
			{
				_animRegistry = _animRegistry,
				Effect = Effect,
				Scale = Scale,
				Color = Color,
				CurrentFrame = CurrentFrame,
				Anchor = Anchor,
				Animation = _anim,
			};

			return s;
		}
	}

	/// <summary>
	/// A struct that hold information about an animation.
	/// </summary>
	public struct Animation : ISerializable, IJsonSerializable
	{
		/// <summary>
		/// Flags.
		/// </summary>
		public const byte DontRepeat = 1, PingPong = 2, StartReversed = 4;

		public byte StartFrame;
		public byte EndFrame;

		/// <summary>
		/// Contains flags.
		/// </summary>
		public byte Data;

		public float Interval;

		public Animation(byte startFrame, byte endFrame, float interval, byte data = 0)
		{
			StartFrame = startFrame;
			EndFrame = endFrame;
			Interval = interval;
			Data = data;
		}

		/// <summary>
		/// Check wheter a flag is active
		/// </summary>
		/// <returns><c>true</c>, if active, otherwise <c>false</c>.</returns>
		/// <param name="flag">A flag.</param>
		public bool HasFlag(byte flag)
		{
			return (Data & flag) == flag;
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public void Serialize(System.IO.BinaryWriter writer)
		{
			writer.Write(StartFrame);
			writer.Write(EndFrame);
			writer.Write(Interval);
			writer.Write(Data);
		}

		public void Deserialize(System.IO.BinaryReader reader)
		{
			StartFrame = reader.ReadByte();
			EndFrame = reader.ReadByte();
			Interval = reader.ReadSingle();
			Data = reader.ReadByte();
		}

		public void ToJson(JsonWriter writer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("startframe");
			writer.WriteValue(StartFrame);
			writer.WritePropertyName("endframe");
			writer.WriteValue(EndFrame);
			writer.WritePropertyName("interval");
			writer.WriteValue(Interval);
			writer.WritePropertyName("data");
			writer.WriteValue(Data);
			writer.WriteEndObject();
		}

		public void FromJson(JObject obj)
		{
			StartFrame = (byte)obj["startframe"];
			EndFrame = (byte)obj["endframe"];
			Interval = (float)obj["interval"];
			Data = (byte)obj["data"];
		}

		public static bool operator ==(Animation a, Animation b)
		{
			return a.StartFrame == b.StartFrame && a.EndFrame == b.EndFrame && a.Interval == b.Interval && a.Data == b.Data;
		}

		public static bool operator !=(Animation a, Animation b)
		{
			return a.StartFrame != b.StartFrame || a.EndFrame != b.EndFrame || a.Interval != b.Interval || a.Data != b.Data;
		}

		public static Animation LoadFromJson(JObject obj)
		{
			Animation anim = new Animation();
			anim.FromJson(obj);
			return anim;
		}
	}
}

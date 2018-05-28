using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;

using Newtonsoft.Json.Linq;
using Mff.Totem.Core;

using SpriterDotNet;
using SpriterDotNet.MonoGame;

namespace Mff.Totem
{
	public static class ContentLoader
	{
		/// <summary>
		/// 1x1 texture of a white pixel.
		/// </summary>
		/// <value>Pixel.</value>
		public static Texture2D Pixel
		{
			get;
			private set;
		}

		public static Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
		public static Dictionary<string, Parallax> Parallaxes = new Dictionary<string, Parallax>();
		public static Dictionary<string, SpriteFont> Fonts = new Dictionary<string, SpriteFont>();
		public static Dictionary<string, SoundEffect> Sounds = new Dictionary<string, SoundEffect>();
        public static Dictionary<string, Effect> Shaders = new Dictionary<string, Effect>();

        public static Dictionary<string, Core.Entity> Entities = new Dictionary<string, Core.Entity>();
		public static Dictionary<string, Core.Item> Items = new Dictionary<string, Core.Item>();
		public static Dictionary<string, Core.Sprite> Sprites = new Dictionary<string, Core.Sprite>();
		public static Dictionary<string, SpriteWrapper> BoneSprites = new Dictionary<string, SpriteWrapper>();
		public static Dictionary<string, Core.Particle> Particles = new Dictionary<string, Core.Particle>();

		public static void Load(Core.TotemGame game)
		{
			// Generate a pixel texture
			Pixel = new Texture2D(game.GraphicsDevice, 1, 1);
			Pixel.SetData<Color>(new Color[] { Color.White });

			// Load Textures
			LoadContentFile(game, "textures/", ".xnb", Textures);

			// Load Sounds
			LoadContentFile(game, "sounds/", ".xnb", Sounds);

			// Load Parallaxes
			foreach (string file in FindAllFiles("Content/assets/parallaxes", ".parallax"))
			{
				var name = Path.GetFileNameWithoutExtension(file);
				Console.WriteLine("Loading a parallax: {0}", name);
				using (FileStream stream = new FileStream(file, FileMode.Open))
				using (StreamReader sReader = new StreamReader(stream))
				using (Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(sReader))
				{
					var obj = JObject.Load(reader);
					var array = JArray.FromObject(obj["textures"]);
					var movArray = JArray.FromObject(obj["movable"]);

					var textures = new string[array.Count];
					var movable = new int[movArray.Count];

					for (int i = 0; i < textures.Length; ++i)
						textures[i] = (string)array[i];
					for (int i = 0; i < movable.Length; ++i)
						movable[i] = (int)movArray[i];

					LoadParallax(game, name, movable, textures);
				}
			}

			// Load Fonts
			LoadContentFile(game, "fonts/", ".xnb", Fonts);

            // Load shaders
			Shaders.Add("ground", game.Content.Load<Effect>("shaders/GroundShader"));
			Shaders.Add("menu", game.Content.Load<Effect>("shaders/MenuShader"));
		
			LoadAssetFile(game, "Content/assets/sprites", ".sprite", Sprites, null, "sprite");

			{
				var file = "Content/textures/sprites.scml";
				using (FileStream stream = new FileStream(file, FileMode.Open))
				using (StreamReader sReader = new StreamReader(stream))
				{
					var spriter = SpriterReader.Default.Read(sReader.ReadToEnd());
					var provider = new SpriterDotNet.Providers.DefaultProviderFactory<ISprite, SoundEffect>(new Config() { SoundsEnabled = false });
					foreach (SpriterFolder folder in spriter.Folders)
					{
						foreach (SpriterFile sfile in folder.Files)
						{
							var sprite = new SpriterDotNet.MonoGame.Sprites.TextureSprite(Textures[sfile.Name.Replace(".png", "")]);
							provider.SetSprite(spriter, folder, sfile, sprite);
						}
					}

					// Add wrapper
					foreach (SpriterEntity ent in spriter.Entities)
						BoneSprites.Add(ent.Name, new SpriteWrapper(ent, provider));
				}
			}

			LoadAssetFile(game, "Content/assets/items", ".item", Items, "id");
			LoadAssetFile(game, "Content/assets/entities", ".entity", Entities, null, "entity");
			LoadAssetFile(game, "Content/assets/particles", ".particle", Particles);
		}

		static void LoadContentFile<T>(TotemGame game, string path, string extension, Dictionary<string, T> output)
		{
			var content = "Content/" + path;
			foreach (string file in FindAllFiles(content, extension))
			{
				var dir = Path.GetDirectoryName(file);
				var name = Path.Combine(
					dir.Remove(0, Math.Min(content.Length, dir.Length)),
					Path.GetFileNameWithoutExtension(file)
				).Replace('\\', '/');

				Console.WriteLine("Loading content: {0}", name);
				output.Add(name, game.Content.Load<T>(path + "/" + name));
			}
		}

		static void LoadAssetFile<T>(
			TotemGame game, 
			string path, 
			string extension, 
			Dictionary<string, T> output,
			string customName = null, 
			string forceClass = null) where T : IJsonSerializable
		{
			foreach (string file in FindAllFiles(path, extension))
			{
				var name = Path.GetFileNameWithoutExtension(file);
				Console.WriteLine("Loading asset: {0}", name);
				using (FileStream stream = new FileStream(file, FileMode.Open))
				using (StreamReader sReader = new StreamReader(stream))
				using (Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(sReader))
				{
					var obj = JObject.Load(reader);
					var qualifiedName = customName != null ? (string)obj[customName] : name;
					if (forceClass != null)
					{
						var ent = DeserializationRegister.CreateInstance<T>(forceClass);
						ent.FromJson(obj);
						output.Add(qualifiedName, ent);
					}
					else
						output.Add(qualifiedName, DeserializationRegister.ObjectFromJson<T>(obj));
				}
			}
		}

		static void LoadParallax(Core.TotemGame game, string asset, int[] movable, params string[] layers)
		{
			var parallax = new Parallax(layers.Length);
			for (int i = 0; i < layers.Length; ++i)
			{
				parallax.Textures[i] = Textures[layers[i]];
			}
			for (int i = 0; i < movable.Length; ++i)
				parallax.Offsetable[movable[i]] = true;
			Parallaxes.Add(asset, parallax);
		}

		/// <summary>
		/// Recursively search a path for files with given extension.
		/// </summary>
		/// <returns>Seznam souboru.</returns>
		/// <param name="path">Cesta.</param>
		/// <param name="extension">Pripona.</param>
		private static List<string> FindAllFiles(string path, string extension)
		{
			var list = new List<string>();
			if (!Directory.Exists(path))
				return list;

			foreach (string file in Directory.GetFiles(path))
			{
				if (Path.GetExtension(file) == extension)
					list.Add(file);
			}
			foreach (string subdir in Directory.GetDirectories(path))
			{
				list.AddRange(FindAllFiles(subdir, extension));
			}
			return list;
		}

		public static void RefreshShaders(TotemGame game)
		{
			if (Shaders.ContainsKey("menu"))
				Shaders["menu"].Parameters["Resolution"].SetValue(game.Resolution);
		}
	}

	public class SpriteWrapper
	{
		public IProviderFactory<ISprite, SoundEffect> AssetProvider;
		public SpriterEntity Entity;

		public SpriteWrapper(SpriterEntity ent, IProviderFactory<ISprite, SoundEffect> prov)
		{
			Entity = ent;
			AssetProvider = prov;
		}

		public MonoGameAnimator GetAnimator()
		{
			return new MonoGameAnimator(Entity, AssetProvider);
		}
	}
}

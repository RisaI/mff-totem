﻿using System;
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
			var textureFolder = "Content/textures/";
			foreach (string file in FindAllFiles(textureFolder, ".xnb"))
			{
				var dir = Path.GetDirectoryName(file);
				var name = Path.Combine(
                    dir.Remove(0, Math.Min(textureFolder.Length, dir.Length)),
                    Path.GetFileNameWithoutExtension(file)
                ).Replace('\\', '/');

				Console.WriteLine("Loading texture: {0}", name);
				Textures.Add(name, game.Content.Load<Texture2D>("textures/" + name));
			}

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
			var fontsFolder = "Content/fonts/";
			foreach (string file in FindAllFiles(fontsFolder, ".xnb"))
			{
				var dir = Path.GetDirectoryName(file);
				var name = Path.Combine(
					dir.Remove(0, Math.Min(fontsFolder.Length, dir.Length)),
					Path.GetFileNameWithoutExtension(file)
				).Replace('\\', '/');

				Console.WriteLine("Loading font: {0}", name);
				Fonts.Add(name, game.Content.Load<SpriteFont>("fonts/" + name));
			}

            // Load shaders
			Shaders.Add("ground", game.Content.Load<Effect>("shaders/GroundShader"));
			Shaders.Add("menu", game.Content.Load<Effect>("shaders/MenuShader"));
		
			foreach (string file in FindAllFiles("Content/assets/sprites", ".sprite"))
			{
				var name = Path.GetFileNameWithoutExtension(file);
				Console.WriteLine("Loading a sprite: {0}", name);
				using (FileStream stream = new FileStream(file, FileMode.Open))
				using (StreamReader sReader = new StreamReader(stream))
				using (Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(sReader))
				{
					Sprites.Add(name, new Core.Sprite(JObject.Load(reader)));
				}
            }

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

            // SPRITEs
            foreach (string file in FindAllFiles("Content/sprites", ".scml"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                Console.WriteLine("Loading a sprite: {0}", name);
                using (FileStream stream = new FileStream(file, FileMode.Open))
                using (StreamReader sReader = new StreamReader(stream))
                {
                    var spriter = SpriterReader.Default.Read(sReader.ReadToEnd());
					var provider = new SpriterDotNet.Providers.DefaultProviderFactory<Texture2D, SoundEffect>(new Config() { SoundsEnabled = false });
					foreach (SpriterEntity ent in spriter.Entities) {
						var wrapper = new SpriteWrapper(ent, provider as IProviderFactory<ISprite, SoundEffect>);
					}
                }
            }

			foreach (string file in FindAllFiles("Content/assets/items", ".item"))
			{
				var name = Path.GetFileNameWithoutExtension(file);
				Console.WriteLine("Loading item: {0}", name);
				using (FileStream stream = new FileStream(file, FileMode.Open))
				using (StreamReader sReader = new StreamReader(stream))
				using (Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(sReader))
				{
					var jobj = JObject.Load(reader);
					var item = Core.DeserializationRegister.ObjectFromJson<Core.Item>(jobj);
					Items.Add(item.ID, item);
				}
			}

			foreach (string file in FindAllFiles("Content/assets/entities", ".entity"))
			{
				var name = Path.GetFileNameWithoutExtension(file);
				Console.WriteLine("Loading entity: {0}", name);
				using (FileStream stream = new FileStream(file, FileMode.Open))
				using (StreamReader sReader = new StreamReader(stream))
				using (Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(sReader))
				{
					var ent = new Core.Entity();
					ent.FromJson(JObject.Load(reader));
					Entities.Add(name, ent);
				}
			}

			foreach (string file in FindAllFiles("Content/assets/particles", ".particle"))
			{
				var name = Path.GetFileNameWithoutExtension(file);
				Console.WriteLine("Loading particle: {0}", name);
				using (FileStream stream = new FileStream(file, FileMode.Open))
				using (StreamReader sReader = new StreamReader(stream))
				using (Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(sReader))
				{
					var par = DeserializationRegister.ObjectFromJson<Particle>(JObject.Load(reader));
					Particles.Add(name, par);
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

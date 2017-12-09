using System;
using System.IO;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using Newtonsoft.Json.Linq;

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

		/// <summary>
		/// A generated texture of night sky
		/// </summary>
		/// <value>Night sky.</value>
		public static Texture2D GeneratedStarSky
		{
			get;
			private set;
		}

		public static Texture2D LightTexture
		{
			get;
			private set;
		}

		public static Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
		public static Dictionary<string, Texture2D[]> Parallaxes = new Dictionary<string, Texture2D[]>();
		public static Dictionary<string, SpriteFont> Fonts = new Dictionary<string, SpriteFont>();
        public static Dictionary<string, Effect> Shaders = new Dictionary<string, Effect>();

        public static Dictionary<string, Core.Entity> Entities = new Dictionary<string, Core.Entity>();
		public static Dictionary<string, Core.Item> Items = new Dictionary<string, Core.Item>();
		public static Dictionary<string, Core.Sprite> Sprites = new Dictionary<string, Core.Sprite>();

		public static void Load(Core.TotemGame game)
		{
			// Generate a pixel texture
			Pixel = new Texture2D(game.GraphicsDevice, 1, 1);
			Pixel.SetData<Color>(new Color[] { Color.White });

			LightTexture = Krypton.LightTextureBuilder.CreatePointLight(game.GraphicsDevice, 512);
			GenerateStarSky(game);

			// Load Textures
			var textureFolder = "Content/textures/";
			foreach (string file in FindAllFiles(textureFolder, ".xnb"))
			{
				var dir = Path.GetDirectoryName(file);
				var name = Path.Combine(dir.Remove(0, Math.Min(textureFolder.Length, dir.Length)), Path.GetFileNameWithoutExtension(file));
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
					var textures = new string[array.Count];
					for (int i = 0; i < textures.Length; ++i)
						textures[i] = (string)array[i];
					LoadParallax(game, name, textures);
				}
			}


			// Load SpriteFonts
			Fonts.Add("console", game.Content.Load<SpriteFont>("fonts/console"));
			Fonts.Add("menu", game.Content.Load<SpriteFont>("fonts/menu"));

            // Load shaders
			Shaders.Add("ground", game.Content.Load<Effect>("shaders/GroundShader"));
		
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
		}

		static void LoadParallax(Core.TotemGame game, string asset, params string[] layers)
		{
			Texture2D[] textures = new Texture2D[layers.Length];
			for (int i = 0; i < layers.Length; ++i)
			{
				textures[i] = Textures[layers[i]];
			}
			Parallaxes.Add(asset, textures);
		}

		static void GenerateStarSky(Core.TotemGame game)
		{
			int size = 256;
			GeneratedStarSky = new Texture2D(game.GraphicsDevice, size, size);
			Color[] colorMap = new Color[size * size];
			for (int i = 0; i < size * size; ++i)
			{
				if (Core.TotemGame.Random.Next(280) != 0)
					continue;

				float intensity = 0.3f + 0.7f * (float)Core.TotemGame.Random.NextDouble();
				Color main = Color.Lerp(Color.Transparent, Color.White, intensity),
					secondary = Color.Lerp(main, Color.Transparent, 0.75f);

				colorMap[i] = main;
				if (i >= size)
					colorMap[i - size] = secondary;
				if (i < size * (size - 1))
					colorMap[i + size] = secondary;
				int mod = i % size;
				if (mod > 0)
					colorMap[i - 1] = secondary;
				if (mod < size - 1)
					colorMap[i + 1] = secondary;
			}
			GeneratedStarSky.SetData<Color>(colorMap);
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
	}
}

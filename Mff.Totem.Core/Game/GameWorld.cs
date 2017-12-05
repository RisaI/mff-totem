using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FarseerPhysics.DebugView;
using FarseerPhysics.Dynamics;
using System.IO;
using Microsoft.Xna.Framework.Input;

namespace Mff.Totem.Core
{
	public class GameWorld : IUpdatable, IDrawable, ISerializable
	{
		const float CAMERA_SPEED = 25f;

		/// <summary>
		/// List of entities currently present in this world.
		/// </summary>
		public List<Entity> Entities;

		/// <summary>
		/// Entities queued for adding.
		/// </summary>
		private List<Entity> EntityQueue;

		/// <summary>
		/// List of active particles.
		/// </summary>
		public List<Particle> Particles;

		/// <summary>
		/// The physics engine.
		/// </summary>
		/// <value>The physics.</value>
		public World Physics
		{
			get;
			private set;
		}

		public DebugViewXNA DebugView
		{
			get;
			private set;
		}

		public TotemGame Game
		{
			get;
			private set;
		}

		public Terrain Terrain
		{
			get;
			private set;
		}

		public Background Background
		{
			get;
			set;
		}

		private Weather _weather = Weather.DefaultWeather;
		public Weather Weather
		{
			get { return _weather; }
			set
			{
				_weather = value ?? Weather.DefaultWeather;
			}
		}

		public DateTime WorldTime
		{
			get;
			private set;
		}

		public float TimeScale = 1f;
		public bool CameraControls = true;

		private Camera _camera;
		public Camera Camera
		{
			get { return _camera; }
			set
			{
				if (value != null)
					_camera = value;
			}
		}

		public GameWorld(TotemGame game)
		{
			Game = game;
			Entities = new List<Entity>(512);
			EntityQueue = new List<Entity>(64);

			// Particles
			Particles = new List<Particle>(8192);

			// Physics
			Physics = new World(new Vector2(0, 9.81f));

			// Physical engine debug view
			DebugView = new DebugViewXNA(Physics) { Enabled = true };
			DebugView.LoadContent(Game.GraphicsDevice, Game.Content);

			// Load basic terrain for debugging
			Terrain = new Terrain(this);
			Terrain.Generate();

			// Default camera
			_camera = new Camera(game);
			_camera.Position.Y = Terrain.HeightMap(0);

			WorldTime = new DateTime(2034, 5, 27, 12, 0, 0);

			Background = new Backgrounds.OutsideBG(this);

			PrepareRenderData((int)game.Resolution.X, (int)game.Resolution.Y);
			Game.OnResolutionChange += PrepareRenderData;

			// Make the world less empty
			// CreateEntity("player").GetComponent<BodyComponent>().Position += new Vector2(0, -100);
		}

		/// <summary>
		/// Create an empty entity.
		/// </summary>
		/// <returns>The entity.</returns>
		public Entity CreateEntity()
		{
			return SpawnEntity(new Entity());
		}

		/// <summary>
		/// Create an entity from assets.
		/// </summary>
		/// <returns>The entity.</returns>
		public Entity CreateEntity(string asset)
		{
			return SpawnEntity(ContentLoader.Entities[asset].Clone());
		}

		/// <summary>
		/// Insert the entity into the world.
		/// </summary>
		/// <returns>The entity for chaining.</returns>
		/// <param name="ent">The entity to add.</param>
		public Entity SpawnEntity(Entity ent)
		{
			if (!Entities.Contains(ent) && !EntityQueue.Contains(ent))
				EntityQueue.Add(ent);
			return ent;
		}

        private GameTime GTime;
		public void Update(GameTime gameTime)
		{
            GTime = gameTime;
			WorldTime = WorldTime.AddMinutes(gameTime.ElapsedGameTime.TotalSeconds * TimeScale);

			Terrain.Update();
			EntityQueue.ForEach(e =>
			{
				Entities.Add(e);
				e.Initialize(this);
			});
			EntityQueue.Clear();

			lock (Physics)
			{
				Physics.Step((float)gameTime.ElapsedGameTime.TotalSeconds * TimeScale);
			}
			Entities.ForEach(e => e.Update(gameTime));

			/// Clear and update particles
			Particles.ForEach(p => p.Update(gameTime));
			Particles.RemoveAll(p => p.Remove);

			Weather.Update(this, gameTime);

			if (CameraControls && Camera != null)
			{
				// FIXME Use PlayerInputComponent?

				float multiplier = 1f;

				if (Game.Input.GetInput(Inputs.Sprint, InputState.Down))
					multiplier = 2.5f;

				if (Game.Input.GetInput(Inputs.Left, InputState.Down))
					Camera.Position.X -= CAMERA_SPEED * multiplier;
				if (Game.Input.GetInput(Inputs.Right, InputState.Down))
					Camera.Position.X += CAMERA_SPEED * multiplier;
				if (Game.Input.GetInput(Inputs.Up, InputState.Down))
					Camera.Position.Y -= CAMERA_SPEED * multiplier;
				if (Game.Input.GetInput(Inputs.Down, InputState.Down))
					Camera.Position.Y += CAMERA_SPEED * multiplier;
				if (Game.Input.GetInput(Inputs.Plus, InputState.Down))
					Camera.Rotation += 0.1f;
				if (Game.Input.GetInput(Inputs.Minus, InputState.Down))
					Camera.Rotation -= 0.1f;

				if (Game.Input.GetInput(Inputs.A, InputState.Pressed))
				{
					var worldMPos = Camera.ToWorldSpace(Game.Input.GetPointerInput(0).Position);
					Terrain.CreateDamage(new List<ClipperLib.IntPoint>() {
						new ClipperLib.IntPoint((int)worldMPos.X - 16, (int)worldMPos.Y - 16),
						new ClipperLib.IntPoint((int)worldMPos.X + 16, (int)worldMPos.Y - 16),
						new ClipperLib.IntPoint((int)worldMPos.X + 16, (int)worldMPos.Y + 16),
						new ClipperLib.IntPoint((int)worldMPos.X - 16, (int)worldMPos.Y + 16)
					});
				}
			}

			Terrain.GenerateChunk(Helper.NegDivision((int)Camera.BoundingBox.Left - Terrain.CHUNK_WIDTH * 3, Terrain.CHUNK_WIDTH));
			Terrain.GenerateChunk(Helper.NegDivision((int)Camera.BoundingBox.Right + Terrain.CHUNK_WIDTH * 3, Terrain.CHUNK_WIDTH));
			Terrain.SetActiveRegion((int)(Camera.BoundingBox.Left) - Terrain.CHUNK_WIDTH / 2, (int)(Camera.BoundingBox.Right) + Terrain.CHUNK_WIDTH / 2);

			if (Background != null)
				Background.Update(gameTime);
		}
        
		Effect GroundEffect;
		RenderTarget2D ForegroundTexture, BackgroundTexture;

		private void PrepareRenderData(int width, int height)
		{
			if (GroundEffect == null)
				GroundEffect = ContentLoader.Shaders["ground"];
			GroundEffect.Parameters["Projection"].SetValue(Matrix.CreateOrthographic(width, -height, 0, 1));
            
			if (ForegroundTexture != null)
				ForegroundTexture.Dispose();
			ForegroundTexture = new RenderTarget2D(Game.GraphicsDevice, width, height);

			if (BackgroundTexture != null)
				BackgroundTexture.Dispose();
			BackgroundTexture = new RenderTarget2D(Game.GraphicsDevice, width, height);
		}

        public void Draw(SpriteBatch spriteBatch)
        {
            if (Background != null)
            {
                Game.GraphicsDevice.SetRenderTarget((RenderTarget2D)BackgroundTexture);
                Background.Draw(spriteBatch);
            }

            Game.Penumbra.AmbientColor = Color.Lerp(Color.White, Color.Black, NightTint(WorldTime.Hour));

            Game.GraphicsDevice.SetRenderTarget((RenderTarget2D)ForegroundTexture);
            Game.Penumbra.BeginDraw();
            Game.GraphicsDevice.Clear(Color.Transparent);

            // Ground rendering
            if (Terrain.TriangulatedActiveArea != null)
            {
                VertexPositionColor[] vertices = new VertexPositionColor[Terrain.TriangulatedActiveArea.Count * 3];
                int index = 0;

                Terrain.TriangulatedActiveArea.ForEach(t =>
                {
                    t.ForEach(point => vertices[index++] = new VertexPositionColor(new Vector3(point.X, point.Y, 0), Color.White));
                });

                GroundEffect.Parameters["View"].SetValue(Camera.ViewMatrix *
                    Matrix.CreateTranslation(-Game.Resolution.X / 2, -Game.Resolution.Y / 2, 0));
                GroundEffect.Parameters["Texture"].SetValue(ContentLoader.Textures["dirt"]);

                foreach (EffectPass pass in GroundEffect.Techniques[0].Passes)
                {
                    pass.Apply();
                    Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
                }
            }

            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, Camera != null ? Camera.ViewMatrix : Matrix.Identity);
            Weather.DrawWeatherEffects(this, spriteBatch);
            spriteBatch.End();
            
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, Camera != null ? Camera.ViewMatrix : Matrix.Identity);
            Entities.ForEach(e => e.Draw(spriteBatch));
            Particles.ForEach(p => p.Draw(spriteBatch));
            spriteBatch.End();

            Game.Penumbra.Draw(GTime);


            Game.GraphicsDevice.SetRenderTarget(null);
            spriteBatch.Begin(SpriteSortMode.BackToFront, null, null, null);
            spriteBatch.Draw(BackgroundTexture, Vector2.Zero, null, Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 1f);
            spriteBatch.Draw(ForegroundTexture, Vector2.Zero, null, Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
            spriteBatch.End();

            // Render debug physics view
            if (DebugView.Enabled)
            {
                Matrix proj = Matrix.CreateOrthographic(Game.Resolution.X / 64f, -Game.Resolution.Y / 64f, 0, 1);
                Matrix view = (Camera != null ? Matrix.CreateTranslation(-Camera.Position.X / 64f, -Camera.Position.Y / 64f, 0) : Matrix.Identity) *
                                    Matrix.CreateRotationZ(Camera.Rotation) *
                                    Matrix.CreateScale(Camera.Zoom);
                lock (Physics)
                    DebugView.RenderDebugData(proj, view);
            }
        }

        public float NightTint(double hour)
        {
            return hour <= 4 || hour >= 20 ? 1 : (float)(hour > 8 && hour < 16 ? 0 : 1f - Math.Pow(Math.Cos(Math.PI * (hour / 8 - 1)), 2));
        }

        public void Serialize(BinaryWriter writer)
		{
			// Entities
			writer.Write((Int16)(Entities.Count + EntityQueue.Count));
			Entities.ForEach(e => e.Serialize(writer));
			EntityQueue.ForEach(e => e.Serialize(writer));
		}

		public void Deserialize(BinaryReader reader)
		{
			// Entities
			var count = reader.ReadInt16();
			for (int i = 0; i < count; ++i)
			{
				var ent = CreateEntity();
				ent.Deserialize(reader);
			}
		}
	}
}

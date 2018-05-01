using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

// using Physics2D.DebugView;
using Physics2D.Dynamics;
using System.IO;
using Microsoft.Xna.Framework.Input;
using System.Text;

namespace Mff.Totem.Core
{
	public partial class GameWorld : IUpdatable, IDrawable, ISerializable
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

		public Penumbra.Penumbra Lighting => Game.Lighting;

		/*public DebugViewXNA DebugView
		{
			get;
			private set;
		}*/

		public TotemGame Game
		{
			get { return Session?.Game; }
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

		public GameSession Session
		{
			get;
			private set;
		}

		public float TimeScale = 1f;
		public bool CameraControls;

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

		private PlanetInfo _info;
		public PlanetInfo Planet
		{
			get
			{
				return _info;
			}
			set
			{
				_info = value;
				System.Threading.Tasks.Task.Run(() => { _info.GenerateTextures(this);});
				Physics.Gravity = new Vector2(0, _info.Gravity);
				Terrain.Generate(_info.TerrainSeed);
			}
		}

		GameWorld(GameSession session)
		{
			Session = session;
			Entities = new List<Entity>(512);
			EntityQueue = new List<Entity>(64);

			// Particles
			Particles = new List<Particle>(8192);

			// Physics
			Physics = new World(new Vector2(0, 0));

			// Physical engine debug view
			// DebugView = new DebugViewXNA(Physics) { Enabled = true };
			// DebugView.LoadContent(Game.GraphicsDevice, Game.Content);

			// Load basic terrain for debugging
			Terrain = new Terrain(this);

			// Default camera
			_camera = new Camera(Game);

			Background = new Backgrounds.OutsideBG(this);

			PrepareRenderData((int)Game.Resolution.X, (int)Game.Resolution.Y);
			Game.OnResolutionChange += PrepareRenderData;
		}

        private GameTime GTime;
		public void Update(GameTime gameTime)
		{
            GTime = gameTime;
			Session.UniverseTime = Session.UniverseTime.AddMinutes(gameTime.ElapsedGameTime.TotalSeconds * TimeScale);

			Terrain.Update();

			lock (EntityQueue)
			{
				EntityQueue.ForEach(e =>
				{
					Entities.Add(e);
					e.Initialize(this);
				});
				EntityQueue.Clear();
			}

			lock (Physics)
			{
				Physics.Step((float)gameTime.ElapsedGameTime.TotalSeconds * TimeScale);
			}

			// Update entities
			var activeArea = ActiveArea;
			for (int i = Entities.Count - 1; i >= 0; --i)
			{
				var ent = Entities[i];
				var body = ent.GetComponent<BodyComponent>();

				if (body != null)
					ent.Active = activeArea.Intersects(body.BoundingBox);
				
				ent.Update(gameTime);
				if (ent.Remove)
					Entities.RemoveAt(i);
			}

			/// Clear and update particles
			Particles.ForEach(p => p.Update(gameTime));
			Particles.RemoveAll(p => p.Remove);

			Weather.Update(this, gameTime);

			if (Game.InputEnabled)
			{
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
					/*if (Game.Input.GetInput(Inputs.Plus, InputState.Down))
						Camera.Rotation += 0.1f;
					if (Game.Input.GetInput(Inputs.Minus, InputState.Down))
						Camera.Rotation -= 0.1f;*/

					if (Game.Input.GetInput(Inputs.A, InputState.Pressed))
					{
						var worldMPos = Camera.ToWorldSpace(Game.Input.GetPointerInput(0).Position);
						//SpawnParticle("leaf", worldMPos);
						Terrain.Damage(new List<ClipperLib.IntPoint>() {
							new ClipperLib.IntPoint((int)worldMPos.X - 16, (int)worldMPos.Y - 16),
							new ClipperLib.IntPoint((int)worldMPos.X + 16, (int)worldMPos.Y - 16),
							new ClipperLib.IntPoint((int)worldMPos.X + 16, (int)worldMPos.Y + 16),
							new ClipperLib.IntPoint((int)worldMPos.X - 16, (int)worldMPos.Y + 16)
						});
					}
				}
				if (Game.Input.GetInput(Inputs.Plus, InputState.Down))
					Camera.Zoom += 0.01f;
				if (Game.Input.GetInput(Inputs.Minus, InputState.Down))
					Camera.Zoom = Math.Max(0.1f, Camera.Zoom - 0.01f);
			}

			Terrain.ActiveRegion(Camera.Position);

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

            Game.GraphicsDevice.SetRenderTarget((RenderTarget2D)ForegroundTexture);
			Lighting.BeginDraw();
			Game.GraphicsDevice.Clear(Color.Transparent);

			// Ground rendering
			{
				GroundEffect.Parameters["View"].SetValue(Camera.ViewMatrix *
					Matrix.CreateTranslation(-Game.Resolution.X / 2, -Game.Resolution.Y / 2, 0));
				GroundEffect.Parameters["Texture"].SetValue(ContentLoader.Textures["dirt"]);

				for (int i = 0; i < Terrain.ActiveChunks.Length; ++i)
				{
					var chunk = Terrain.ActiveChunks[i];
					if (chunk == null || chunk.State != Terrain.ChunkStateEnum.Placed)
						continue;

					foreach (EffectPass pass in GroundEffect.Techniques[0].Passes)
					{
						pass.Apply();
						Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, chunk.TriangulatedBackgroundVertices,
																0, chunk.TriangulatedBackgroundVertices.Length / 3);
					}
				}
			}

			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, Camera != null ? Camera.ViewMatrix : Matrix.Identity);
			Weather.DrawWeatherEffects(this, spriteBatch);
			spriteBatch.End();

			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera != null ? Camera.ViewMatrix : Matrix.Identity);
			Entities.ForEach(e => e.Draw(spriteBatch));
			Particles.ForEach(p => p.Draw(spriteBatch));
			spriteBatch.End();

			// Ground rendering
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera != null ? Camera.ViewMatrix : Matrix.Identity);
			{
				for (int i = 0; i < Terrain.ActiveChunks.Length; ++i)
				{
					var chunk = Terrain.ActiveChunks[i];
					if (chunk == null || chunk.State != Terrain.ChunkStateEnum.Placed)
						continue;

					foreach (EffectPass pass in GroundEffect.Techniques[0].Passes)
					{
						pass.Apply();
						Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, chunk.TriangulatedForegroundVertices,
															   0, chunk.TriangulatedForegroundVertices.Length / 3);
					}
					foreach (Terrain.Chunk.GrassPoint g in chunk.GrassPoints)
					{
						var texture = ContentLoader.Textures["grass"];
						spriteBatch.Draw(texture, g.Position, null, Color.Green, g.Rotation, new Vector2(texture.Width / 2, texture.Height), 1.2f, SpriteEffects.None, 1f);
					}
				}
			}
			spriteBatch.End();
			Lighting.Draw(default(GameTime));



            Game.GraphicsDevice.SetRenderTarget(null);
			Game.GraphicsDevice.Clear(Color.Black);
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
            spriteBatch.Draw(BackgroundTexture, Vector2.Zero, null, Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
            spriteBatch.Draw(ForegroundTexture, Vector2.Zero, null, Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 1f);
            spriteBatch.End();

            // Render debug physics view
            /*if (DebugView.Enabled)
            {
				spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, Camera != null ? Camera.ViewMatrix : Matrix.Identity);
				foreach (Entity ent in Entities)
				{
					var body = ent.GetComponent<BodyComponent>();
					if (body != null)
					{
						spriteBatch.DrawRectangle(body.BoundingBox, new Color(255,0,0,80), 0);
					}
				}
				spriteBatch.End();
                Matrix proj = Matrix.CreateOrthographic(Game.Resolution.X / 64f, -Game.Resolution.Y / 64f, 0, 1);
                Matrix view = (Camera != null ? Matrix.CreateTranslation(-Camera.Position.X / 64f, -Camera.Position.Y / 64f, 0) : Matrix.Identity) *
                                    Matrix.CreateRotationZ(Camera.Rotation) *
                                    Matrix.CreateScale(Camera.Zoom);
                lock (Physics)
                    DebugView.RenderDebugData(proj, view);

				StringBuilder text = new StringBuilder();
				var mpos = Camera.ToWorldSpace(Game.Input.GetPointerInput(0).Position);
				text.AppendFormat("Mouse position: {0}, {1}", (int)mpos.X, (int)mpos.Y);
				var font = ContentLoader.Fonts["menu"];
				spriteBatch.Begin();
				spriteBatch.DrawString(font, text, new Vector2(Game.Resolution.X, 0), Color.White, 0, new Vector2(font.MeasureString(text).X, 0), 0.3f, SpriteEffects.None, 1f);
				spriteBatch.End();
            }*/
        }

        public float NightTint(double hour)
        {
            return hour <= 4 || hour >= 20 ? 1 : (float)(hour > 8 && hour < 16 ? 0 : 1f - Math.Pow(Math.Cos(Math.PI * (hour / 8 - 1)), 2));
        }

		const int ACTIVE_AREA = 14 * 512;
		public Rectangle ActiveArea
		{
			get
			{
				int x = Camera != null ? (int)Camera.Position.X : 0,
					y = Camera != null ? (int)Camera.Position.Y : 0;
				return new Rectangle(
					x - ACTIVE_AREA / 2,
					y - ACTIVE_AREA / 2,
					ACTIVE_AREA,
					ACTIVE_AREA
				);
			}
		}
	}
}

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

		public DateTime WorldTime
		{
			get;
			private set;
		}

		public float TimeScale = 1f;
		public bool CameraControls = false;

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

			WorldTime = new DateTime(2034, 5, 27, 12, 0, 0);

			Background = new Backgrounds.BlankOutsideBG(this);

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

		public void Update(GameTime gameTime)
		{
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
			Particles.RemoveAll(p => p.Remove);
			Particles.ForEach(p => p.Update(gameTime));

			if (CameraControls && Camera != null)
			{
				// FIXME Use PlayerInputComponent?

				float multiplier = 1f;

				if (Input.KBState.IsKeyDown(Keys.LeftShift))
					multiplier = 2.5f;

				if (Input.KBState.IsKeyDown(Keys.A))
					Camera.Position.X -= CAMERA_SPEED * multiplier;
				if (Input.KBState.IsKeyDown(Keys.D))
					Camera.Position.X += CAMERA_SPEED * multiplier;
				if (Input.KBState.IsKeyDown(Keys.W))
					Camera.Position.Y -= CAMERA_SPEED * multiplier;
				if (Input.KBState.IsKeyDown(Keys.S))
					Camera.Position.Y += CAMERA_SPEED * multiplier;
			}

			Terrain.GenerateChunk(Helper.NegDivision((int)Camera.Left - Terrain.CHUNK_WIDTH * 3, Terrain.CHUNK_WIDTH));
			Terrain.GenerateChunk(Helper.NegDivision((int)Camera.Right + Terrain.CHUNK_WIDTH * 3, Terrain.CHUNK_WIDTH));
			Terrain.SetActiveRegion((int)(Camera.Left) - Terrain.CHUNK_WIDTH / 2, (int)(Camera.Right) + Terrain.CHUNK_WIDTH / 2);

			if (Background != null)
				Background.Update(gameTime);
		}


		DepthStencilState MaskStencil = new DepthStencilState()
		{
			StencilEnable = true,
			StencilFunction = CompareFunction.Always,
			StencilPass = StencilOperation.Replace,
			ReferenceStencil = 1,
			DepthBufferEnable = false
		},
		GroundStencil = new DepthStencilState()
		{
			StencilEnable = true,
			StencilFunction = CompareFunction.Equal,
			StencilPass = StencilOperation.Zero,
			ReferenceStencil = 1,
			DepthBufferEnable = false
		};
		AlphaTestEffect AlphaTest;
		BasicEffect GroundEffect;
		RenderTarget2D GroundMaskTexture, SkyTexture;

		private void PrepareRenderData(int width, int height)
		{
			if (GroundEffect == null)
				GroundEffect = new BasicEffect(Game.GraphicsDevice) { VertexColorEnabled = true };
			GroundEffect.Projection = Matrix.CreateOrthographic(width, -height, 0, 1);

			if (AlphaTest == null)
				AlphaTest = new AlphaTestEffect(Game.GraphicsDevice);
			AlphaTest.Projection = Matrix.CreateOrthographic(width, -height, 0, 1);
			AlphaTest.View = Matrix.CreateTranslation(-width / 2, -height / 2, 0);

			if (GroundMaskTexture != null)
				GroundMaskTexture.Dispose();
			GroundMaskTexture = new RenderTarget2D(Game.GraphicsDevice, width, height);

			if (SkyTexture != null)
				SkyTexture.Dispose();
			SkyTexture = new RenderTarget2D(Game.GraphicsDevice, width, height);
		}

		public void Draw(SpriteBatch spriteBatch)
		{

			// Ground rendering
			if (Terrain.TriangulatedActiveArea != null)
			{
				Game.GraphicsDevice.SetRenderTarget((RenderTarget2D)GroundMaskTexture);
				Game.GraphicsDevice.Clear(Color.Transparent);

				VertexPositionColor[] vertices = new VertexPositionColor[Terrain.TriangulatedActiveArea.Count * 3];
				int index = 0;

				Terrain.TriangulatedActiveArea.ForEach(t =>
				{
					t.ForEach(point => vertices[index++] = new VertexPositionColor(new Vector3(point.X, point.Y, 0), Color.White));
				});

				GroundEffect.View = Camera.ViewMatrix *
					Matrix.CreateTranslation(-Game.Resolution.X / 2, -Game.Resolution.Y / 2, 0);

				foreach (EffectPass pass in GroundEffect.Techniques[0].Passes)
				{
					pass.Apply();
					Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
				}

				Game.GraphicsDevice.SetRenderTarget(null);

				if (Background != null)
				{
					Game.GraphicsDevice.SetRenderTarget((RenderTarget2D)SkyTexture);
					Background.Draw(spriteBatch);
					Game.GraphicsDevice.SetRenderTarget(null);
					spriteBatch.Begin(SpriteSortMode.BackToFront);
					spriteBatch.Draw(SkyTexture, Vector2.Zero, Color.White);
					spriteBatch.End();
				}

				spriteBatch.Begin(SpriteSortMode.BackToFront, null, null, MaskStencil, null, AlphaTest);
				spriteBatch.Draw(GroundMaskTexture, Vector2.Zero, Color.White);
				spriteBatch.End();

				var groundTexture = ContentLoader.Textures["dirt"];
				int x = (int)(Game.Resolution.X / groundTexture.Width) + 2,
					y = (int)(Game.Resolution.Y / groundTexture.Height) + 2;
				spriteBatch.Begin(SpriteSortMode.BackToFront, null, null, GroundStencil);
				for (int a1 = 0; a1 < y; ++a1)
				{
					for (int a0 = 0; a0 < x; ++a0)
					{
						spriteBatch.Draw(groundTexture, groundTexture.Size() * new Vector2(a0, a1) -
										 new Vector2(Helper.NegModulo((int)Camera.Position.X, groundTexture.Width),
													 Helper.NegModulo((int)Camera.Position.Y, groundTexture.Height)), null, Color.White);
					}
				}
				spriteBatch.End();
			}
			else
			{
				if (Background != null)
					Background.Draw(spriteBatch);
				else
					Game.GraphicsDevice.Clear(Color.Black);
			}

			// Render debug physics view
			if (DebugView.Enabled)
			{
				Matrix proj = Matrix.CreateOrthographic(Game.Resolution.X / 64f, -Game.Resolution.Y / 64f, 0, 1);
				Matrix view = Matrix.CreateScale(Camera.Zoom) * 
				                    Matrix.CreateRotationZ(Camera.Rotation) * 
				                    (Camera != null ? Matrix.CreateTranslation(-Camera.Position.X / 64f, -Camera.Position.Y / 64f, 0) : Matrix.Identity);
				lock (Physics)
					DebugView.RenderDebugData(proj, view);
			}

			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, Camera != null ? Camera.ViewMatrix : Matrix.Identity);
			Entities.ForEach(e => e.Draw(spriteBatch));
			Particles.ForEach(p => p.Draw(spriteBatch));
			spriteBatch.End();
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

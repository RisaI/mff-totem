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

				Terrain.GenerateChunk(Helper.NegDivision((int)Camera.Left - 512, Terrain.CHUNK_WIDTH));
				Terrain.GenerateChunk(Helper.NegDivision((int)Camera.Right + 512, Terrain.CHUNK_WIDTH));
				Terrain.SetActiveRegion((int)(Camera.Left), (int)(Camera.Right));
			}
		}

		public void Draw(SpriteBatch spriteBatch)
		{
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

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FarseerPhysics.DebugView;
using FarseerPhysics.Dynamics;

namespace Mff.Totem.Core
{
	public class GameWorld : IUpdatable, IDrawable
	{
		/// <summary>
		/// List of entities currently present in this world.
		/// </summary>
		public List<Entity> Entities;

		/// <summary>
		/// Entities queued for adding.
		/// </summary>
		private List<Entity> EntityQueue;

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

			// Physics
			Physics = new World(new Vector2(0, 9.81f));

			// Physical engine debug view
			DebugView = new DebugViewXNA(Physics) { Enabled = true };
			DebugView.LoadContent(Game.GraphicsDevice, Game.Content);

			// Load basic terrain for debugging
			Terrain = new Terrain(this);
			Terrain.Generate();
			Terrain.PlaceInWorld();

			// Default camera
			_camera = new Camera(game);

			// Make the world less empty
			CreateEntity("human").GetComponent<BodyComponent>().Position += new Vector2(128, 0);
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
			EntityQueue.ForEach(e =>
			{
				Entities.Add(e);
				e.Initialize(this);
			});
			EntityQueue.Clear();

			Physics.Step((float)gameTime.ElapsedGameTime.TotalSeconds * TimeScale);
			Entities.ForEach(e => e.Update(gameTime));
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
				DebugView.RenderDebugData(proj, view);
			}

			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, Camera != null ? Camera.ViewMatrix : Matrix.Identity);
			Entities.ForEach(e => e.Draw(spriteBatch));
			spriteBatch.End();
		}
	}
}

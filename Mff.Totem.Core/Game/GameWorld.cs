using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
		/// The physics engine.
		/// </summary>
		/// <value>The physics.</value>
		public World Physics
		{
			get;
			private set;
		}

		public TotemGame Game
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
			Entities = new List<Entity>();
			Physics = new World(Vector2.Zero);
			_camera = new Camera(game);
		}

		/// <summary>
		/// Create a new empty entity.
		/// </summary>
		/// <returns>The entity.</returns>
		public Entity CreateEntity()
		{
			var ent = new Entity();
			return SpawnEntity(ent);
		}

		/// <summary>
		/// Insert the entity into the world.
		/// </summary>
		/// <returns>The entity for chaining.</returns>
		/// <param name="ent">The entity to add.</param>
		public Entity SpawnEntity(Entity ent)
		{
			if (!Entities.Contains(ent))
				Entities.Add(ent);
			ent.Initialize(this);
			return ent;
		}

		public void Update(GameTime gameTime)
		{
			Physics.Step((float)gameTime.ElapsedGameTime.TotalSeconds * TimeScale);
			Entities.ForEach(e => e.Update(gameTime));
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, null, null, Camera != null ? Camera.ViewMatrix : default(Matrix));
			Entities.ForEach(e => e.Draw(spriteBatch));
			spriteBatch.End();
		}
	}
}

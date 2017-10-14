using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public class GameWorld : IUpdatable, IDrawable
	{
		/// <summary>
		/// List of entities currently present in this world.
		/// </summary>
		public List<Entity> Entities;

		public GameWorld()
		{
			Entities = new List<Entity>();
		}

		/// <summary>
		/// Create a new empty entity.
		/// </summary>
		/// <returns>The entity.</returns>
		public Entity CreateEntity()
		{
			var ent = new Entity(this);
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
			return ent;
		}

		public void Update(GameTime gameTime)
		{
			Entities.ForEach(e => e.Update(gameTime));
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
			Entities.ForEach(e => e.Draw(spriteBatch));
			spriteBatch.End();
		}
	}
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public partial class GameWorld
	{
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
			lock (EntityQueue)
			{
				if (!Entities.Contains(ent) && !EntityQueue.Contains(ent))
					EntityQueue.Add(ent);
			}
			return ent;
		}

		public Particle SpawnParticle(string asset, Vector2 position)
		{
			var p = ContentLoader.Particles[asset].Clone();
			p.Position = position;
			Particles.Add(p);
			p.Spawn(this);
			return p;
		}

		public Entity GetEntity(Guid uid)
		{
			return Entities.Find(e => e.UID == uid) ?? EntityQueue.Find(e => e.UID == uid);
		}

		public void EntitiesInRange(Vector2 position, float range, Predicate<Entity> cont)
		{
			for (int i = 0; i < Entities.Count; ++i)
			{
				var p = Entities[i].Position;
				if (p.HasValue && 
				    Math.Abs(p.Value.X - position.X) < range && 
				    Math.Abs(p.Value.Y - position.Y) < range)
					if (!cont(Entities[i]))
						return;
			}
		}

		public void EntitiesAt(Vector2 position, Predicate<Entity> cont)
		{
			for (int i = 0; i < Entities.Count; ++i)
			{
				var body = Entities[i].GetComponent<BodyComponent>();
				if (body.BoundingBox.Contains(position.ToPoint()))
					if (!cont(Entities[i]))
						return;
			}
		}

		public void EntitiesAt(Rectangle area, Predicate<Entity> cont)
		{
			for (int i = 0; i < Entities.Count; ++i)
			{
				var body = Entities[i].GetComponent<BodyComponent>();
				if (body.BoundingBox.Intersects(area))
					if (!cont(Entities[i]))
						return;
			}
		}

		public Entity FirstEntity(Func<Entity, bool> f)
		{
			return Entities.Find(e => f.Invoke(e));
		}
	}
}

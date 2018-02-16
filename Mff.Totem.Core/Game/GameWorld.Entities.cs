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

		public IEnumerable<Entity> FindEntities(Func<Entity, bool> f)
		{
			for (int i = 0; i < Entities.Count; ++i)
				if (f.Invoke(Entities[i]))
					yield return Entities[i];
		}

		public IEnumerable<Entity> FindEntitiesInRange(Vector2 position, float range)
		{
			for (int i = 0; i < Entities.Count; ++i)
			{
				var p = Entities[i].Position;
				if (p.HasValue && Math.Abs(p.Value.X - position.X) < range && Math.Abs(p.Value.Y - position.Y) < range)
					yield return Entities[i];
			}
		}

		public IEnumerable<Entity> FindEntitiesAt(Vector2 position)
		{
			for (int i = 0; i < Entities.Count; ++i)
			{
				var body = Entities[i].GetComponent<BodyComponent>();
				if (body.BoundingBox.Contains(position.ToPoint()))
					yield return Entities[i];
			}
		}

		public IEnumerable<Entity> FindEntitiesAt(Rectangle area)
		{
			for (int i = 0; i < Entities.Count; ++i)
			{
				var body = Entities[i].GetComponent<BodyComponent>();
				if (body.BoundingBox.Intersects(area))
					yield return Entities[i];
			}
		}

		public Entity FirstEntity(Func<Entity, bool> f)
		{
			return Entities.Find(e => f.Invoke(e));
		}
	}
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public abstract class EntityComponent : ICloneable<EntityComponent>
	{
		public Entity Parent
		{
			get;
			private set;
		}

		public GameWorld World
		{
			get { return Parent != null ? Parent.World : World; }
		}

		/// <summary>
		/// Attach an entity to this component.
		/// </summary>
		/// <param name="ent">Entity.</param>
		public void Attach(Entity ent)
		{
			OnEntityAttach(ent);
			if (Parent != null)
				Parent.RemoveComponent(this);
			Parent = ent;
		}

		/// <summary>
		/// Called when an entity is attached to this component.
		/// </summary>
		/// <param name="entity">Entity.</param>
		protected abstract void OnEntityAttach(Entity entity);

		/// <summary>
		/// Called when an entity is spawned into a world.
		/// </summary>
		public abstract void Initialize();

		/// <summary>
		/// Called when parent entity gets destroyed.
		/// </summary>
		public abstract void Destroy();

		/// <summary>
		/// Clone this instance.
		/// </summary>
		/// <returns>The clone.</returns>
		public abstract EntityComponent Clone();
	}
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public abstract class EntityComponent
	{
		public Entity Parent
		{
			get;
			private set;
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
	}
}

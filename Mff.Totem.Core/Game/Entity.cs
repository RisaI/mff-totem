using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public sealed class Entity
	{
		/// <summary>
		/// A unique ID of this entity.
		/// </summary>
		public Guid UID
		{
			get;
			private set;
		}

		/// <summary>
		/// A reference to a GameWorld instance this entity resides in.
		/// </summary>
		public GameWorld World
		{
			get;
			private set;
		}

		/// <summary>
		/// A list of entity components that belong to this entity.
		/// </summary>
		private List<EntityComponent> Components;

		public Entity()
		{
			UID = Guid.NewGuid();
			Components = new List<EntityComponent>();
		}

		public Entity(GameWorld gameWorld) : this()
		{
			Initialize(gameWorld);
		}

		/// <summary>
		/// Adds a component to this entity and attaches it.
		/// </summary>
		/// <param name="component">Component.</param>
		public void AddComponent(EntityComponent component)
		{
			if (!Components.Contains(component))
			{
				Components.Add(component);
				component.Attach(this);
			}
		}

		/// <summary>
		/// Get component by type.
		/// </summary>
		/// <returns>The component.</returns>
		/// <typeparam name="T">Type.</typeparam>
		public T GetComponent<T>() where T : EntityComponent
		{
			for (int i = 0; i < Components.Count; ++i)
			{
				if (Components[i] is T)
					return (T)Components[i];
			}
			return null;
		}

		/// <summary>
		/// Remove a specific component from this entity.
		/// </summary>
		/// <param name="component">Component.</param>
		public void RemoveComponent(EntityComponent component)
		{
			Components.Remove(component);
		}

		/// <summary>
		/// Remove all components of a type.
		/// </summary>
		/// <typeparam name="T">Component type.</typeparam>
		public void RemoveComponents<T>() where T : EntityComponent
		{
			Components.RemoveAll(c => c is T);
		}

		/// <summary>
		/// Initialize this entity in a GameWorld.
		/// </summary>
		/// <param name="world">World.</param>
		public void Initialize(GameWorld world)
		{
			World = world;
		}

		public void Update(GameTime gameTime)
		{
			for (int i = 0; i < Components.Count; ++i)
			{
				var updatable = Components[i] as IUpdatable;
				if (updatable != null)
					updatable.Update(gameTime);
			}
		}

		public void Draw(SpriteBatch spriteBatch)
		{

			for (int i = 0; i < Components.Count; ++i)
			{
				var drawable = Components[i] as IDrawable;
				if (drawable != null)
					drawable.Draw(spriteBatch);
			}
		}
	}
}

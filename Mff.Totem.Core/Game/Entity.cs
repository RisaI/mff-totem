using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public sealed class Entity : IUpdatable, IDrawable, ICloneable<Entity>
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

		/// <summary>
		/// Adds a component to this entity and attaches it.
		/// </summary>
		/// <param name="component">Component.</param>
		/// <returns>The entity for chaining.</returns>
		public Entity AddComponent(EntityComponent component)
		{
			if (!Components.Contains(component))
			{
				Components.Add(component);
				component.Attach(this);
				if (World != null)
					component.Initialize();
			}
			return this;
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
		/// <returns>The entity for chaining.</returns>
		public Entity Initialize(GameWorld world)
		{
			World = world;
			Components.ForEach(c => c.Initialize());
			return this;
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

		/// <summary>
		/// Destroy this entity and it's components.
		/// </summary>
		public void Destroy()
		{
			Components.ForEach(c => c.Destroy());
		}

		/// <summary>
		/// Clone this entity and it's components. Does not preserve UID.
		/// </summary>
		/// <returns>The clone.</returns>
		public Entity Clone()
		{
			var entity = new Entity();
			Components.ForEach(c => entity.AddComponent(c.Clone()));
			return entity;
		}
	}
}

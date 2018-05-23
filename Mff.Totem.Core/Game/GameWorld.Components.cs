using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public partial class GameWorld 
	{
		List<WorldComponent> Components = new List<WorldComponent>(32);

		public int DrawLayer;

		public T GetComponent<T>() where T : WorldComponent
		{
			return Components.Find(c => c is T) as T;
		}

		public GameWorld AddComponent(WorldComponent component)
		{
			if (!Components.Contains(component))
			{
				Components.Add(component);
				component.Attach(this);
				component.Initialize();
			}
			return this;
		}

		public void SetActiveArea(Vector2 position)
		{
			Components.ForEach(c => c.SetActiveArea(position));
		}
	}
}
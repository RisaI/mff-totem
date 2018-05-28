using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public partial class GameWorld 
	{
		List<WorldComponent> Components = new List<WorldComponent>(32);

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

		public int DrawLayer;
		public void DrawComponentLayer(SpriteBatch spriteBatch, int layer)
		{
			DrawLayer = layer;
			Components.ForEach(c =>
			{
				var draw = c as IDrawable;
				if (draw != null)
				{
					draw.Draw(spriteBatch);
				}
			});

		}
	}
}
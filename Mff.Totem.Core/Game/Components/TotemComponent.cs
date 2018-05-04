using System;
using Microsoft.Xna.Framework;
namespace Mff.Totem.Core
{
	[Serializable("component_totem")]
	public class TotemComponent : EntityComponent, IInteractive
	{
		public TotemComponent()
		{
		}

		public override EntityComponent Clone()
		{
			return new TotemComponent();
		}

		public void Interact(Entity ent)
		{
			World.Game.Hud.Chat("This doesn't do anything yet ;)", Color.LightBlue);	
		}
	}
}

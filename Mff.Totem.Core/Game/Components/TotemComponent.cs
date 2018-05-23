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

	    public override void Serialize(System.IO.BinaryWriter writer)
		{
			return;
		}

		public override void Deserialize(System.IO.BinaryReader reader)
		{
			return;
		}

		public override bool DisableEntitySaving
		{
			get
			{
				return true;
			}
		}
	}
}

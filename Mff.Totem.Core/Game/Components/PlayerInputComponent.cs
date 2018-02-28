using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	[Serializable("component_player_input")]
	public class PlayerInputComponent : EntityComponent, IUpdatable
	{
		public PlayerInputComponent()
		{
		}

		public override EntityComponent Clone()
		{
			return new PlayerInputComponent();
		}

		private BodyComponent body;
		private SpriteComponent sprite;
		public override void Initialize()
		{
			body = Parent.GetComponent<BodyComponent>();
			sprite = Parent.GetComponent<SpriteComponent>();
		}

		public void Update(GameTime gameTime)
		{
			World.Camera.MoveTo(body.Position, 0.3f);
			World.Game.Hud.Observed = Parent;

			Vector2 movement = Vector2.Zero;
			var character = Parent.GetComponent<CharacterComponent>();
            var inventory = Parent.GetComponent<InventoryComponent>();

            if (World.Game.InputEnabled)
            {
				if (World.Game.Input.GetInput(Inputs.Right, InputState.Down))
					movement.X += character != null ? character.Speed : 120;

                if (World.Game.Input.GetInput(Inputs.Left, InputState.Down))
					movement.X -= character != null ? character.Speed : 120;

				if (World.Game.Input.GetInput(Inputs.Up, InputState.Pressed))
                    movement.Y = -100;

				if (World.Game.Input.GetInput(Inputs.Inventory, InputState.Pressed))
				{
					var g = World.Game.GuiManager.GetGuiOfType<Gui.InventoryScreen>();
					if (g == null)
						World.Game.GuiManager.Add(new Gui.InventoryScreen(Parent));
					else
						g.Closing = true;
				}

                character.Target = World.Camera.ToWorldSpace(World.Game.Input.GetPointerInput(0).Position) - body.Position;

				if (World.Game.Input.GetInput(Inputs.A, InputState.Down) && inventory != null)
                {
                    inventory.Use(0);
                }

				if (World.Game.Input.GetInput(Inputs.Swap, InputState.Pressed) && inventory != null)
				{
					if (inventory.UseItems.Length >= 2)
					{
						var i = inventory.UseItems[0];
						inventory.UseItems[0] = inventory.UseItems[1];
						inventory.UseItems[1] = i;
					}
				}

				if (World.Game.Input.GetInput(Inputs.Use, InputState.Pressed))
				{
					foreach (Entity ent in World.FindEntitiesAt(body.BoundingBox))
					{
						if (ent.Interact(Parent))
							break;
					}
				}


            }

			if (Math.Abs(movement.X) > Helper.EPSILON)
			{
				sprite.PlayAnim("move");
				sprite.Effect = movement.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			}
			else
			{
				sprite.PlayAnim("idle");
			}

			body.Move(movement);
		}
	}
}

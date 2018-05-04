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

		Penumbra.PointLight light;
		public override void Initialize()
		{
			light = new Penumbra.PointLight();
			light.Scale = new Vector2(256);
			light.Color = Color.White;
			light.ShadowType = Penumbra.ShadowType.Solid;
			World.Lighting.Lights.Add(light);
		}

		public void Update(GameTime gameTime)
		{
			var body   = Parent.GetComponent<BodyComponent>();
			var sprite = Parent.GetComponent<SpriterComponent>();

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
						{
							sprite.PlayAnim("use", true);
							break;
						}
					}
				}
            }

			if (!(body is HumanoidBody) || (body as HumanoidBody).OnGround().HasValue)
			{
				if (Math.Abs(movement.X) > Helper.EPSILON)
				{
					sprite.PlayAnim("walk", false, 200);
					sprite.Effect = movement.X > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				}
				else
				{
					if ((sprite.Sprite.CurrentAnimation?.Name != "use" && sprite.Sprite.NextAnimation?.Name != "use") || sprite.Sprite.Progress >= 1f)
						sprite.PlayAnim("idle", false, 200);
				}
			}
			else
			{
				if (body.LinearVelocity.Y < -4)
				{
					sprite.PlayAnim("jump_start", false, 0);
				}
				else if (body.LinearVelocity.Y > 4)
				{
					sprite.PlayAnim("fall_start", false, 0);
				}
			}

			body.Move(movement);
			light.Position = body.Position;
		}
	}
}

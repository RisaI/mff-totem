using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
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

		SoundEffectInstance jump;
		Penumbra.PointLight light;
		public override void Initialize()
		{
			light = new Penumbra.PointLight();
			light.Scale = new Vector2(256);
			light.Color = Color.White;
			light.ShadowType = Penumbra.ShadowType.Solid;
			World.Lighting.Lights.Add(light);

			jump = ContentLoader.Sounds["jump"].CreateInstance();
		}

		public void Update(GameTime gameTime)
		{
			var body = Parent.GetComponent<BodyComponent>();
			var sprite = Parent.GetComponent<SpriterComponent>();

			World.Camera.MoveTo(body.Position, 0.3f);
			World.Game.Hud.Observed = Parent;

			Vector2 movement = Vector2.Zero;
			var character = Parent.GetComponent<CharacterComponent>();
			var inventory = Parent.GetComponent<InventoryComponent>();

			character.IsWalking = false;
			if (character.IsJumping && body.Grounded)
				character.IsJumping = false;

			if (World.Game.InputEnabled)
			{
				// Crouch
				if (World.Game.Input.GetInput(Inputs.Down, InputState.Pressed))
				{
					if (!character.IsCrouching && !character.QueueContains<CrouchAction>())
						character.AddToQueue(new CrouchAction());
				}
				else if (World.Game.Input.GetInput(Inputs.Down, InputState.Released))
				{
					if (character.IsCrouching && !character.QueueContains<StandUpAction>())
						character.AddToQueue(new StandUpAction());
				}

				if (!character.IsCrouching && character.QueueEmpty)
				{
					if (World.Game.Input.GetInput(Inputs.Right, InputState.Down))
						movement.X += character != null ? character.Speed : 120;

					if (World.Game.Input.GetInput(Inputs.Left, InputState.Down))
						movement.X -= character != null ? character.Speed : 120;

					// Jump
					if (World.Game.Input.GetInput(Inputs.Up, InputState.Pressed) && !character.IsJumping)
					{
						sprite.PlayAnim("jump_start", false, 0);
						jump.Play();
						movement.Y = -100;
						character.IsJumping = true;
					}
					else if (character.IsJumping)
					{
						if (body.Grounded)
							character.IsJumping = false;
					}

					if (body.Grounded)
					{
						if (Math.Abs(movement.X) > Helper.EPSILON)
						{
							character.IsWalking = true;
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
						if (body.LinearVelocity.Y > 4)
						{
							sprite.PlayAnim("fall_start", false, 0);
						}
					}

					body.Move(movement);
				}

				// --- REGION: State independent ---

				// Open inventory screen
				if (World.Game.Input.GetInput(Inputs.Inventory, InputState.Pressed))
				{
					var g = World.Game.GuiManager.GetGuiOfType<Gui.InventoryScreen>();
					if (g == null)
						World.Game.GuiManager.Add(new Gui.InventoryScreen(Parent));
					else
						g.Closing = true;
				}

				character.Target = World.Camera.ToWorldSpace(World.Game.Input.GetPointerInput(0).Position) - body.Position;

				if (World.Game.Input.GetInput(Inputs.A, InputState.Down) &&
					!World.Game.GuiManager.PointerInGui(World.Game.Input.GetPointerInput(0)) && inventory != null)
				{
					inventory.Use(0);
				}

				// Swap weapons
				if (World.Game.Input.GetInput(Inputs.Swap, InputState.Pressed) && inventory != null)
				{
					if (inventory.UseItems.Length >= 2)
					{
						var i = inventory.UseItems[0];
						inventory.UseItems[0] = inventory.UseItems[1];
						inventory.UseItems[1] = i;
					}
				}

				// Use entity
				if (World.Game.Input.GetInput(Inputs.Use, InputState.Pressed))
				{
					World.EntitiesAt(body.BoundingBox, (ent) =>
					{
						if (ent.Interact(Parent))
						{
							sprite.PlayAnim("use", true);
							return false;
						}
						return true;
					});
				}
			}

			// Final positioning things regardles of state and input
			light.Position = body.Position;
		}

		public override void Serialize(System.IO.BinaryWriter writer)
		{
			return;
		}

		public override void Deserialize(System.IO.BinaryReader reader)
		{
			return;
		}
	}

	public class CrouchAction : CharacterAction
	{
		const float CROUCH_TIME = 0.25f;
		public override bool Remove
		{
			get
			{
				return LifeTime >= CROUCH_TIME;
			}
		}

		float LifeTime;
		public override void Update(GameTime gameTime)
		{
			float prevLifeTime = LifeTime;
			LifeTime += (float)gameTime.ElapsedGameTime.TotalSeconds * World.TimeScale;
			if (LifeTime >= CROUCH_TIME && prevLifeTime < CROUCH_TIME)
			{
				var sprite = Parent.GetComponent<SpriterComponent>();
				if (sprite != null)
					sprite.PlayAnim("crouch_idle", false);
			}
		}

		protected override void Initialize()
		{
			ParentComponent.IsCrouching = true;

			var sprite = Parent.GetComponent<SpriterComponent>();
			if (sprite != null)
				sprite.PlayAnim("crouch_down", true);
		}
	}

	public class StandUpAction : CharacterAction
	{
		const float STANDUP_TIME = 0.35f;
		public override bool Remove
		{
			get
			{
				return LifeTime >= STANDUP_TIME;
			}
		}

		float LifeTime;
		public override void Update(GameTime gameTime)
		{
			float prevLifeTime = LifeTime;
			LifeTime += (float)gameTime.ElapsedGameTime.TotalSeconds * World.TimeScale;
			if (LifeTime >= STANDUP_TIME && prevLifeTime < STANDUP_TIME)
			{
				var sprite = Parent.GetComponent<SpriterComponent>();
				if (sprite != null)
					sprite.PlayAnim("idle", true);
			}
		}

		protected override void Initialize()
		{
			ParentComponent.IsCrouching = false;
			var sprite = Parent.GetComponent<SpriterComponent>();
			if (sprite != null)
				sprite.PlayAnim("stand_up", true);
		}
	}
}

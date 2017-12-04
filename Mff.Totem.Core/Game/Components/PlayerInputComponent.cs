using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	[Serializable("player_input")]
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

			Vector2 movement = Vector2.Zero;

            if (World.Game.InputEnabled)
            {
				if (World.Game.Input.GetInput(Inputs.Right, InputState.Down))
                    movement.X += 120;

                if (World.Game.Input.GetInput(Inputs.Left, InputState.Down))
                    movement.X -= 120;

                if (World.Game.Input.GetInput(Inputs.Up, InputState.Down))
                    movement.Y = -100;
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

			// Generate chunks in range (for testing purposes, should be reaplced with another system)
			World.Terrain.GenerateChunk(((int)body.Position.X + 128) / Terrain.CHUNK_WIDTH); 
			World.Terrain.GenerateChunk(Helper.NegDivision((int)body.Position.X - 128, Terrain.CHUNK_WIDTH));

			body.Move(movement);
		}
	}
}

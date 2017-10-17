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

			if (Input.KBState.IsKeyDown(Keys.D))
				movement.X += 120;

			if (Input.KBState.IsKeyDown(Keys.A))
				movement.X -= 120;

			if (Input.KeyPressed(Keys.W))
				movement.Y = -100;

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

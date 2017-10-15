using System;
using Microsoft.Xna.Framework;
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

		public override void Destroy()
		{
			return;
		}

		private BodyComponent body;
		public override void Initialize()
		{
			body = Parent.GetComponent<BodyComponent>();
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

			body.Move(movement);
		}

		protected override void OnEntityAttach(Entity entity)
		{
			return;
		}

		protected override void ReadFromJson(JObject obj)
		{
			return;
		}

		protected override void WriteToJson(JsonWriter writer)
		{
			return;
		}
	}
}

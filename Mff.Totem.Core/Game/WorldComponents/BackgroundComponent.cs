using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	[Serializable("background_wcomponent")]
	public class BackgroundComponent : WorldComponent, IUpdatable, IDrawable
	{
		public Background Background;

		public BackgroundComponent()
		{
			Background = new Backgrounds.OutsideBG().Attach(this);
		}

		public override void Deserialize(BinaryReader reader)
		{
			
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (World.DrawLayer != -1)
				return;

			if (Background != null)
			{
				Background.Draw(spriteBatch);
			}
		}

		public override void Initialize()
		{
			
		}

		public override void Serialize(BinaryWriter writer)
		{
			
		}

		public override void SetActiveArea(Vector2 pos)
		{
		}

		public void Update(GameTime gameTime)
		{
			if (Background != null)
				Background.Update(gameTime);
		}
	}
}

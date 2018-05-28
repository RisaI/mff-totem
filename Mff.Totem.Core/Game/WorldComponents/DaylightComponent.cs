using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public class DaylightComponent : WorldComponent, IUpdatable
	{
		const float SUN_RADIUS = 10000000,
					SUN_SPHERE = 100000;
		public DaylightComponent()
		{
		}


		public override void Serialize(BinaryWriter writer)
		{
			return;
		}

		public override void Deserialize(BinaryReader reader)
		{
			return;
		}

		//Penumbra.Light Sun;
		public override void Initialize()
		{
			/*Sun = new Penumbra.PointLight();
			Sun.Radius = SUN_RADIUS;
			Sun.Scale = new Vector2(256);
			Sun.Color = Color.White;
			Sun.Intensity = 1.5f;
			Sun.ShadowType = Penumbra.ShadowType.Solid;
			World.Lighting.Lights.Add(Sun);*/
		}

		public override void SetActiveArea(Vector2 pos)
		{
			
		}

		public void Update(GameTime gameTime)
		{
			World.Lighting.AmbientColor = Color.Lerp(Color.White, Color.Black, World.NightTint(World.Session.UniverseTime.TimeOfDay.TotalHours));
		}
	}
}

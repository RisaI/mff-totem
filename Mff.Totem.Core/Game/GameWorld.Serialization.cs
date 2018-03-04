using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	public partial class GameWorld
	{
		public void Serialize(BinaryWriter writer)
		{
			writer.Write(Camera);
			writer.Write(CameraControls);

			// Planet info
			writer.Write(Planet.Seed);

			// Terrain
			this.Terrain.Serialize(writer);

			//TODO: weather

			// Entities
			writer.Write((Int16)(Entities.Count(e => e.ShouldSave) + EntityQueue.Count(e => e.ShouldSave)));
			Entities.ForEach(e => { if (e.ShouldSave) { e.Serialize(writer); } });
			EntityQueue.ForEach(e => { if (e.ShouldSave) { e.Serialize(writer); } });

			// Time
			writer.Write(TimeScale);
		}

		public void Deserialize(BinaryReader reader)
		{
			Camera.Deserialize(reader);
			CameraControls = reader.ReadBoolean();

			// Planet info
			var p = new PlanetInfo();
			p.Randomize(reader.ReadInt32());
			Planet = p;

			// Terrain
			Terrain = new Terrain(this);
			Terrain.Deserialize(reader);

			// Entities
			var count = reader.ReadInt16();
			for (int i = 0; i < count; ++i)
			{
				var ent = CreateEntity();
				ent.Deserialize(reader);
			}

			// Time
			TimeScale = reader.ReadSingle();
		}

		public static GameWorld CreatePlanet(GameSession session, int planetId)
		{
			var w = new GameWorld(session);

			// Planet info
			var _info = new PlanetInfo();
			_info.Randomize(planetId);
			_info.GenerateTextures(w);
			w.Planet = _info;

			w._camera.Position.Y = w.Terrain.HeightMap(0);

			// Make the world less empty
			{
				var player = w.CreateEntity("player");
				player.GetComponent<BodyComponent>().LegPosition = new Vector2(0, w.Terrain.HeightMap(0));
				player.GetComponent<InventoryComponent>().AddItem(Item.Create("test_axe"));
				player.GetComponent<InventoryComponent>().AddItem(Item.Create("test_bow"));
			}
			//CameraControls = true;

			return w;
		}

		public static GameWorld LoadWorld(GameSession session, BinaryReader reader)
		{
			var w = new GameWorld(session);
			w.Deserialize(reader);
			return w;
		}
	}
}

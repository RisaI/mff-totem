using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

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
			writer.Write(Builder);

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
			Builder = new WorldBuilder(this);
			Builder.Deserialize(reader);

			// Entities
			var count = reader.ReadInt16();
			for (int i = 0; i < count; ++i)
			{
				var ent = CreateEntity();
				ent.Deserialize(reader);
			}

			// Time
			TimeScale = reader.ReadSingle();

			// Load camera region to prevent entities falling out of the world
			var terrain = Builder.GetComponent<TerrainComponent>();
			if (terrain != null)
				terrain.Multithreaded = false;
			Builder.SetActiveArea(Camera.Position);
			if (terrain != null)
				terrain.Multithreaded = true;
		}

		public static GameWorld CreatePlanet(GameSession session, int planetId)
		{
			var w = new GameWorld(session);

			// Planet info
			var _info = new PlanetInfo();
			_info.Randomize(planetId);
			w.Planet = _info;

			var terrain = w.Builder.GetComponent<TerrainComponent>();
			w._camera.Position.Y = terrain.HeightMap(0);

			// Make the world less empty
			{
				var player = w.CreateEntity("player");
				player.GetComponent<BodyComponent>().LegPosition = new Vector2(0, terrain.HeightMap(0));
				player.GetComponent<InventoryComponent>().AddItem(Item.Create("test_axe"));
				player.GetComponent<InventoryComponent>().AddItem(Item.Create("test_bow"));
				var slime = w.CreateEntity("slime");
				slime.GetComponent<BodyComponent>().LegPosition = new Vector2(600, terrain.HeightMap(600));
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

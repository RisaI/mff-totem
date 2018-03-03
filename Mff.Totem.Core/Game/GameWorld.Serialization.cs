using System;
using System.Diagnostics;
using System.IO;

namespace Mff.Totem.Core
{
	public partial class GameWorld
	{
        public void Serialize(BinaryWriter writer)
		{
			writer.Write(Camera.Position);
			writer.Write(Camera.Rotation);
			writer.Write(Camera.Zoom);

			// Planet info
			writer.Write(Planet.Seed);

			// Terrain
			this.Terrain.Serialize(writer);

			//TODO: weather

			// Entities
			writer.Write((Int16)(Entities.Count + EntityQueue.Count));
			Entities.ForEach(e => e.Serialize(writer));
			EntityQueue.ForEach(e => e.Serialize(writer));

			// Time
			writer.Write(TimeScale);
			writer.Write(WorldTime.Ticks);
		}

		public void Deserialize(BinaryReader reader)
		{
			Camera.Position = reader.ReadVector2();
			Camera.Rotation = reader.ReadSingle();
			Camera.Zoom = reader.ReadSingle();

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
			WorldTime = new DateTime(reader.ReadInt64());
		}

		public static GameWorld CreateTestWorld(TotemGame game)
		{
			var w = new GameWorld(game);

			// Planet info
			var _info = new PlanetInfo();
			_info.Randomize(TotemGame.Random.Next());
			_info.GenerateTextures(w);
			w.Planet = _info;

			w._camera.Position.Y = w.Terrain.HeightMap(0);

			return w;
		}

		public static GameWorld LoadFromStream(TotemGame game, Stream s)
		{
			var w = new GameWorld(game);
			using (BinaryReader reader = new BinaryReader(s))
			{
				w.Deserialize(reader);
			}
			return w;
		}
	}
}

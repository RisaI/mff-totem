using System;
using System.IO;

namespace Mff.Totem.Core
{
	public class GameSession : ISerializable
	{
		public DateTime UniverseTime;

		public GameSession()
		{
			UniverseTime = new DateTime(2034, 5, 27, 12, 0, 0);
		}

		public void Deserialize(BinaryReader reader)
		{
			UniverseTime = new DateTime(reader.ReadInt64());
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(UniverseTime.Ticks);
		}
	}
}

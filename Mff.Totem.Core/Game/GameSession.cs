﻿using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public class GameSession : ISerializable, IUpdatable, IDrawable
	{
		public TotemGame Game
		{
			get;
			private set;
		}

		public DateTime UniverseTime;

		public GameWorld CurrentPlanet
		{
			get;
			private set;
		}

		private GameWorld _instance;
		public GameWorld CurrentInstance
		{
			get { return _instance; }
			private set
			{
				_instance = value;
			}
		}

		GameSession(TotemGame game)
		{
			Game = game;
			UniverseTime = new DateTime(2034, 5, 27, 12, 0, 0);
		}

		/// <summary>
		/// Sets the planet.
		/// </summary>
		/// <param name="planetId">Planet identifier.</param>
		public void SetPlanet(int planetId)
		{
			CurrentInstance = CurrentPlanet = GameWorld.CreatePlanet(this, planetId);
		}

		//TODO: SetDungeon

		public void Deserialize(BinaryReader reader)
		{
			UniverseTime = new DateTime(reader.ReadInt64());
			if (reader.ReadBoolean())
				CurrentPlanet = GameWorld.LoadWorld(this, reader);
			var a = reader.ReadByte();
			switch (a)
			{
				case 1:
					CurrentInstance = GameWorld.LoadWorld(this, reader);
					break;
				case 2:
					CurrentInstance = CurrentPlanet;
					break;
			}
		}

		public void Serialize(BinaryWriter writer)
		{
			writer.Write(UniverseTime.Ticks);

			// Serialize the current planet
			writer.Write(CurrentPlanet != null);
			if (CurrentPlanet != null)
				CurrentPlanet.Serialize(writer);

			// Determine saving procedure for current instance
			byte a = (byte)(CurrentInstance != null ? (CurrentInstance != CurrentPlanet ? 1 : 2) : 0);
			writer.Write(a);
			if (a == 1)
				CurrentInstance.Serialize(writer);
		}

		/// <summary>
		/// Update this game session.
		/// </summary>
		/// <param name="gameTime">Game time.</param>
		public void Update(GameTime gameTime)
		{
			if (CurrentInstance != null)
			{
				UniverseTime = UniverseTime.AddMinutes(gameTime.ElapsedGameTime.TotalSeconds * CurrentInstance.TimeScale);
				CurrentInstance.Update(gameTime);
			}
		}

		/// <summary>
		/// Draws this game session.
		/// </summary>
		/// <param name="spriteBatch">Sprite batch.</param>
		public void Draw(SpriteBatch spriteBatch)
		{
			if (CurrentInstance != null)
				CurrentInstance.Draw(spriteBatch);
		}

		/// <summary>
		/// Create new game.
		/// </summary>
		/// <returns>The new game.</returns>
		/// <param name="game">Game.</param>
		public static GameSession CreateNewGame(TotemGame game)
		{
			var s = new GameSession(game);
			s.SetPlanet(TotemGame.Random.Next());
			return s;
		}

		/// <summary>
		/// Loads game session from a file.
		/// </summary>
		/// <returns>A game session.</returns>
		/// <param name="game">TotemGame.</param>
		/// <param name="stream">An IO stream.</param>
		public static GameSession LoadGame(TotemGame game, Stream stream)
		{
			var s = new GameSession(game);
			using (BinaryReader reader = new BinaryReader(stream))
			{
				s.Deserialize(reader);
			}
			return s;
		}
	}
}

using System;
namespace Mff.Totem.Core
{
	[Serializable("component_player")]
	public class PlayerComponent : CharacterComponent
	{
		public int TechnologyLevel = 0, MagicLevel = 0;


		int _techExp = 0;
		public int TechExperience
		{
			get { return _techExp; }
			set {
				_techExp = value;
				while (_techExp >= TechExpCap)
				{
					_techExp -= TechExpCap;
					++TechnologyLevel;
					World.Game.Hud.Chat("Your technology level is now " + TechnologyLevel);
				}
			}
		}

		public int TechExpCap
		{
			get { return TechnologyLevel * 64 + (int)Math.Pow(2, TechnologyLevel); }
		}

		int _magExp = 0;
		public int MagicExperience
		{
			get { return _magExp; }
			set
			{
				_magExp = value;
				while (_magExp >= MagicExpCap)
				{
					_magExp -= MagicExpCap;
					++MagicLevel;
					World.Game.Hud.Chat("Your magic level is now " + TechnologyLevel);
				}
			}
		}

		public int MagicExpCap
		{
			get { return TechnologyLevel * 64 + (int)Math.Pow(2, TechnologyLevel); }
		}


		protected override void OnSerialize(System.IO.BinaryWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(TechnologyLevel);
			writer.Write(MagicLevel);

			writer.Write(_techExp);
			writer.Write(_magExp);
		}

		protected override void OnDeserialize(System.IO.BinaryReader reader)
		{
			base.OnDeserialize(reader);
			TechnologyLevel = reader.ReadInt32();
			MagicLevel = reader.ReadInt32();

			_techExp = reader.ReadInt32();
			_magExp = reader.ReadInt32();
		}
	}
}

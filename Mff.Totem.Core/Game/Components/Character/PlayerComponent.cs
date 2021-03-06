using System;
using System.Collections.Generic;

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
				if (_techExp < value)
					World.Game.Hud.Chat("Added " + (value - _techExp) + " experience to TECH.");
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


		public override void Serialize(System.IO.BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write(TechnologyLevel);
			writer.Write(MagicLevel);

			writer.Write(_techExp);
			writer.Write(_magExp);
		}

		public override void Deserialize(System.IO.BinaryReader reader)
		{
			base.Deserialize(reader);
			TechnologyLevel = reader.ReadInt32();
			MagicLevel = reader.ReadInt32();

			_techExp = reader.ReadInt32();
			_magExp = reader.ReadInt32();
		}

		public override EntityComponent Clone()
		{

			List<string> tags = new List<string>();
			TargetedTags.ForEach(t => tags.Add(t));
			return new PlayerComponent()
			{
				_hp = _hp,
				_stamina = _stamina,
				_baseMaxHp = _baseMaxHp,
				_baseMaxStamina = _baseMaxStamina,
				_baseSpeed = _baseSpeed,
				_expReward = _expReward,
				TargetedTags = tags,
				TechnologyLevel = TechnologyLevel,
				_techExp = _techExp,
				MagicLevel = MagicLevel,
				_magExp = _magExp,
			};
		}
	}
}

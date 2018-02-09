using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Mff.Totem.Core;
using Mff.Totem.Ai;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mff.Totem.Core
{
	[Serializable("ai_component")]
	public class AiComponent : EntityComponent, IUpdatable
	{
		public List<AiElement> Elements;

		public AiComponent() : this(new List<AiElement>())
		{
			
		}

		public AiComponent(List<AiElement> elems)
		{
			Elements = elems;
		}

		public AiComponent(params AiElement[] elems) : this(elems.ToList())
		{
			
		}

		public override EntityComponent Clone()
		{
			List<AiElement> c = new List<AiElement>();
			Elements.ForEach(e => c.Add(e.Clone()));
			return new AiComponent(c);
		}

		protected override void WriteToJson(JsonWriter writer)
		{
			
		}

		public void Update(GameTime gameTime)
		{
			for (int i = 0; i < Elements.Count; ++i)
			{
				if (Elements[i].Stimulated(Parent))
				{
					Elements[i].Update(Parent, gameTime);
					break;
				}
			}
		}
	}
}

namespace Mff.Totem.Ai
{
	public abstract class AiElement : ICloneable<AiElement>, IJsonSerializable, ISerializable
	{
		public abstract AiElement Clone();

		public abstract void Serialize(BinaryWriter writer);
		public abstract void Deserialize(BinaryReader reader);

		public abstract void FromJson(JObject obj);
		public abstract void ToJson(JsonWriter writer);

		public abstract bool Stimulated(Entity ent);
		public abstract void Update(Entity ent, GameTime gameTime);
	}

}

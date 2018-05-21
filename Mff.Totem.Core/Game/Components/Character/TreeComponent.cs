using System;
using Microsoft.Xna.Framework;

namespace Mff.Totem.Core
{
	[Serializable("component_tree")]
	public class TreeComponent : DamagableComponent, IUpdatable
	{
		Rectangle leafArea;
		float _shake;
		bool _falling;

		public Rectangle LeafArea
		{
			get
			{
				var pos = Parent.Position.HasValue ? Parent.Position.Value : Vector2.Zero;
				return new Rectangle((int)pos.X + leafArea.X, (int)pos.Y + leafArea.Y, leafArea.Width, leafArea.Height);
			}
		}

		public void Update(GameTime gameTime)
		{
			var body = Parent.GetComponent<BodyComponent>();
			if (body != null)
			{
				if (_falling)
				{
					body.Rotation += (float)gameTime.ElapsedGameTime.TotalSeconds * Parent.World.TimeScale * MathHelper.PiOver2;
					if (body.Rotation > MathHelper.PiOver2)
					{
						Parent.Remove = true;
					}
				}
				else if (_shake > 0)
				{
					_shake -= (float)gameTime.ElapsedGameTime.TotalSeconds * Parent.World.TimeScale;
					body.Rotation = (float)(Math.Sin(_shake * 8 * MathHelper.Pi) * MathHelper.PiOver4 / 8);
					if (TotemGame.Random.NextDouble() < 0.25)
					{
						World.SpawnParticle("leaf", LeafArea.RandomPoint());
					}
				}
				else
				{
					body.Rotation = 0;
				}
			}
		}

		protected override void Death(object source)
		{
			base.Death(source);
			_falling = true;

			var ent = World.CreateEntity("itembag");
			ent.GetComponent<BodyComponent>().Position = Parent.Position.Value - new Vector2(0, 64);
			ent.GetComponent<ItemComponent>().AddItem(Item.Create("wood", 10));
		}

		public override EntityComponent Clone()
		{
			return new TreeComponent() { _baseMaxHp = _baseMaxHp, _hp = _hp, leafArea = leafArea };
		}

		public override void Damage(object source, int damage)
		{
			_shake = 0.25f;
			base.Damage(source, damage);
		}

		protected override void ReadFromJson(Newtonsoft.Json.Linq.JObject obj)
		{
			base.ReadFromJson(obj);
			if (obj["leaf_area"] != null)
				leafArea = Helper.JTokenToRectangle(obj["leaf_area"]);
		}

		public override void Serialize(System.IO.BinaryWriter writer)
		{
			base.Serialize(writer);
			writer.Write(leafArea);
		}

		public override void Deserialize(System.IO.BinaryReader reader)
		{
			base.Deserialize(reader);
			leafArea = reader.ReadRectangle();
		}

		// Makes sure trees are not serialized when saving the GameWorld as they are managed by the terrain system
		public override bool DisableEntitySaving
		{
			get
			{
				return true;
			}
		}
	}
}

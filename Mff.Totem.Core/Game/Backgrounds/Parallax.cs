using System;
using Microsoft.Xna.Framework.Graphics;

namespace Mff.Totem.Core
{
	public class Parallax
	{
		public Texture2D[] Textures
		{
			get;
			private set;
		}

		public bool[] Offsetable
		{
			get;
			private set;
		}

		public Parallax(int layers)
		{
			Textures = new Texture2D[layers];
			Offsetable = new bool[layers];
		}
	}
}

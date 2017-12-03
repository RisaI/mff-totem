using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Mff.Totem.Core
{
	/// <summary>
	/// Holds methods for input handling.
	/// </summary>
	public static class Input
	{
		public static MouseState MState, PrevMState;
		public static KeyboardState KBState, PrevKBState;

		/// <summary>
		/// Refreshes input states, should be called once every update.
		/// </summary>
		public static void Update()
		{
			PrevMState = MState;
			MState = Mouse.GetState();

			PrevKBState = KBState;
			KBState = Keyboard.GetState();
		}

		/// <summary>
		/// Was a key pressed this update?
		/// </summary>
		/// <param name="key">Key.</param>
		public static bool KeyPressed(Keys key)
		{
			return KBState.IsKeyDown(key) && PrevKBState.IsKeyUp(key);
		}

		/// <summary>
		/// Was a key released this update?
		/// </summary>
		/// <param name="key">Key.</param>
		public static bool KeyReleased(Keys key)
		{
			return PrevKBState.IsKeyDown(key) && KBState.IsKeyUp(key);
		}

		public static bool LMBPressed
		{
			get { return PrevMState.LeftButton == ButtonState.Released && MState.LeftButton == ButtonState.Pressed; }
		}

		/// <summary>
		/// Returns the mouse position as a vector.
		/// </summary>
		/// <value>The mouse position.</value>
		public static Vector2 MousePosition
		{
			get { return new Vector2(MState.X, MState.Y); }
		}
	}
}

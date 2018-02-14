using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Mff.Totem.Core
{
	public abstract class Input
	{
		public TotemGame Game
		{
			get;
			private set;
		}

		public Input(TotemGame game)
		{
			Game = game;
		}

		public abstract void Update(GameTime gameTime);
		public abstract bool GetInput(Inputs i, InputState state);
		public abstract bool InputInsideRectangle(Rectangle rect, InputState state);
		public abstract bool InputOutsideRectangle(Rectangle rect, InputState state);

		public abstract PointerInput GetPointerInput(byte id);
		public abstract List<PointerInput> GetPointerInputs();
		public abstract List<PointerInput> GetPointerInputsOutsideGui();

		public event Action<char> OnTextInput;
		protected void RegisterTextEvent(char ch)
		{
			OnTextInput?.Invoke(ch);
		}
	}

	public enum Inputs
	{
		Up,
		Down,
		Left,
		Right,
		A,
		B,
		Use,
		Plus,
		Minus,
		Sprint,
		Pause,
		Inventory
	}

	public enum InputState
	{
		Pressed,
		Down,
		Up,
		Released
	}

	public struct PointerInput
	{
		public Vector2 Position;
		public byte ID;
		public InputState State;
	}
}

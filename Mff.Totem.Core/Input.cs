using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Mff.Totem.Gui;
using Mff.Totem.Core;

namespace Mff.Totem
{
	public class Input
	{
		public TotemGame Game
		{
			get;
			private set;
		}

		public Input(TotemGame game)
		{
			Game = game;
			//game.Window.TextInput += (sender, e) => { RegisterTextEvent(e.Character); };
		}

		public bool inGui;
		public PointerInput LMB, RMB, MMB;
		public KeyboardState KBState, PrevKBState;
		public MouseState MState, PrevMState;
		public void Update(GameTime gameTime)
		{
			PrevKBState = KBState;
			KBState = Keyboard.GetState();
			PrevMState = MState;
			MState = Mouse.GetState();

			LMB = new PointerInput() { ID = 0, Position = MousePosition, State = LMBState };
			RMB = new PointerInput() { ID = 1, Position = MousePosition, State = RMBState };
			MMB = new PointerInput() { ID = 2, Position = MousePosition, State = MMBState };

			Gui.Gui g;
			if ((g = Game.GuiManager.GuiAt(MousePosition)) != null)
			{
				inGui = true;
				g.Input(LMB);
				g.Input(RMB);
				g.Input(MMB);
			}
		}

		public PointerInput GetPointerInput(byte id)
		{
			switch (id)
			{
				case 0:
					return LMB;
				case 1:
					return RMB;
				case 2:
					return MMB;
				default:
					return new PointerInput() { State = InputState.Up };
			}
		}

		public List<PointerInput> GetPointerInputs()
		{
			return new List<PointerInput>() { LMB, RMB, MMB };
		}

		public List<PointerInput> GetPointerInputsOutsideGui()
		{
			return inGui ? new List<PointerInput>() { } : GetPointerInputs();
		}

		public bool GetInput(Inputs i, InputState state)
		{
			switch (i)
			{
				case Inputs.Pause:
					return GetKeyState(Keys.Escape) == state;
				case Inputs.Left:
					return GetKeyState(Keys.A) == state;
				case Inputs.Right:
					return GetKeyState(Keys.D) == state;
				case Inputs.Up:
					return GetKeyState(Keys.W) == state;
				case Inputs.Down:
					return GetKeyState(Keys.S) == state;
				case Inputs.Plus:
					return GetKeyState(Keys.OemPlus) == state;
				case Inputs.Minus:
					return GetKeyState(Keys.OemMinus) == state;
				case Inputs.Sprint:
					return GetKeyState(Keys.LeftShift) == state;
				case Inputs.A:
					return GetPointerInput(0).State == state;
				case Inputs.Use:
					return GetKeyState(Keys.E) == state;
				case Inputs.Swap:
					return GetKeyState(Keys.Q) == state;
				case Inputs.Inventory:
					return GetKeyState(Keys.Tab) == state;
				case Inputs.QuickSave:
					return GetKeyState(Keys.F9) == state;
				case Inputs.QuickLoad:
					return GetKeyState(Keys.F5) == state;
			}
			return false;
		}

		public bool InputInsideRectangle(Rectangle rect, InputState state)
		{
			if (rect.Contains(MousePosition.ToPoint()))
			{
				return LMBState == state || RMBState == state || MMBState == state;
			}
			return false;
		}

		public bool InputOutsideRectangle(Rectangle rect, InputState state)
		{
			if (!rect.Contains(MousePosition.ToPoint()))
			{
				return LMBState == state || RMBState == state || MMBState == state;
			}
			return false;
		}

		public InputState LMBState
		{
			get
			{
				return MState.LeftButton == ButtonState.Pressed && PrevMState.LeftButton == ButtonState.Released ? InputState.Pressed :
					(MState.LeftButton == ButtonState.Released && PrevMState.LeftButton == ButtonState.Pressed ? InputState.Released :
					(MState.LeftButton == ButtonState.Pressed ? InputState.Down : InputState.Up));
			}
		}
		public InputState RMBState
		{
			get
			{
				return MState.RightButton == ButtonState.Pressed && PrevMState.RightButton == ButtonState.Released ? InputState.Pressed :
				  (MState.RightButton == ButtonState.Released && PrevMState.RightButton == ButtonState.Pressed ? InputState.Released :
				  (MState.RightButton == ButtonState.Pressed ? InputState.Down : InputState.Up));
			}
		}
		public InputState MMBState
		{
			get
			{
				return MState.MiddleButton == ButtonState.Pressed && PrevMState.MiddleButton == ButtonState.Released ? InputState.Pressed :
				  (MState.MiddleButton == ButtonState.Released && PrevMState.MiddleButton == ButtonState.Pressed ? InputState.Released :
				  (MState.MiddleButton == ButtonState.Pressed ? InputState.Down : InputState.Up));
			}
		}


		public InputState GetKeyState(Keys key)
		{
			if (KBState.IsKeyDown(key))
				return PrevKBState.IsKeyUp(key) ? InputState.Pressed : InputState.Down;
			else
				return PrevKBState.IsKeyDown(key) ? InputState.Released : InputState.Up;
		}

		public Vector2 MousePosition
		{
			get { return new Vector2(MState.X, MState.Y); }
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
		Swap,
		Pause,
		Inventory,
		QuickSave,
		QuickLoad
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

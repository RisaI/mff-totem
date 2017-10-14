using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using System.Text.RegularExpressions;

namespace Mff.Totem.Core
{
	public class DeveloperConsole
	{
		/// <summary>
		/// Console constants.
		/// </summary>
		public const int HEIGHT = 328;
		const int SCROLL = 6;
		const float CURSOR_TIME = 0.5f, INPUT_TIME_F = 1f, INPUT_TIME = 0.05f;

		/// <summary>
		/// Parent TotemGame reference
		/// </summary>
		/// <value>A TotemGame instance.</value>
		public TotemGame Game
		{
			get;
			private set;
		}

		public SpriteFont Font
		{
			get { return ContentLoader.Fonts["console"]; }
		}

		private int _off;

		/// <summary>
		/// Offset from the bottom of the text
		/// </summary>
		/// <value>Offset.</value>
		public int Offset
		{
			get { return _off; }
			set
			{
				_off = MathHelper.Clamp(value, 0, Math.Max(0, Output.Count - 2));
			}
		}

		/// <summary>
		/// Stores the last console input.
		/// </summary>
		/// <value>The last input.</value>
		public string PreviousInput
		{
			get;
			private set;
		}

		private string _input;
		/// <summary>
		/// Currently typed input.
		/// </summary>
		/// <value>Input</value>
		public string Input
		{
			get { return _input; }
			private set
			{
				_input = value;
				if (value != null)
					_cursorIndex = Math.Min(value.Length, _cursorIndex);
				else
					_cursorIndex = 0;
			}
		}

		/// <summary>
		/// Is console visible and enabled?
		/// </summary>
		public bool Enabled = false;

		protected Dictionary<string, ConsoleCommand> Commands = new Dictionary<string, ConsoleCommand>();

		/// <summary>
		/// Primary constructor.
		/// </summary>
		/// <param name="game">Game.</param>
		public DeveloperConsole(TotemGame game)
		{
			Game = game;
			Game.Window.TextInput += (sender, args) =>
			{
				if (Enabled && args.Character != '`' && args.Character != '~' && Font.Characters.Contains(args.Character))
				{
					if (!string.IsNullOrEmpty(Input))
						Input = Input.Substring(0, Input.Length - _cursorIndex) + args.Character + Input.Substring(Input.Length - _cursorIndex);
					else
						Input = string.Empty + args.Character;

					RefreshCursor();
				}
			};
			Console.SetOut(new ConsoleWriter(this));

			AddCommand("help", "Shows the help menu.", args => // Help command
			{
				if (args.Length == 0)
				{
					WriteLine(TotemGame.ProjectName + ", ver: " + TotemGame.Version);
					Commands.ToList().ForEach(pair =>
					{
						Console.WriteLine("{0} - {1} {2}", pair.Key, pair.Value.Description,
										  pair.Value.ArgumentString != null ? "(args: " + pair.Value.ArgumentString + ")" : string.Empty);
					});
				}
				else
				{
					if (Commands.ContainsKey(args[0]))
					{
						var command = Commands[args[0]];
						Console.WriteLine("{0} - {1} {2}", args[0], command.Description,
										  command.ArgumentString != null ? "(args: " + command.ArgumentString + ")" : string.Empty);
					}
					else
					{
						Console.WriteLine("Command '{0}' does not exist.", args[0]);
					}
				}
			}, "[command]");
			AddCommand("clear", "Clears the console output.", args =>
			{
				Output.Clear();
				Output.Add(string.Empty);
			});
			AddCommand("echo", "Prints every argument to the console.", args => // Echo command
			{
				for (int i = 0; i < args.Length; ++i)
					WriteLine(args[i]);
			}, "strings");
			AddCommand("exit", "Quits the game.", args => // Exit command
			{
				Game.Exit();
			});
		}

		/// <summary>
		/// Adds a command to the console.
		/// </summary>
		/// <param name="command">Name.</param>
		/// <param name="description">Description.</param>
		/// <param name="action">Action</param>
		/// <param name="arguments">Argument format</param>
		public void AddCommand(string command, string description, Action<string[]> action, params string[] arguments)
		{
			Commands.Add(command, new ConsoleCommand(description, action, arguments));
		}

		/// <summary>
		/// Updates the console to process input.
		/// </summary>
		/// <param name="gameTime">Game time.</param>
		public void Update(GameTime gameTime)
		{
			_cursorTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (_cursorTimer >= CURSOR_TIME)
			{
				_cursorTimer %= CURSOR_TIME;
				_cursorVisible = !_cursorVisible;
			}

			if (_inputTimer > 0)
			{
				if (Core.Input.KBState.IsKeyDown(Keys.Back) ||
					Core.Input.KBState.IsKeyDown(Keys.Delete) ||
					Core.Input.KBState.IsKeyDown(Keys.Left) ||
					Core.Input.KBState.IsKeyDown(Keys.Right))
					_inputTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
				else
					_inputTimer = 0;
			}

			// Predchozi vstup
			if (Core.Input.KeyPressed(Keys.Up) && !string.IsNullOrWhiteSpace(PreviousInput))
			{
				Input = PreviousInput; // Restores the last input
			}
			if (Input != null)
			{
				if (Core.Input.KeyPressed(Keys.Enter)) // Should the input be executed?
				{
					Input = Input.Trim();

					if (!string.IsNullOrWhiteSpace(Input))
					{
						string command = Input.Split(' ')[0],
						remainder = Input.Substring(command.Length).Trim();
						Regex reg = new Regex(@"(\""[^\""]*\"")|(\b\S+\b)");
						var matches = reg.Matches(remainder);
						string[] args = new string[matches.Count];
						for (int i = 0; i < matches.Count; ++i)
						{
							args[i] = matches[i].Value.Replace("\"", string.Empty); // Parse arguments
						}
						Command(command, args); // Execute a command
						PreviousInput = Input;
					}

					Input = null;
				}
				else
				{
					// Backspace and delete
					if (Core.Input.KBState.IsKeyDown(Keys.Back) && Input.Length - _cursorIndex > 0 && _inputTimer <= 0)
					{
						_inputTimer += Mff.Totem.Core.Input.KeyPressed(Keys.Back) ? INPUT_TIME_F : INPUT_TIME;
						if (_cursorIndex == 0)
							Input = Input.Substring(0, Input.Length - 1); // Remove a character
						else
							Input = Input.Substring(0, Input.Length - 1 - _cursorIndex) + Input.Substring(Input.Length - _cursorIndex);

						RefreshCursor();
					}
					else if (Core.Input.KBState.IsKeyDown(Keys.Delete) && _cursorIndex > 0 && _inputTimer <= 0)
					{
						_inputTimer += Core.Input.KeyPressed(Keys.Delete) ? INPUT_TIME_F : INPUT_TIME;
						Input = Input.Substring(0, Input.Length - _cursorIndex) + Input.Substring(Input.Length - _cursorIndex + 1);
						if (Input.Length != _cursorIndex)
							--_cursorIndex;
						RefreshCursor();
					}

					// Cursor navigation
					if (Core.Input.KBState.IsKeyDown(Keys.Left) && _inputTimer <= 0)
					{
						_inputTimer += Core.Input.KeyPressed(Keys.Left) ? INPUT_TIME_F : INPUT_TIME;
						_cursorIndex = MathHelper.Min(Input.Length, _cursorIndex + 1);
						RefreshCursor();
					}
					if (Core.Input.KBState.IsKeyDown(Keys.Right) && _inputTimer <= 0)
					{
						_inputTimer += Core.Input.KeyPressed(Keys.Right) ? INPUT_TIME_F : INPUT_TIME;
						_cursorIndex = MathHelper.Max(0, _cursorIndex - 1);
						RefreshCursor();
					}
				}

				if (Core.Input.KeyPressed(Keys.End))
				{
					_cursorIndex = 0; // Go to end
				}
				else if (Core.Input.KeyPressed(Keys.Home))
				{
					_cursorIndex = Input.Length; // Go to the beggining
				}

				if (Core.Input.KeyPressed(Keys.Escape))
				{
					Input = null; // Clear input
				}

			}

			// Scrolling
			if (Core.Input.KeyPressed(Keys.PageUp))
			{
				Offset += SCROLL; // Scroll up
			}
			else if (Core.Input.KeyPressed(Keys.PageDown))
			{
				Offset -= SCROLL; // Scroll down
			}

			// Autocomplete
			if (Core.Input.KeyPressed(Keys.Tab) && !string.IsNullOrEmpty(Input))
			{
				Input = Input.TrimStart();
				if (!string.IsNullOrEmpty(Input))
				{
					var commands = Commands.Keys.ToList().FindAll(key => key.StartsWith(Input.ToLower(), StringComparison.InvariantCulture));
					if (commands.Count == 1)
						Input = commands[0] + " ";
					else if (commands.Count > 1)
					{
						string common = Input.ToLower();
						int shortest = int.MaxValue;
						commands.ForEach(c => { if (c.Length < shortest) { shortest = c.Length; } });
						for (int iCh = common.Length; iCh < shortest; ++iCh)
						{
							bool contains = true;
							for (int i = 1; i < commands.Count; ++i)
							{
								if (commands[i][iCh] != commands[0][iCh])
								{
									contains = false;
									break;
								}
							}
							if (contains)
								common += commands[0][iCh];
							else
								break;
						}
						if (common.Length > Input.Length)
							Input = common;
						else
						{
							string output = string.Empty;
							commands.ForEach(c => { output += c + ", "; });
							output = output.Substring(0, output.Length - 2);
							if (Output.Count <= 1 || Output[Output.Count - 2] != output)
								WriteLine(output);
						}
					}
				}
			}

			if (Core.Input.KeyPressed(Keys.OemTilde))
			{
				Enabled = !Enabled;
			}
		}

		private bool _cursorVisible = true;
		private float _cursorTimer = 0, _inputTimer = 0;
		private int _cursorIndex = 0;

		public void Draw(SpriteBatch spriteBatch)
		{
			int cWidth = (int)Game.Resolution.X;
			spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, cWidth, HEIGHT);
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, new RasterizerState() { ScissorTestEnable = true });
			spriteBatch.DrawRectangle(new Rectangle(0, 0, cWidth, HEIGHT), new Color(0, 0, 0, 200), 0f);

			if (Output.Count > 1 || !string.IsNullOrEmpty(Output[0]))
			{
				int lastModifier = string.IsNullOrWhiteSpace(Output[Output.Count - 1]) ? 1 : 0;
				int drawRange = MathHelper.Min(HEIGHT / Font.LineSpacing + 1, Output.Count - lastModifier);
				int currentOffset = 0;
				for (int i = Offset + lastModifier; i < Math.Min(Output.Count, drawRange + Offset + lastModifier); ++i)
				{
					var line = Output[Output.Count - 1 - i];
					if (!string.IsNullOrWhiteSpace(line))
					{
						currentOffset += ((int)Font.MeasureString(line).X - 1) / cWidth + 1;
						int localCounter = 1, standardOffset = (Offset % HEIGHT);
						for (int s = 0; s < line.Length; ++s)
						{
							if (Font.MeasureString(line.Substring(0, s)).X > cWidth)
							{
								spriteBatch.DrawString(Font, line.Substring(0, s - 1), new Vector2(0, HEIGHT - (currentOffset - localCounter) * Font.LineSpacing), Color.White,
													   0, new Vector2(0, Font.LineSpacing), 1, SpriteEffects.None, 1f);
								++localCounter;
								line = line.Substring(s - 1);
								s = 1;
							}
							else if (s == line.Length - 1)
							{
								spriteBatch.DrawString(Font, line, new Vector2(0, HEIGHT - (currentOffset - localCounter) * Font.LineSpacing), Color.White,
													   0, new Vector2(0, Font.LineSpacing), 1, SpriteEffects.None, 1f);
								break;
							}
						}
					}
					else
					{
						++currentOffset;
					}
				}

			}
			spriteBatch.End();

			spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle(0, HEIGHT, cWidth, Font.LineSpacing);
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, new RasterizerState() { ScissorTestEnable = true });
			spriteBatch.DrawRectangle(new Rectangle(0, HEIGHT, cWidth, Font.LineSpacing), new Color(0, 0, 0, 225), 0f);
			{
				if (!string.IsNullOrEmpty(Input))
					spriteBatch.DrawString(Font, Input, new Vector2(0, HEIGHT), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
				if (_cursorVisible)
				{
					float x = string.IsNullOrEmpty(Input) ? 0 : Font.MeasureString(Input.Substring(0, Input.Length - _cursorIndex)).X;
					spriteBatch.Draw(ContentLoader.Pixel, new Vector2(x, HEIGHT), null, Color.White, 0, Vector2.Zero,
									 new Vector2(1, Font.LineSpacing), SpriteEffects.None, 1f);
				}
			}
			spriteBatch.End();
		}

		public void Command(string command, string[] args)
		{
			if (Commands.ContainsKey(command))
			{
				try
				{
					Commands[command].Invoke(args);
				}
				catch (Exception ex)
				{
					Console.WriteLine("An error has occured while executing command. Message: {0}", ex.Message);
				}
			}
			else
				Console.WriteLine("Command '{0}' does not exist.", command);
		}

		List<string> Output = new List<string>(2048) { "Initializing console.", string.Empty };

		public void WriteLine()
		{
			Output.Add(string.Empty);
		}

		public void WriteLine(string line)
		{
			if (line.Contains('\n'))
			{
				Output[Output.Count - 1] += line.Substring(0, line.IndexOf('\n'));
				line = line.Substring(line.IndexOf('\n') + 1);
				foreach (string subline in line.Split('\n'))
				{
					Output.Add(subline);
				}
			}
			else
			{
				Output[Output.Count - 1] += line;
			}
			Output.Add(string.Empty);
		}

		public void Write(char value)
		{
			if (value == '\n')
				Output.Add(string.Empty);
			else
				Output[Output.Count - 1] += value;
		}

		public void RefreshCursor()
		{
			_cursorTimer = 0;
			_cursorVisible = true;
		}

		public class ConsoleCommand
		{
			private Action<string[]> _action;
			private int _minimumArguments = 0;

			public string[] Arguments
			{
				get;
				private set;
			}

			public string ArgumentString
			{
				get;
				private set;
			}

			public string Description
			{
				get;
				private set;
			}

			public ConsoleCommand(string description, Action<string[]> action, string[] arguments)
			{
				Description = description;
				_action = action;
				Arguments = arguments;
				if (Arguments != null && Arguments.Length > 0)
				{
					ArgumentString = string.Empty;
					for (int i = 0; i < arguments.Length; ++i)
					{
						ArgumentString += Arguments[i];
						if (i < Arguments.Length - 1)
							ArgumentString += ", ";

						if (_minimumArguments == 0 && arguments[i].StartsWith("[", StringComparison.InvariantCulture)
							&& arguments[i].EndsWith("]", StringComparison.InvariantCulture))
						{
							_minimumArguments = i;
						}
					}
				}
			}

			public void Invoke(string[] args)
			{
				if (args.Length < _minimumArguments)
					Console.WriteLine("Invalid parameters, check help for this command.");
				else if (_action != null)
					_action.Invoke(args);
			}
		}

		class ConsoleWriter : System.IO.TextWriter
		{
			public DeveloperConsole Console
			{
				get;
				private set;
			}

			public ConsoleWriter(DeveloperConsole console)
			{
				Console = console;
			}

			public override void Write(char value)
			{
				Console.Write(value);
			}

			public override Encoding Encoding
			{
				get
				{
					return Encoding.UTF8;
				}
			}
		}
	}
}
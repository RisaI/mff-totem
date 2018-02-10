using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using System.Text.RegularExpressions;
using ClipperLib;

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

		DesktopInput KInput
		{
			get { return (DesktopInput)Game.Input; }
		}

		/// <summary>
		/// A reference to the console font from ContentLoader for code clarity.
		/// </summary>
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
				_off = (int)MathHelper.Clamp(value, 0, Math.Max(0, Output.Count - 2));
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

			// Test input handling
			TextInputEXT.TextInput += (ch) =>
			{
				if (Enabled && ch != '`' && ch != '~' && Font.Characters.Contains(ch))
				{
					if (!string.IsNullOrEmpty(Input))
						Input = Input.Substring(0, Input.Length - _cursorIndex) + ch + Input.Substring(Input.Length - _cursorIndex); // Insert character at cursor
					else
						Input = string.Empty + ch;

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

			#region Entity commands
			AddCommand("list_ents", "Lists all entities in GameWorld.", args => // List of entities
			{
				int index = 0;
				Game.World.Entities.ForEach(e =>
				{
					string pos = "none";
					var body = e.GetComponent<BodyComponent>();
					if (body != null)
						pos = body.Position.ToString();
					Console.WriteLine("Index: {0}, UID: {1}, Position: {2}", index++, e.UID, pos);
				});
			});
			AddCommand("ent_spawn", "Spawns an entity loaded from assets.", args =>
			{
				Vector2 pos = new Vector2(float.Parse(args[1]), args.Length >= 3 ? float.Parse(args[2]) : Game.World.Terrain.HeightMap(float.Parse(args[1])));
				var ent = Game.World.CreateEntity(args[0]);
				var body = ent.GetComponent<BodyComponent>();
				if (body != null)
					body.LegPosition = pos;
			}, "asset", "x", "[y]");
#endregion
			AddCommand("terrain_test", "Spawns an entity loaded from assets.", args =>
			{
				Game.World.Terrain.CreateDamage(new List<IntPoint>() { new IntPoint(200, 0), new IntPoint(250, 0), new IntPoint(250, 1990), new IntPoint(200, 2000) });
			}, "asset", "x", "y");

			AddCommand("camera_controls", "Set camera controls on/off.", args =>
			{
				Game.World.CameraControls = bool.Parse(args[0]);
			}, "true/false");

			AddCommand("timescale", "Set the timescale.", args =>
			{
				Game.World.TimeScale = float.Parse(args[0]);
			}, "float");

			AddCommand("heightmap", "Get the height for specified x.", args =>
			{
				var x = float.Parse(args[0]);
				Console.WriteLine("Height on {0}: {1}", x, Game.World.Terrain.HeightMap(x));
			}, "x");

			AddCommand("weather", "Set the weather.", args =>
			{
				switch (args[0].ToLower())
				{
					case "rain":
						Game.World.Weather = new RainWeather();
						break;
					default:
						Game.World.Weather = null;
						break;
				}
			}, "float");

			AddCommand("guitest", "Show GUI test.", args =>
			{
				if (args.Length > 0)
					Game.GuiManager.Add(new Gui.MessageBox(args[0]));
			}, "text");

			AddCommand("hurt", "Hurt yourself.", args =>
			{
				Game.Hud.Observed.GetComponent<CharacterComponent>().Stamina -= int.Parse(args[0]);
			}, "amount");

			AddCommand("additem", "Add an item to your inventory.", args =>
			{
				var item = ContentLoader.Items[args[0]].Clone();
				if (args.Length >= 2)
					item.Count = int.Parse(args[1]);
				Game.Hud.Observed.GetComponent<InventoryComponent>().AddItem(item);
			}, "id", "[count]");

			AddCommand("debugview", "Toggle debugview.", args =>
			{
				if (args.Length > 0)
					Game.World.DebugView.Enabled = bool.Parse(args[0]);
				else
					Game.World.DebugView.Enabled = !Game.World.DebugView.Enabled;
			}, "[on]");
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
			// Cursor blinking
			_cursorTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (_cursorTimer >= CURSOR_TIME)
			{
				_cursorTimer %= CURSOR_TIME;
				_cursorVisible = !_cursorVisible;
			}

			// Repeating input on key hold
			if (_inputTimer > 0)
			{
				if (KInput.KBState.IsKeyDown(Keys.Back) ||
					KInput.KBState.IsKeyDown(Keys.Delete) ||
					KInput.KBState.IsKeyDown(Keys.Left) ||
					KInput.KBState.IsKeyDown(Keys.Right))
					_inputTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
				else
					_inputTimer = 0;
			}

			// Last input
			if (KInput.GetKeyState(Keys.Up) == InputState.Pressed && !string.IsNullOrWhiteSpace(PreviousInput))
			{
				Input = PreviousInput; // Restores the last input
			}
			if (Input != null)
			{
				if (KInput.GetKeyState(Keys.Enter) == InputState.Pressed) // Should the input be executed?
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
					if (KInput.KBState.IsKeyDown(Keys.Back) && Input.Length - _cursorIndex > 0 && _inputTimer <= 0)
					{
						_inputTimer += KInput.GetKeyState(Keys.Back) == InputState.Pressed ? INPUT_TIME_F : INPUT_TIME;
						if (_cursorIndex == 0)
							Input = Input.Substring(0, Input.Length - 1); // Remove a character
						else
							Input = Input.Substring(0, Input.Length - 1 - _cursorIndex) + Input.Substring(Input.Length - _cursorIndex);

						RefreshCursor();
					}
					else if (KInput.KBState.IsKeyDown(Keys.Delete) && _cursorIndex > 0 && _inputTimer <= 0)
					{
						_inputTimer += KInput.GetKeyState(Keys.Delete) == InputState.Released ? INPUT_TIME_F : INPUT_TIME;
						Input = Input.Substring(0, Input.Length - _cursorIndex) + Input.Substring(Input.Length - _cursorIndex + 1);
						if (Input.Length != _cursorIndex)
							--_cursorIndex;
						RefreshCursor();
					}

					// Cursor navigation
					if (KInput.KBState.IsKeyDown(Keys.Left) && _inputTimer <= 0)
					{
						_inputTimer += KInput.GetKeyState(Keys.Left) == InputState.Released ? INPUT_TIME_F : INPUT_TIME;
						_cursorIndex = (int)MathHelper.Min(Input.Length, _cursorIndex + 1);
						RefreshCursor();
					}
					if (KInput.KBState.IsKeyDown(Keys.Right) && _inputTimer <= 0)
					{
						_inputTimer += KInput.GetKeyState(Keys.Right) == InputState.Released ? INPUT_TIME_F : INPUT_TIME;
						_cursorIndex = (int)MathHelper.Max(0, _cursorIndex - 1);
						RefreshCursor();
					}
				}

				if (KInput.GetKeyState(Keys.End) == InputState.Released)
				{
					_cursorIndex = 0; // Go to end
				}
				else if (KInput.GetKeyState(Keys.Home) == InputState.Released)
				{
					_cursorIndex = Input.Length; // Go to the beggining
				}

				if (KInput.GetKeyState(Keys.Escape) == InputState.Released)
				{
					Input = null; // Clear input
				}

			}

			// Scrolling
			if (KInput.GetKeyState(Keys.PageUp) == InputState.Released)
			{
				Offset += SCROLL; // Scroll up
			}
			else if (KInput.GetKeyState(Keys.PageDown) == InputState.Released)
			{
				Offset -= SCROLL; // Scroll down
			}

			// Autocomplete
			if (KInput.GetKeyState(Keys.Tab) == InputState.Released && !string.IsNullOrEmpty(Input))
			{
				Input = Input.TrimStart();
				if (!string.IsNullOrEmpty(Input))
				{
					var commands = Commands.Keys.ToList().FindAll(key => key.StartsWith(Input.ToLower(), StringComparison.InvariantCulture));
					if (commands.Count == 1) // If only 1 similar command is available, finish the input string
						Input = commands[0] + " "; 
					else if (commands.Count > 1) // Otherwise list all commands that begin with input
					{
						string common = Input.ToLower();
						int shortest = int.MaxValue;
						commands.ForEach(c => { if (c.Length < shortest) { shortest = c.Length; } });
						// If all similar commands start with the same substring, autocomplete until you reach a character that at least one command differs at
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
								common += commands[0][iCh]; // If all similar commands contain the character, add it to the input
							else
								break;
						}
						if (common.Length > Input.Length) // If more then one character was added to the input, change the input box
							Input = common; 
						else // Otherwise list all similar commands
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

			// Toggle the console
			if (KInput.GetKeyState(Keys.OemSemicolon) == InputState.Pressed) 
			{
				Enabled = !Enabled;
			}
		}

		// Variables important for cursor rendering and character inputing
		private bool _cursorVisible = true;
		private float _cursorTimer = 0, _inputTimer = 0;
		private int _cursorIndex = 0;

		/// <summary>
		/// Draw the console.
		/// </summary>
		/// <param name="spriteBatch">Sprite batch.</param>
		public void Draw(SpriteBatch spriteBatch)
		{
			int cWidth = (int)Game.Resolution.X;
			spriteBatch.GraphicsDevice.ScissorRectangle = new Rectangle(0, 0, cWidth, HEIGHT);
			spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, null, null, new RasterizerState() { ScissorTestEnable = true });
			spriteBatch.DrawRectangle(new Rectangle(0, 0, cWidth, HEIGHT), new Color(0, 0, 0, 200), 0f); // Draw background

			//Draw text
			if (Output.Count > 1 || !string.IsNullOrEmpty(Output[0]))
			{
				int lastModifier = string.IsNullOrWhiteSpace(Output[Output.Count - 1]) ? 1 : 0;
				int drawRange = (int)MathHelper.Min(HEIGHT / Font.LineSpacing + 1, Output.Count - lastModifier);
				int currentOffset = 0;
				for (int i = Offset + lastModifier; i < Math.Min(Output.Count, drawRange + Offset + lastModifier); ++i)
				{
					var line = Output[Output.Count - 1 - i];
					if (!string.IsNullOrWhiteSpace(line))
					{
                        //Escape unknown characters
                        var escapedLine = new StringBuilder(line.Length);
                        foreach (char ch in line)
                        {
                            if (Font.Characters.Contains(ch))
                            {
                                escapedLine.Append(ch);
                            }
                            else
                            {
                                escapedLine.Append('?');
                            }
                        }
                        line = escapedLine.ToString();
                        

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

			// Draw input box
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

		/// <summary>
		/// Execute a command from string and arguments.
		/// </summary>
		/// <param name="command">Name.</param>
		/// <param name="args">Arguments.</param>
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

		/// <summary>
		/// List of lines
		/// </summary>
		List<string> Output = new List<string>(2048) { "Initializing console.", string.Empty };

		/// <summary>
		/// Write a line to the console
		/// </summary>
		public void WriteLine()
		{
			Output.Add(string.Empty);
		}

		/// <summary>
		/// Write a line to the console
		/// </summary>
		/// <param name="line">Text.</param>
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

		/// <summary>
		/// Write a character to the console
		/// </summary>
		/// <param name="value">Character.</param>
		public void Write(char value)
		{
			if (value == '\n')
				Output.Add(string.Empty);
			else
				Output[Output.Count - 1] += value;
		}

		/// <summary>
		/// Make cursor visible and restart it's blinking timer.
		/// </summary>
		public void RefreshCursor()
		{
			_cursorTimer = 0;
			_cursorVisible = true;
		}

		public class ConsoleCommand
		{
			private Action<string[]> _action;
			private int _minimumArguments = 0;

			/// <summary>
			/// Argument template
			/// </summary>
			/// <value>The arguments.</value>
			public string[] Arguments
			{
				get;
				private set;
			}

			/// <summary>
			/// Argument string for 'help' command drawing.
			/// </summary>
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
							&& arguments[i].EndsWith("]", StringComparison.InvariantCulture)) // Skip optional parameters
						{
							_minimumArguments = i;
						}
					}
				}
			}

			/// <summary>
			/// Invoke the command, output an error if arguments are not according to the template
			/// </summary>
			/// <param name="args">Arguments.</param>
			public void Invoke(string[] args)
			{
				if (args.Length < _minimumArguments)
					Console.WriteLine("Invalid parameters, check help for this command.");
				else if (_action != null)
					_action.Invoke(args);
			}
		}

		/// <summary>
		/// Class that helps route System.Console output to a developer console instance.
		/// </summary>
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
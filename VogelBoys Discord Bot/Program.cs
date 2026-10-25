#define Graphics
#define Legacy

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Discord_Bot
{
	// A snowflake is just a 64-bit integer.
	using snowflake = System.UInt64;

	#if Graphics
	using System.Drawing;
	using System.Drawing.Drawing2D;
	using System.Windows.Forms;
	using System.Web;
	#endif

	public static class Extensions
	{
		/// <inheritdoc cref="HttpClient.GetAsync(string?)"/>
		public static JsonNode? GetJson(this HttpClient client, string requestUri)
		{
			return JsonNode.Parse(client.GetStringAsync(requestUri).Result);
		}
	}

	public static class Program
	{
		#if Graphics
		public static RectangleF Fit(SizeF Source, RectangleF Bounds)
		{
			RectangleF r = Bounds;
			r.Height = (Source.Height/Source.Width)*r.Width;

			if(r.Height > Bounds.Height)
			{
				r.Height = Bounds.Height;
				r.Width = (Source.Width/Source.Height)*r.Height;
				r.X+= (Bounds.Width-r.Width)*0.5f;
			}
			else
				r.Y+= (Bounds.Height-r.Height)*0.5f;

			return r;
		}
		#endif

		#if DEBUG
		const string BuildMode = "Debug";
		#else
		const string BuildMode = "Release";
		#endif

		/// <inheritdoc cref="System.IO.ConsoleStream.Write(byte[], int, int)"/>
		/// <inheritdoc cref="System.IO.Stream.Write(byte[], int, int)"/>
		/// <param name="NewLine">If there should be a new line after the buffer.</param>
		public static void WriteBytesToConsole(byte[] buffer, bool NewLine = true)
		{
			var Stream = Console.OpenStandardOutput();
			Stream.Write(buffer, 0, buffer.Length);
			Stream.Dispose();

			if(NewLine)
				Console.WriteLine();
		}

		/// <inheritdoc cref="Console.WriteLine(object?)"/>
		public static void WriteResultToConsole(HttpResponseMessage value) => Console.WriteLine(value.Content.ReadAsStringAsync().Result);
		
		/// <inheritdoc cref="WriteResultToConsole(HttpResponseMessage)"/>
		public static void WriteResultToConsole(Task<HttpResponseMessage> value) => WriteResultToConsole(value.Result);

		
		/// <summary>
		/// Bot token.
		/// </summary>
		const string Token = "Bot BOTTOKEN";
		const string APP_ID = "BOTAPPID";

		/// <summary>
		/// Bot gateway intents.
		/// </summary>
		public const int 
			GUILDS = (1 << 0),
			GUILD_MEMBERS = (1 << 1),
			GUILD_MODERATION = (1 << 2),
			GUILD_EXPRESSIONS = (1 << 3),
			GUILD_INTEGRATIONS = (1 << 4),
			GUILD_WEBHOOKS = (1 << 5),
			GUILD_INVITES = (1 << 6),
			GUILD_VOICE_STATES = (1 << 7),
			GUILD_PRESENCES = (1 << 8),
			GUILD_MESSAGES = (1 << 9),
			GUILD_MESSAGE_REACTIONS = (1 << 10),
			GUILD_MESSAGE_TYPING = (1 << 11),
			DIRECT_MESSAGES = (1 << 12),
			DIRECT_MESSAGE_REACTIONS = (1 << 13),
			DIRECT_MESSAGE_TYPING = (1 << 14),
			MESSAGE_CONTENT = (1 << 15),
			GUILD_SCHEDULED_EVENTS = (1 << 16),
			AUTO_MODERATION_CONFIGURATION = (1 << 20),
			AUTO_MODERATION_EXECUTION = (1 << 21),
			GUILD_MESSAGE_POLLS = (1 << 24),
			DIRECT_MESSAGE_POLLS = (1 << 25);
		
		/// <summary>
		/// Information on how the data is sent to the Discord API.
		/// </summary>
		public const string 
			API_Ver = "9",
			Command_URL = $"https://discord.com/api/v{API_Ver}/applications/{APP_ID}/commands";

		public static HttpClient client;

		static Program()
		{
			client = new HttpClient()
			{
				BaseAddress = new Uri("https://discord.com"),
			};
			client.DefaultRequestHeaders.Add("Authorization", Token);
		}
		
		/// <summary>
		/// Command information
		/// </summary>
		public static JsonNode? 
			 Ping_Command 
			,Cool_Command 
			,Random_Command 
			,RandomFloat_Command 
			,Pat_Command 
			,whoami_Command 
			,Stats_Command 
			,Tokens_Command 
			,Quote_Command 
			,Quote2_Command
			,Gamble_Command
			,Passcode_Command
			,Timeout_Command
			,SnowflakeToDate_Command
		;
		
		// Commands IDS so I don't have to compare strings and can just compare 8 bytes.
		public static snowflake
			 Ping_CommandID
			,Cool_CommandID
			,Random_CommandID
			,RandomFloat_CommandID
			,Pat_CommandID
			,whoami_CommandID
			,Stats_CommandID
			,Tokens_CommandID
			,Quote_CommandID
			,Quote2_CommandID
			,Gamble_CommandID
			,Passcode_CommandID
			,Timeout_CommandID
			,SnowflakeToDate_CommandID
		;

		
		/// <summary>
		/// Color theming information.
		/// </summary>
		public const ConsoleColor DataColor = ConsoleColor.DarkCyan, ErrorColor = ConsoleColor.Red;

		static bool SendHeartbeatNow = false;
		static Thread? Heartbeats = null;

		public static void Main()
		{
			// I don't want to lose some important logs.
			Console.SetBufferSize(Console.BufferWidth, Math.Max(Math.Max(Console.WindowHeight, Console.BufferHeight), 1600));
		Start:

			Console.WriteLine("Retrieving gateway.");
			var gatewayr = client.GetAsync($"https://discord.com/api/v{API_Ver}/gateway");

			gatewayr.Wait();

			var gateway = gatewayr.Result.Content.ReadAsStringAsync().Result;

			var dataj = JsonObject.Parse(gateway);
			Console.ForegroundColor = DataColor;
			Console.WriteLine(gateway);
			Console.ResetColor();
			gateway = dataj["url"].GetValue<string>()+$"/?v={API_Ver}&encoding=json";
			Console.WriteLine("Gateway: {0}", gateway);


			var gate = new ClientWebSocket();
			gate.ConnectAsync(new(gateway), CancellationToken.None).Wait();

			byte[] data = new byte[512*13*10];

			var result = gate.ReceiveAsync(data, CancellationToken.None);

			dataj = JsonObject.Parse(new ArraySegment<byte>(data, 0, result.Result.Count));
			var HeartBeatInterval = dataj["d"]["heartbeat_interval"].GetValue<int>();
			WriteBytesToConsole(data);
			Console.WriteLine("Heartbeat Interval {0} (ms)", HeartBeatInterval);

			int? num = null;

			// This is so Discord shouldn't kick me out of the gateway.
			bool StartHeatbeatThread()
			{
				if(Heartbeats == null || Heartbeats.ThreadState is not ThreadState.Running and not ThreadState.Background)
				{
					Heartbeats = new Thread(() => 
					{
						var Dataj = new JsonObject();
						Dataj["op"] = 1;

						void SendHeartbeat()
						{
							Console.WriteLine("Heartbeat ({0}) - {1}", num.HasValue ? num.Value : "null", DateTime.Now.ToLongTimeString());
							Dataj["d"] = num;
							if(gate.State != WebSocketState.Open)
								return;
							gate.SendAsync(Encoding.UTF8.GetBytes(Dataj.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
						}

						long Next = Environment.TickCount64+(long)(HeartBeatInterval*Random.Shared.NextSingle());
						while(gate.CloseStatus == null)
						{
							while(Environment.TickCount64 < Next)
							{
								if(SendHeartbeatNow)
								{
									SendHeartbeat();
									SendHeartbeatNow = false;
								}

								Thread.Sleep(100);
								Thread.Yield();
							}
							Next = Environment.TickCount64+HeartBeatInterval;

							SendHeartbeat();
						}
					})
					{
						IsBackground = true,
						Name = "Heartbeat",
					};
					Heartbeats.Start();
					return true;
				}
				return false;
			}
			StartHeatbeatThread();

			dataj = new JsonObject();
			dataj["op"] = 2;
			dataj["d"] = new JsonObject();
			dataj["d"]["token"] = Token;
			#if Legacy
			dataj["d"]["intents"] = MESSAGE_CONTENT | GUILD_MESSAGES;
			#else
			dataj["d"]["intents"] = 0;
			#endif
			dataj["d"]["compress"] = false;
			dataj["d"]["large_threshold"] = 50;
			
			dataj["d"]["properties"] = new JsonObject();
			dataj["d"]["properties"]["os"] = Environment.OSVersion.ToString();
			dataj["d"]["properties"]["browser"] = "VogelBoys Discord Bot";
			dataj["d"]["properties"]["device"] = ".NET Framework";

			var presence = new JsonObject();
			presence["since"] = null;
			var BotStatus = new JsonObject();
			presence["activities"] = new JsonArray(BotStatus);
			
			BotStatus["name"] = "Status";
			#if DEBUG
			BotStatus["state"] = "Debug, expect the bot to go offline.";
			presence["status"] = "dnd";
			#else
			if(File.Exists(Environment.CurrentDirectory+"\\Status.txt"))
			{
				BotStatus["state"] = File.ReadAllText(Environment.CurrentDirectory+"\\Status.txt");
				var watcher = new FileSystemWatcher(Environment.CurrentDirectory, "Status.txt")
				{
					EnableRaisingEvents = true,
					IncludeSubdirectories = false,
				};
				watcher.Changed+= (sender, e) => 
				{
					Retry:
					try
					{
						Console.WriteLine("{0} was changed.", e.FullPath);
						BotStatus["state"] = File.ReadAllText(Environment.CurrentDirectory+"\\Status.txt");

						var Update = new JsonObject();
						Update["op"] = 3;
						Update["d"] = presence.DeepClone();
						gate.SendAsync(Encoding.UTF8.GetBytes(Update.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
					}
					catch(Exception E)
					{
						Console.ForegroundColor = ErrorColor;
						Console.WriteLine(E);
						Console.ResetColor();
						Thread.Sleep(1000);
						goto Retry;
					}
				};
			}
			else
				BotStatus["state"] = string.Empty;
			presence["status"] = "online";
			#endif
			BotStatus["type"] = 4;
			presence["afk"] = false;

			//dataj["d"]["presence"] = null;
			dataj["d"]["presence"] = presence;

			Task<HttpResponseMessage> response;

			Console.WriteLine(dataj);

			// Convert the JSON data to UTF8 data and send it through the gateway to identify the bot.
			gate.SendAsync(Encoding.UTF8.GetBytes(dataj.ToString()), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

			//Console.WriteLine(response.Result);

			// Sends a message from the bot in a channel. You mainly would see this with the Legacy things from VogelBoys that I used before I learnt how to use the / commands.
			Task<HttpResponseMessage> SendMessage(string Content, string Channel, int flags = 0/*, bool tts = false*/)
			{
				return client.PostAsJsonAsync
				(
					$"https://discord.com/api/v{API_Ver}/channels/{Channel}/messages", 
					new 
					{
						content = Content,
						//nonce = Random.Shared.NextInt64(long.MaxValue),
						enforce_nonce = false,
						//tts = tts,
						flags = flags,
					}
				);
			}
			Task<HttpResponseMessage> InteractionResponse(string Content, string ID, string Token/*, bool tts = false*/, bool Hide = false)
			{	
				return client.PostAsJsonAsync
				(
					$"https://discord.com/api/v{API_Ver}/interactions/{ID}/{Token}/callback", 
					new 
					{
						type = 4,
						data = new
						{
							//tts = tts,
							with_response = false,
							content = Content,
							flags = Hide ? (1 << 6) : 0,
						},
					}
				);
			}

			// Register the commands, retrieve the command data and delete unknown commands.
			void RegisterCommands()
			{
				// I wish there was an easier way I knew to do this, but it works and isn't terrible.
				Console.WriteLine("Registering Commands...");

				int[] DefaultIntergration = new int[] {0,1}, DefaultContexts = new int[] {0,2};
				var a = 
				(
					PingCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "ping",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Check if the bot is online.",
						contexts = DefaultContexts,
					}), 
					CoolCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "cool",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Does a cool thing.",
						contexts = DefaultContexts,
						options = new object[]
						{
							new
							{
								type = 4,
								name = "lines",
								description = "The amount of lines to draw.",
								required = true,
								min_value = 1,
								max_value = 3000,
							},
							new
							{
								type = 4,
								name = "width",
								description = "Image width",
								required = true,
								min_value = 120,
								max_value = 2000,
							},
							new
							{
								type = 4,
								name = "height",
								description = "Image height",
								required = true,
								min_value = 120,
								max_value = 2000,
							},
							new
							{
								type = 5,
								name = "alpha",
								description = "If the lines can be transparent.",
								required = false,
							},
							new
							{
								type = 10,
								name = "penwidth",
								description = "The width of the pen.",
								required = false,
								min_value = 1f,
								max_value = 2000f,
							},
						},
					}), 
					RandomCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "random",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Roll the dice.",
						contexts = DefaultContexts,
						options = new[]
						{
							new
							{
								type = 4,
								name = "minimum",
								description = "Minimum number to roll.",
								required = true,
							},
							new
							{
								type = 4,
								name = "maximum",
								description = "Maximum number to roll.",
								required = true,
							},
						},
					}), 
					RandomFloatCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "randomfloat",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Roll the dice as a decimal.",
						contexts = DefaultContexts,
						options = new[]
						{
							new
							{
								type = 10,
								name = "minimum",
								description = "Minimum number to roll.",
								required = true,
							},
							new
							{
								type = 10,
								name = "maximum",
								description = "Maximum number to roll.",
								required = true,
							},
						},
					}), 
					PatCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "pat",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Pat.",
						contexts = DefaultContexts,
					}), 
					whoamiCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "whoami",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Who am I?",
						contexts = DefaultContexts,
					}),
					StatsCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "stats",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Get info about the instance.",
						contexts = DefaultContexts,
					}),
					TokensCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "tokens",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Ye",
						contexts = DefaultContexts,
					}),
					QuoteCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "quote",
						type = 3,
						integration_types = DefaultIntergration,
						//description = "Ya cool stuff happens here.",
						contexts = DefaultContexts,
					}),
					Quote2CommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "quotehq",
						type = 3,
						integration_types = DefaultIntergration,
						//description = "Ya cool stuff happens here.",
						contexts = DefaultContexts,
					}),
					GambleCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "lootboxreal",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Free",
						contexts = DefaultContexts,
					}),
					PasscodeCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "code",
						type = 1,
						integration_types = DefaultIntergration,
						description = "na na na boo boo 🍭🐱",
						options = new[]
						{
							new
							{
								type = 3,
								name = "code",
								description = "Passcode.",
								required = true,
							},
						},
						contexts = DefaultContexts,
					}),
					TimeoutCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "timeoutlength",
						type = 1,
						integration_types = new int[] {0},
						description = "Timeout a user for a custom length.",
						options = new[]
						{
							new
							{
								type = 6,
								name = "user",
								description = "The user to timeout.",
								required = true,
							},
							new
							{
								type = 4,
								name = "days",
								description = "The amount of days to time the user out.",
								required = true,
							},
							new
							{
								type = 4,
								name = "hours",
								description = "The amount of days to time the user out.",
								required = true,
							},
							new
							{
								type = 4,
								name = "minutes",
								description = "The amount of days to time the user out.",
								required = true,
							},
							new
							{
								type = 4,
								name = "seconds",
								description = "The amount of days to time the user out.",
								required = true,
							},
							new
							{
								type = 3,
								name = "reason",
								description = "The reason to associate the timeout with.",
								required = true,
							},
						},
						contexts = new int[] {0},
						default_member_permissions = (1L << 40).ToString(), // ResolvedBug: Was a 32-bit number and trying to use the 40th bit, which doesn't exist. Resolution: Change to 64-bit.
					}),
					SnowflakeToDateCommandData: JsonSerializer.SerializeToNode(new 
					{
						name = "snowflaketodatetime",
						type = 1,
						integration_types = DefaultIntergration,
						description = "Converts a Discord snowflake to a date.",
						options = new[]
						{
							new
							{
								type = 3,
								name = "snowflake",
								description = "The snowflake to convert.",
								required = true,
							},
						},
						contexts = DefaultContexts,
					})
				);
				Console.WriteLine("Created JSON");
				Console.ForegroundColor = ConsoleColor.DarkGreen;

				JsonArray CommandsEdit = new JsonArray
				(
					a.Item1,
					a.Item2,
					a.Item3,
					a.Item4,
					a.Item5,
					a.Item6,
					a.Item7,
					a.Item8,
					a.Item9,
					a.Item10,
					a.Item11,
					a.Item12,
					a.Item13,
					a.Item14
				);

				//var CommandResult = (JsonArray)JsonArray.Parse(client.PutAsJsonAsync(Command_URL, CommandsEdit).Result.Content.ReadAsStringAsync().Result);
				response = client.PutAsJsonAsync(Command_URL, CommandsEdit);
				WriteResultToConsole(response);

				var Commands = (JsonArray)client.GetJson(Command_URL);

				//Console.WriteLine(CommandResult);
				Console.WriteLine(CommandsEdit);
				
				foreach(var i in Commands)
				{
					Console.WriteLine(i["name"].GetValue<string>());
					switch(i["name"].GetValue<string>())
					{
						case "ping":
						Ping_Command = i;
						break;
						case "cool":
						Cool_Command = i;
						break;
						case "random":
						Random_Command = i;
						break;
						case "randomfloat":
						RandomFloat_Command = i;
						break;
						case "pat":
						Pat_Command = i;
						break;
						case "whoami":
						whoami_Command = i;
						break;
						case "stats":
						Stats_Command = i;
						break;
						case "tokens":
						Tokens_Command = i;
						break;
						case "quote":
						Quote_Command = i;
						break;
						case "quotehq":
						Quote2_Command = i;
						break;
						case "lootboxreal":
						Gamble_Command = i;
						break;
						case "code":
						Passcode_Command = i;
						break;
						case "timeoutlength":
						Timeout_Command = i;
						break;
						case "snowflaketodatetime":
						SnowflakeToDate_Command = i;
						break;
						default:
						// Delete unknown commands.
						client.DeleteAsync($"{Command_URL}/{i["id"].GetValue<string>()}");
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("Deleted {0}.", i["name"].GetValue<string>());
						Console.ForegroundColor = ConsoleColor.DarkGreen;
						continue;
					}
				}

				Ping_CommandID = snowflake.Parse(Ping_Command["id"].GetValue<string>());
				Cool_CommandID = snowflake.Parse(Cool_Command["id"].GetValue<string>());
				Random_CommandID = snowflake.Parse(Random_Command["id"].GetValue<string>());
				RandomFloat_CommandID = snowflake.Parse(RandomFloat_Command["id"].GetValue<string>());
				Pat_CommandID = snowflake.Parse(Pat_Command["id"].GetValue<string>());
				whoami_CommandID = snowflake.Parse(whoami_Command["id"].GetValue<string>());
				Stats_CommandID = snowflake.Parse(Stats_Command["id"].GetValue<string>());
				Tokens_CommandID = snowflake.Parse(Tokens_Command["id"].GetValue<string>());
				Quote_CommandID = snowflake.Parse(Quote_Command["id"].GetValue<string>());
				Quote2_CommandID = snowflake.Parse(Quote2_Command["id"].GetValue<string>());
				Gamble_CommandID = snowflake.Parse(Gamble_Command["id"].GetValue<string>());
				Passcode_CommandID = snowflake.Parse(Passcode_Command["id"].GetValue<string>());
				Timeout_CommandID = snowflake.Parse(Timeout_Command["id"].GetValue<string>());
				SnowflakeToDate_CommandID = snowflake.Parse(SnowflakeToDate_Command["id"].GetValue<string>());

				Console.ResetColor();
			}

			RegisterCommands();

			Console.WriteLine("Begin main loop.");

			// I have it somewhere so if I get an unexpected disconnect then I restart the entire bot, I don't know why Discord randomly disconnects the bot from the gateway and it's annoying.
			while(true)
			{
				if(gate.CloseStatus != null)
				{
					Console.WriteLine("Gateway was closed with status {0} ({2}).\nDescription: {1}", gate.CloseStatus, gate.CloseStatusDescription, (int)gate.CloseStatus);
					break;
				}

				result = gate.ReceiveAsync(data, CancellationToken.None);
				Console.WriteLine("Waiting for packet.");
				try
				{
					result.Wait();
				}
				catch(AggregateException e)
				{
					Console.WriteLine("I ded\n{0}", e);
					//new Thread(() => MessageBox.Show($"I ded\n{e}", default, MessageBoxButtons.OK, MessageBoxIcon.Error)).Start();
					goto Start;
				}
				if(result.Result.Count <= 0)
				{
					Console.WriteLine("No data received.");
					Thread.Sleep(300);
					continue;
				}

				dataj = JsonObject.Parse(new ArraySegment<byte>(data, 0, result.Result.Count));
				Console.ForegroundColor = DataColor;
				Console.WriteLine(dataj);
				Console.ResetColor();

				switch(dataj["op"].GetValue<int>())
				{
					//Heartbeat
					case 1:
					SendHeartbeatNow = true;
					break;
					//Dispatch
					case 0:
					Console.WriteLine("Dispatch data received.");
					num = dataj["s"].GetValue<int?>();
					{
						switch(dataj["t"].GetValue<string>())
						{
							#if Legacy
							case "MESSAGE_CREATE":
							{
								string content = dataj["d"]["content"].GetValue<string>(),
								channel = dataj["d"]["channel_id"].GetValue<string>();

								if(content.Equals("!ping", StringComparison.InvariantCultureIgnoreCase))
									SendMessage("Pong!", channel);
								else if(content.Equals("!help", StringComparison.InvariantCultureIgnoreCase))
									SendMessage("Please use bot commands, this is deprecated.\n-Commands-\n!ping - Check if the bot is online.", channel);
								else if(content.Contains("nobody", StringComparison.InvariantCultureIgnoreCase))
									SendMessage("<@1434184285954375680>", channel);
							}
							break;
							#endif
							case "INTERACTION_CREATE":
							try
							{
								snowflake Cmd_ID = snowflake.Parse(dataj["d"]["data"]["id"].GetValue<string>());
								if(Ping_CommandID == Cmd_ID)
								{
									response = InteractionResponse("guh", dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>());
									Console.WriteLine(response.Result);
								}
								else if(Cool_CommandID == Cmd_ID)
								{
									#if Graphics
									var image = new Bitmap(dataj["d"]["data"]["options"][1]["value"].GetValue<int>(), dataj["d"]["data"]["options"][2]["value"].GetValue<int>());
									var g = Graphics.FromImage(image);

									Pen pen = new Pen(Color.Black, 1.4f);
									{
										var Opt = dataj["d"]["data"]["options"].Find("name", "alpha");
										if(Opt == null || !Opt["value"].GetValue<bool>())
											g.CompositingMode = CompositingMode.SourceCopy;
										else
											g.CompositingMode = CompositingMode.SourceOver;

										Opt = dataj["d"]["data"]["options"].Find("name", "penwidth");
										if(Opt != null)
											pen.Width = Opt["value"].GetValue<float>();
									}

									for(int i = 0; i < dataj["d"]["data"]["options"][0]["value"].GetValue<int>(); i++)
									{
										g.DrawLine(pen, Random.Shared.Next(0, image.Width), Random.Shared.Next(0, image.Height), Random.Shared.Next(0, image.Width), Random.Shared.Next(0, image.Height));
										//pen.Color = ColorTranslator.FromWin32(Random.Shared.Next());
										if(g.CompositingMode == CompositingMode.SourceCopy)
											pen.Color = Color.FromArgb(Random.Shared.Next(1 << 24) | (byte.MaxValue << 24));
										else
											pen.Color = Color.FromArgb(Random.Shared.Next(int.MinValue, int.MaxValue));
									}
									pen.Dispose();
									g.Dispose();

									var ImgData = new MemoryStream();
									image.Save(ImgData, System.Drawing.Imaging.ImageFormat.Png);
									var ImgDataA = ImgData.GetBuffer();

									var ImgByte = new ByteArrayContent(ImgDataA);

									var JsonData = JsonContent.Create
									(
										new
										{
											type = 4,
											data = new
											{
												tts = false,
												content = "Here is a cool thing.",
												attachments = new[] 
												{
													new 
													{
														id = 0,
														filename = "Cool Image.png",
														title = "Cool",
													},
												},
											},
										}
									);

									//JsonData.Headers.Add("content-type", "application/json");
									ImgByte.Headers.Add("content-type", "image/png");

									var Content = new MultipartFormDataContent("boundry")
									{
										{JsonData, "payload_json"},
										{ImgByte, "files[0]", "Cool Image.png"},
									};

									//Console.WriteLine(Content.ReadAsStringAsync().Result);

									response = client.PostAsync
									(
										$"https://discord.com/api/v{API_Ver}/interactions/{dataj["d"]["id"].GetValue<string>()}/{dataj["d"]["token"].GetValue<string>()}/callback", 
										Content
									);
									response.Wait();
									image.Dispose();
									ImgData.Dispose();
									WriteResultToConsole(response);
									#else
									response = InteractionResponse
									(
										"<:Error:1496202946948497478> This command uses GDI+, which is not available.\n-# To fix this issue, build the bot for Windows.",
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
									#endif
								}
								else if(Random_CommandID == Cmd_ID)
								{
									long min = dataj["d"]["data"]["options"][0]["value"].GetValue<long>();
									long max = dataj["d"]["data"]["options"][1]["value"].GetValue<long>();
									response = InteractionResponse
									(
										$"Your random number from {min} to {max} is {Random.Shared.NextInt64(min > max ? max : min, (min > max ? min : max)+1)}",
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
									Console.WriteLine(response.Result);
								}
								else if(RandomFloat_CommandID == Cmd_ID)
								{
									float fMin = dataj["d"]["data"]["options"][0]["value"].GetValue<float>();
									float fMax = dataj["d"]["data"]["options"][1]["value"].GetValue<float>();
									response = InteractionResponse
									(
										$"Your random number from {fMin} to {fMax} is {MathF.FusedMultiplyAdd(Random.Shared.NextSingle(), fMax-fMin, fMin)}",
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
									Console.WriteLine(response.Result);
								}
								else if(Pat_CommandID == Cmd_ID)
								{
									response = InteractionResponse
									(
										"https://cdn.discordapp.com/attachments/1437593920170233986/1496127340005167276/pat.gif?ex=69e8c0aa&is=69e76f2a&hm=f0d67b9845e9c53c9a656200e141a57758f7e35a47939eec7d0a4a4fb227c179&animated=true", 
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
									Console.WriteLine(response.Result);
								}
								else if(whoami_CommandID == Cmd_ID)
								{
									response = InteractionResponse
									(
										dataj["d"]["member"]["user"]["global_name"].GetValue<string>(), 
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
									Console.WriteLine(response.Result);
								}
								else if(Stats_CommandID == Cmd_ID)
								{
									[System.Runtime.InteropServices.DllImport("ucrtbase.dll")]
									static extern Int32 clock();

									response = InteractionResponse
									(
										//This looks ugly.
$@"OS: {Environment.OSVersion} (x{(Environment.Is64BitOperatingSystem ? "64" : "86")})
Runtime: Microsoft .NET Core {Environment.Version} {BuildMode} (x{(Environment.Is64BitProcess ? "64" : "86")})
Uptime: {new TimeSpan(0,0,0,0,clock()):g}
Server uptime: {new TimeSpan(0,0,0,0,Environment.TickCount):g}
Discord API version {API_Ver}"
#if Graphics
+ (SystemInformation.PowerStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery ? string.Empty : $@"
Battery: {SystemInformation.PowerStatus.BatteryLifePercent*100:f0}% {(SystemInformation.PowerStatus.BatteryChargeStatus == BatteryChargeStatus.Charging ? "⚡" : string.Empty)}
Powerline: {SystemInformation.PowerStatus.PowerLineStatus}")
#endif
, 
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
									Console.WriteLine(response.Result);
								}
								else if(Tokens_CommandID == Cmd_ID)
								{
									response = InteractionResponse
									(
										"So, I'm kinda broke.", 
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
									Console.WriteLine(response.Result);
								}
								else if(Quote_CommandID == Cmd_ID || Quote2_CommandID == Cmd_ID)
								{
									#if Graphics
									bool HQ = Quote2_CommandID == Cmd_ID;
									Font? font = HQ ? new Font(Control.DefaultFont.FontFamily, 90, FontStyle.Regular, GraphicsUnit.Pixel) : default;

									string avatarSrc;
									var message = dataj["d"]["data"]["resolved"]["messages"][dataj["d"]["data"]["target_id"].GetValue<string>()];
									//Console.WriteLine(message.ToJsonString());
									var Attachments = message["attachments"].AsArray();

									string Text = message["content"].GetValue<string>();

									void SearchForUrl(string Indicator)
									{
										int i = Text.IndexOf(Indicator, StringComparison.InvariantCultureIgnoreCase), endI;
										while(true)
										{
											if(i == -1)
												break;

											endI = Text.IndexOfAny([' ', ')'], i+Indicator.Length);
											Console.WriteLine(endI == -1 ? Text.Substring(i) : Text[i..endI]);
											Attachments.Add
											(
												JsonSerializer.SerializeToElement(new
												{
													url = endI == -1 ? Text.Substring(i) : Text[i..endI],
												})
											);

											if(endI == -1)
												break;

											i = Text.IndexOf(Indicator, i+Indicator.Length, StringComparison.InvariantCultureIgnoreCase);
										}
									}
									SearchForUrl("http://");
									SearchForUrl("https://");
									
									/*if(message["author"]["avatar"] != null)
									{
										//I can't seem to retrieve the avatar ID when it isn't the one sending the command.
										avatarSrc = $"https://cdn.discordapp.com/guilds/{dataj["d"]["channel"]["guild_id"].GetValue<string>()}/users/{message["author"]["id"].GetValue<string>()}/avatars/{message["author"]["avatar"].GetValue<string>()}.png?size=4096&quality=lossless";
									}
									else */if(message["author"]["avatar"] != null)
									{
										avatarSrc = $"https://cdn.discordapp.com/avatars/{message["author"]["id"].GetValue<string>()}/{message["author"]["avatar"].GetValue<string>()}.png?size=4096&quality=lossless";
									}
									else
									{
										ulong defaultAvatarIndex = (ulong.Parse(message["author"]["id"].GetValue<string>()) >> 22) % 6;
										avatarSrc = $"https://cdn.discordapp.com/embed/avatars/{defaultAvatarIndex}.png";
									}

									Size BaseSize = new Size(3820/(HQ ? 1 : 7), 1080/(HQ ? 1 : 7));

									Bitmap image;
									/*bool StackHorizontal = false;
									(Attachments.Count > 1)
									{
										JsonNode Largest = null;
										{
											JsonNode LargestHeight = null;

											int Width = 0, Height = 0;

											foreach(var i in Attachments)
											{
												int w = i["width"].GetValue<int>(), h = i["height"].GetValue<int>();
												Width+= w;
												Height+= h;

												if(Largest == null)
												{
													Largest = i;
													LargestHeight = i;
												}
												else
												{
													if(w > Largest["width"].GetValue<int>())
														Largest = i;
													if(h > Largest["height"].GetValue<int>())
														LargestHeight = i;
												}
											}

											StackHorizontal = Width < Height;

											if(!StackHorizontal)
												Largest = LargestHeight;
										}

										float Size;
										if(StackHorizontal)
										{
											Size = BaseSize.Width;



											image = new Bitmap((int)Size, BaseSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
										}
										else
										{
											Size = BaseSize.Height;



											image = new Bitmap(BaseSize.Width, (int)Size, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
										}
									}
									else
										*/image = new Bitmap(BaseSize.Width, BaseSize.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
									var g = Graphics.FromImage(image);

									if(!HQ)
									{
										g.InterpolationMode = InterpolationMode.NearestNeighbor;
										g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
									}

									var PictureStream = client.GetStreamAsync(avatarSrc).Result;
									var Picture = Image.FromStream(PictureStream);

									g.DrawImage(Picture, 0,0,image.Height,image.Height);

									Picture.Dispose();
									PictureStream.Dispose();

									Rectangle TextRect = new Rectangle(image.Height, 0, image.Width-image.Height, image.Height);

									string Error = null;
									if(Attachments.Count > 0)
									{
										TextRect.X++;
										TextRect.Width--;

										TextRenderer.DrawText(g, Text, font, TextRect, Color.White, TextFormatFlags.HorizontalCenter|TextFormatFlags.Top|TextFormatFlags.WordBreak);
										var TextSize = TextRenderer.MeasureText(Text, font, TextRect.Size, TextFormatFlags.HorizontalCenter|TextFormatFlags.Top|TextFormatFlags.WordBreak|TextFormatFlags.NoFullWidthCharacterBreak);
										var TextSize2 = TextRenderer.MeasureText("This sentence is for measuring.", font, TextRect.Size, TextFormatFlags.Bottom);
										
										g.FillRectangles(Brushes.Gray, new Rectangle[]
										{
											new Rectangle(image.Height, TextSize.Height, image.Width, 1),
											new Rectangle(image.Height, image.Height-TextSize2.Height-1, image.Width, 1),
											new Rectangle(image.Height, 0, 1, image.Height),
										});
										Console.WriteLine("{0} Attachments", Attachments.Count);
										
										RectangleF Bounds = Rectangle.FromLTRB(TextRect.Left, TextSize.Height+1, image.Width, image.Height-TextSize2.Height-1);
										float AddXY = /*StackHorizontal ? Bounds.Width/Attachments.Count : */Bounds.Height/Attachments.Count;
										for(int i = 0; i < Attachments.Count; i++)
										{
											try
											{
												PictureStream = client.GetStreamAsync(Attachments[i]["url"].GetValue<string>()).Result;
												Picture = Image.FromStream(PictureStream);
											}
											catch(Exception e)
											{
												PictureStream = null;
												Picture = SystemIcons.Error.ToBitmap();
												Error+= $"-# <:Error:1496202946948497478> Failed to load attachment {Attachments[i]["url"].GetValue<string>()}. Error: {e.Message}\n";

												Console.ForegroundColor = ErrorColor;
												Console.WriteLine(e);
												Console.ResetColor();
											}

											/*if(StackHorizontal)
												g.DrawImage(Picture, Fit(Picture.Size, new RectangleF(Bounds.X+(AddXY*i),Bounds.Y, AddXY, Bounds.Height)));
											else
												*/g.DrawImage(Picture, Fit(Picture.Size, new RectangleF(Bounds.X,Bounds.Y+(AddXY*i), Bounds.Width, AddXY)));

											Picture.Dispose();
											PictureStream?.Dispose();
										}
									}
									else
										TextRenderer.DrawText(g, Text, font, TextRect, Color.White, TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak|TextFormatFlags.NoFullWidthCharacterBreak);
									
									TextRenderer.DrawText(g, $"-{message["author"]["username"].GetValue<string>()}", font, new Rectangle(TextRect.X, 0, TextRect.Width, image.Height), Color.Gray, TextFormatFlags.Bottom|TextFormatFlags.Right);
									TextRenderer.DrawText(g, $"{DateTime.Parse(message["timestamp"].GetValue<string>())} CST", font, new Rectangle(TextRect.X, 0, TextRect.Width, image.Height), Color.Gray, TextFormatFlags.Bottom|TextFormatFlags.Left);


									g.Dispose();

									var ImgData = new MemoryStream();
									image.Save(ImgData, System.Drawing.Imaging.ImageFormat.Png);
									var ImgDataA = ImgData.GetBuffer();

									var ImgByte = new ByteArrayContent(ImgDataA);
									Console.WriteLine("File size: {0} MBs", ImgDataA.Length/1000000);

									var JsonData = JsonContent.Create
									(
										new
										{
											type = 4,
											data = new
											{
												tts = false,
												//content = "<:Please:1494845548585291927>",
												content = Error,
												attachments = new[] 
												{
													new 
													{
														id = 0,
														filename = "Quote.png",
														title = "Quote",
													},
												},
											},
										}
									);

									var Content = new MultipartFormDataContent("boundry");
									Content.Add(JsonData, "payload_json");
									ImgByte.Headers.Add("content-type", "image/png");
									Content.Add(ImgByte, "files[0]", "Quote.png");

									/*new Form()
									{
										BackgroundImage = image,
										BackgroundImageLayout = ImageLayout.Zoom,
									}.Show();*/

									response = client.PostAsync
									(
										$"https://discord.com/api/v{API_Ver}/interactions/{dataj["d"]["id"].GetValue<string>()}/{dataj["d"]["token"].GetValue<string>()}/callback", 
										Content
									);
									//response.Wait();
									image.Dispose();
									ImgData.Dispose();
									font?.Dispose();
									WriteResultToConsole(response);
									#else
									response = InteractionResponse
									(
										"<:Error:1496202946948497478> This command uses GDI+, which is not available.\n-# To fix this issue, build the bot for Windows.",
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
									#endif
								}
								else if(Gamble_CommandID == Cmd_ID)
								{
									var random = Random.Shared.Next(10);
									Console.WriteLine(random);
									response = InteractionResponse
									(
										random != 0 ?
										"https://tenor.com/view/i-just-hit-the-jackpot-jackpot-i-just-hit-i-just-i-gif-6623346210678000489"
										:
										"https://tenor.com/view/mario-super-mario-bros-depression-sad-mario-and-luigi-gif-14815881022592239601", 
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
									Console.WriteLine(response.Result);
								}
								else if(Passcode_CommandID == Cmd_ID)
								{
									string? r = null;
									var p = dataj["d"]["data"]["options"][0]["value"].GetValue<string>();

									switch(p.ToLowerInvariant())
									{
										case "wyatt":
										r = "no";
										break;
										case "finn":
										r = "https://media.discordapp.net/stickers/1173450580740554773.webp?size=4096&quality=lossless";
										break;
										case "jory":
										case "unfunnyjory":
										case "justjory":
										r = "<t:1777185410:S>";
										break;
										case "chairboard":
										r = "https://tenor.com/view/dr-hax-hax-hacks-hacker-hackers-gif-1970435453844108305";
										break;
										case "alex smith":
										//r = "I remember smelling lavender.";
										r = "Alex Smith";
										break;
										case "vogelboys":
										r = "I like computers.";
										break;
										case "birthday":
										r = "Don't ask for this information, I'm not allowed to give it.";
										break;
										case "mikumikubeam2":
										case "mikumikubibeam2":
										case "beamku":
										r = "▭";
										break;
										case "1000mcnuggets":
										r = "https://media.discordapp.net/stickers/1495558367630852297.webp?size=4096&quality=lossless";
										break;
										case "yuki":
										case "yukix":
										r = "https://media.discordapp.net/stickers/948666978002931742.webp?size=4096&quality=lossless";
										break;
										case "guh":
										r = "https://media.discordapp.net/stickers/896411892745142293.png?size=32";
										break;
										case "a":
										r = "https://www.youtube.com/watch?v=uXIs-L8wB5s";
										break;
										case "b":
										r = "Flashling Lights https://www.youtube.com/watch?v=LXCVkbdHR9s&list=PL-42dGAIbsZRhG1-QfgAwlmqx3Cz2xiF0&index=14";
										break;
										case "c":
										r = "https://youtube.com/live/GR9v-nyT95I";
										break;
										case "d":
										r = "https://www.youtube.com/watch?v=ECfggAHG0ys";
										break;
										case "e":
										r = "https://www.youtube.com/watch?v=DiqcB_p4OPA";
										break;
										case "f":
										r = "https://www.youtube.com/watch?v=9iqg8-DUOGM&t=46s";
										break;
										case "g":
										r = "https://www.youtube.com/watch?v=kv1HtrCoiGk";
										break;
										case "h":
										r = "https://www.youtube.com/watch?v=394hinKUIxw&t=17475s";
										break;
										case "i":
										r = "https://www.youtube.com/watch?v=BeG5FqTpl9U";
										break;
										case "j":
										r = "https://www.youtube.com/watch?v=Ib2Vl7JEjfc";
										break;
										case "k":
										r = "https://www.youtube.com/watch?v=NNlLjTpmyu0";
										break;
										case "l":
										r = "https://www.youtube.com/watch?v=lvhqCavWdK0";
										break;
										case "m":
										r = "https://www.youtube.com/watch?v=mSCB8qeJs5o&t=33s";
										break;
										case "n":
										r = "https://www.youtube.com/watch?v=_hBS562RLtI";
										break;
										case "o":
										r = "https://www.youtube.com/watch?v=-9THfUPGfwQ";
										break;
										case "p":
										r = "https://www.youtube.com/watch?v=98PG9lDk8VM";
										break;
										case "q":
										r = "https://www.youtube.com/watch?v=VKJkkHUw2dY";
										break;
										case "r":
										r = "https://www.youtube.com/watch?v=42jv-WiSkPc";
										break;
										case "s":
										r = "https://www.youtube.com/watch?v=wjpUaYHXwOA";
										break;
										case "t":
										r = "https://www.youtube.com/watch?v=4d8oYTme3eM";
										break;
										case "u":
										r = "https://www.youtube.com/watch?v=5I0bz_Cbinc";
										break;
										case "v":
										r = "https://www.youtube.com/watch?v=2WvMPZ--2qo";
										break;
										case "w":
										r = "https://www.youtube.com/watch?v=9xp1XWmJ_Wo";
										break;
										case "x":
										r = "https://www.youtube.com/watch?v=XBEz8s-0hnU";
										break;
										case "y":
										r = "https://www.youtube.com/watch?v=yJEPTk7FLmk&list=PL-42dGAIbsZTI84RyiPLM0j4I8zSPIfPr";
										break;
										case "z":
										//r = "I won't forget.";
										//r = "Reminds me of something.";
										r = "https://www.youtube.com/watch?v=fzDZrPZ8s5c&list=PL-42dGAIbsZRhG1-QfgAwlmqx3Cz2xiF0&index=599";
										break;
										case "help":
										r = "Help me.";
										break;
										case "akinator":
										r = "I do not want to see his face.";
										break;
										case "mikowei":
										r = "I know what you are.";
										break;
										case "clue":
										//r = "Can I please be unbanned? That happened like 5 years ago and I've changed. Also wasn't that bad of a rule break, I promise not to do it again.";
										r = "no.";
										break;
										case "password":
										case "code":
										r = "Very creative.";
										break;
										case "cookie":
										r = "Give.";
										break;
										case "paint":
										r = "It smells good.";
										break;
										case "burger":
										case "burgers":
										case "taco":
										case "burrito":
										r = "I don't want that.";
										break;
										case "ivycomb":
										r = "https://media.discordapp.net/stickers/947969044236107856.webp?size=4096&quality=lossless";
										break;
										case "grey":
										r = "<@1045336951798112328>";
										break;
										case "gray":
										r = "<@525838000333127725>";
										break;
										case "moderator":
										r = "The horror...\nI don't want to get moderator, I'm a minor and the stuff that probably gets posted to mod chat or mod info I don't want to see.";
										break;
										case "c#":
										r = "I think there is something better you could try guessing.";
										break;
										case "eden":
										r = "I'm tired.";
										break;/* GROOMER PEDOPHILE
										case "phobia":
										case "phobiah":
										r = "Hello.";
										break;*/
										case "jefferey sampson":
										r = "Hey.";
										break;
										case "clarence walker":
										case "the real clarence walker":
										r = "Dog.";
										break;
										case "chaircord":
										case "esmbot":
										r = "My husband?!";
										break;
										case "hi":
										r = "Hello.";
										break;
										case "hello":
										r = "Hi.";
										break;
										case "sanity":
										r = "It's virtual.";
										break;
										case "vent":
										case "vents":
										r = "Sorry, that's private.";
										break;
										case "die":
										r = "https://media.discordapp.net/stickers/864191452807168000.png?size=4096&quality=lossless";
										break;
										case "pizza":
										r = "Give it to me, please.";
										break;
										case "999mcnuggets":
										r = "https://media.discordapp.net/stickers/1441085235885506582.webp?size=4096&quality=lossless";
										break;
										case "bee":
										r = "https://cdn.discordapp.com/emojis/946106014204301372.webp?size=4096&quality=lossless";
										break;
										case "prompt":
										r = "Answer.";
										break;
										case "more":
										r = "There will be.";
										break;
										case "na na na boo boo":
										r = "https://cdn.discordapp.com/attachments/1266047228338896946/1361146521487282429/caption-1-1.gif";
										break;
										case "yes":
										r = "I don't know what you're saying that to.";
										break;
										case "breeland":
										r = "Cat";
										break;
										case "conan":
										r = "Dog";
										break;
										case "tuesday":
										r = "Dog";
										break;
										case "again":
										r = "Literal";
										break;
										case "fourth brother":
										r = "No.";
										break;
										case "restart":
										r = "How?";
										break;
										case "aeiou":
										r = "aeiou";
										break;
										case "abcdefghijklmnopqrstuvwxyz":
										r = "Letter";
										break;
										case "donation":
										r = "10 doller";
										break;
										case "hunter":
										r = "https://media.discordapp.net/stickers/1488678125670895817.webp?size=4096&quality=lossless";
										break;
										case "meow":
										r = "Meow.";
										break;
										case "severe":
										r = "https://cdn.discordapp.com/emojis/956697172303290439.webp?size=4096&quality=lossless";
										break;
										case "ivy":
										r = "I kept adding ivy stuff when I was tired. |:";
										break;
										/*case "alphabet":
										r = "Don't try it.";
										break;*/
										case "0":
										r = "https://www.youtube.com/watch?v=a6A0quGPuAM";
										break;
									}

									response = InteractionResponse
									(
										r ?? "Invalid Code, please try again.", 
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>(), r == null
									);
									Console.WriteLine(response.Result);
								}
								else if(Timeout_CommandID == Cmd_ID)
								{
									string User = dataj["d"]["data"]["options"][0]["value"].GetValue<string>();
									int Days = dataj["d"]["data"]["options"][1]["value"].GetValue<int>();
									int Hours = dataj["d"]["data"]["options"][2]["value"].GetValue<int>();
									int Minutes = dataj["d"]["data"]["options"][3]["value"].GetValue<int>();
									int Seconds = dataj["d"]["data"]["options"][4]["value"].GetValue<int>();
									string Reason = dataj["d"]["data"]["options"][5]["value"].GetValue<string>();

									TimeSpan Length = new TimeSpan(Days, Hours, Minutes, Seconds);

									JsonContent json = JsonContent.Create(new
									{
										communication_disabled_until = (DateTime.UtcNow+Length).ToString("u"),
									});
									//json.Headers.Add("X-Audit-Log-Reason", $"{Reason} - <@{dataj["d"]["member"]["user"]["id"].GetValue<string>()}>");
									json.Headers.Add("X-Audit-Log-Reason", HttpUtility.UrlEncode($"{Reason} - {dataj["d"]["member"]["user"]["username"].GetValue<string>()}"));

									response = client.PatchAsync($"https://discord.com/api/v{API_Ver}/guilds/{dataj["d"]["guild_id"].GetValue<string>()}/members/{User}", json);

									if(response.Result.IsSuccessStatusCode)
										response = InteractionResponse
										(
											//$"Timed out <@{User}> for {Length:%d days, %h hours, %m minutes, %s seconds.}.", 
											$"Timed out <@{User}> for {Length}.\nReason: {Reason}", 
											dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
										);
									else
										response = InteractionResponse
										(
											//$"Timed out <@{User}> for {Length:%d days, %h hours, %m minutes, %s seconds.}.", 
											$"Could not time out <@{User}> for {Length}.\nReason: {Reason}\n\nError: {response.Result.ReasonPhrase} ({(int)response.Result.StatusCode})\n-# You can't usually time a user out for longer than 28 days.", 
											dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
										);
									Console.WriteLine(response.Result);
								}
								else if(SnowflakeToDate_CommandID == Cmd_ID)
								{
									if(!snowflake.TryParse(dataj["d"]["data"]["options"][0]["value"].GetValue<string>(), out snowflake Snowflake))
									{
										response = InteractionResponse
										(
											"<:Error:1496202946948497478> Failed to parse the snowflake.", 
											dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
										);
										break;
									}

									var Result = (Snowflake >> 22) + 1420070400000;
									response = InteractionResponse
									(
										$"<t:{Result/1000}:S>\n-# Unix Timestamp in MS: {Result}", 
										dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
									);
								}
							}
							catch(Exception e)
							{
								Console.ForegroundColor = ErrorColor;
								Console.WriteLine(e);
								Console.ResetColor();

								response = InteractionResponse
								(
									$"<:Error:1496202946948497478> An error occured.\n-# {e.ToString().Replace("\n", "\n-# ")}", 
									dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
								);
							}
							break;
						}
					}
					break;
					//Heartbeat ACK
					case 11:
					Console.WriteLine("Heartbeat Acknowledged.");
					break;
					//Invalid Session
					case 9:
					Console.WriteLine("Invalid Session.");
					goto Start;
					//Reconnect
					case 7:
					Console.WriteLine("Reconnecting now!");
					goto Start;

					default:
					Console.ForegroundColor = ErrorColor;
					Console.WriteLine("Unknown OpCode received!\nOP: {0}", dataj["op"].GetValue<int>());
					Console.ResetColor();
					break;
				}

				Thread.Yield();
			}
		}
	}
}
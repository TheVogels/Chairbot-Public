#define NobodyLolPinger

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
using System.Web;
using System.Runtime.CompilerServices;

#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Discord_Bot
{
	// A snowflake is just a 64-bit integer.
	using snowflake = System.UInt64;

	public static partial class Program
	{
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
		public static List<Commands.Command> CommandInfo = new();

		// Register the commands, retrieve the command data and delete unknown commands.
		static void RegisterCommands()
		{
			// I wish there was an easier way I knew to do this, but it works and isn't terrible.
			Console.WriteLine("Registering Commands...");

			int[] DefaultIntergration = new int[] {0,1}, 
				DefaultContexts = new int[] {0,2}, 
				Zero = new int[] {0};

			Console.WriteLine("Created JSON");
			Console.ForegroundColor = ConsoleColor.DarkGreen;

			JsonArray CommandsEdit = new JsonArray
			{
				new 
				{
					name = "ping",
					type = 1,
					integration_types = DefaultIntergration,
					description = "Check if the bot is online.",
					contexts = DefaultContexts,
				},
				new 
				{
					name = "stats",
					type = 1,
					integration_types = DefaultIntergration,
					description = "Get info about the instance.",
					contexts = DefaultContexts,
				},
				new 
				{
					name = "timeoutlength",
					type = 1,
					integration_types = Zero,
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
					contexts = Zero,
					default_member_permissions = (1L << 40).ToString(),
				},
				new 
				{
					name = "setstatus",
					type = 1,
					integration_types = Zero,
					description = "Write to the bot's Status.txt file.",
					options = new[]
					{
						new
						{
							type = 3,
							name = "status",
							description = "The new status.",
							required = true,
						},
					},
					contexts = Zero,
					default_member_permissions = (1L << 51).ToString(),
				},
				new 
				{
					name = "clearstatus",
					type = 1,
					integration_types = Zero,
					description = "Clear the bot's Status.txt file.",
					contexts = Zero,
					default_member_permissions = (1L << 51).ToString(),
				},
				new 
				{
					name = "lockstatus",
					type = 1,
					integration_types = Zero,
					description = "Set if non-Super MODS can change the status message.",
					options = new[]
					{
						new
						{
							type = 5,
							name = "lock",
							description = "Whether to unlock or lock the status.",
							required = true,
						},
						new
						{
							type = 3,
							name = "expire",
							description = "The time in minutes or the date for the lock to expire.",
							required = false,
						},
					},
					contexts = Zero,
					default_member_permissions = (1L << 5).ToString(),
				},
				new 
				{
					name = "warn",
					type = 1,
					integration_types = Zero,
					description = "Warn a user.",
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
							name = "penaltypoints",
							description = "The penalty points. (Please keep this value small.)",
							required = true,
						},
						new
						{
							type = 3,
							name = "reason",
							description = "The reason for the warning.",
							required = true,
						},
					},
					contexts = Zero,
					default_member_permissions = (1L << 40).ToString(),
				},
				new 
				{
					name = "viewwarns",
					type = 1,
					integration_types = Zero,
					description = "View the warnings a user has.",
					options = new[]
					{
						new
						{
							type = 6,
							name = "user",
							description = "The user to check.",
							required = true,
						},
						new
						{
							type = 5,
							name = "forceold",
							description = "If the result should be in plain text.",
							required = false,
						}
					},
					contexts = Zero,
					default_member_permissions = (1L << 7).ToString(),
				},
				new 
				{
					name = "viewallwarns",
					type = 1,
					integration_types = Zero,
					description = "See the list of users who have warnings.",
					contexts = Zero,
					default_member_permissions = (1L << 7).ToString(),
				},
				new 
				{
					name = "viewmywarns",
					type = 1,
					integration_types = Zero,
					description = "View your own warnings.",
					contexts = Zero,
				},
				new 
				{
					name = "passreaction",
					type = 1,
					integration_types = Zero,
					description = "Add a reaction for 4 seconds.",
					options = new[]
					{
						new
						{
							type = 3,
							name = "link",
							description = "Link to the message.",
							required = true,
						},
						new
						{
							type = 3,
							name = "emoji_id",
							description = "Markdown of the animated emoji.",
							required = true,
						}
					},
					contexts = Zero,
				},
			};

			response = client.PutAsJsonAsync(Command_URL, CommandsEdit);
			response.Wait();

			WriteResultToConsole(response);

			var AllCommands = (JsonArray)client.GetJson(Command_URL);

			Console.WriteLine(CommandsEdit);
			
			Commands.Command.CommandCode Execute;
			string Name;
			foreach(var i in AllCommands)
			{
				Name = i["name"].GetValue<string>();
				Console.WriteLine(Name);
				switch(Name)
				{
					case "ping":
					Execute = Commands.Misc.Ping;
					break;
					case "stats":
					Execute = Commands.Misc.Stats;
					break;
					case "timeoutlength":
					Execute = Commands.Moderation.TimeoutLength;
					break;
					case "setstatus":
					Execute = Commands.Misc.Status.SetStatus;
					break;
					case "clearstatus":
					Execute = Commands.Misc.Status.ClearStatus;
					break;
					case "lockstatus":
					Execute = Commands.Misc.Status.LockStatus;
					break;
					case "warn":
					Execute = Commands.Moderation.Warn;
					break;
					case "viewwarns":
					Execute = Commands.Moderation.ViewWarns;
					break;
					case "viewallwarns":
					Execute = Commands.Moderation.ViewAllWarnings;
					break;
					case "viewmywarns":
					Execute = Commands.Moderation.ViewMyWarns;
					break;
					case "passreaction":
					Execute = Commands.Misc.PassReaction;
					break;

					default:
					// Delete unknown commands.
					client.DeleteAsync($"{Command_URL}/{i["id"].GetValue<string>()}");
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Deleted {0}.", i["name"].GetValue<string>());
					Console.ForegroundColor = ConsoleColor.DarkGreen;
					continue;
				}

				CommandInfo.Add(new Commands.Command
				{
					Code = Execute,
					data = i,
					Name = Name,
					ID = snowflake.Parse(i["id"].GetValue<string>()),
				});
			}

			CommandInfo.TrimExcess();
			ResetConsoleColor();
		}

		static bool GetIsApprovedForLock(snowflake User, bool IncludeTestUsers = true) => User switch
		{
			660663312584802334L => IncludeTestUsers,
			1185254272825311294L => true,
			_ => false,
		};

		static object ConstructMessageData(string Content, bool Hide = false, snowflake AllowMentions = 1, object[]? Embeds = null)
		{
			return new
			{
				content = Content,
				flags = Hide ? (1 << 6) : 0,
				allowed_mentions = AllowMentions == 1 ? null : new 
				{
					parse = AllowMentions != 1 ? Array.Empty<string>() : ["users"],
					users = AllowMentions == 0 ? null : new string[] {AllowMentions.ToString()},
				},
				embeds = Embeds,
			};
		}

		static Task<HttpResponseMessage> SendMessage(string Content, string Channel, bool Hide = false, snowflake AllowMentions = 1, object[]? Embeds = null)
		{
			return client.PostAsJsonAsync
			(
				$"https://discord.com/api/v{API_Ver}/channels/{Channel}/messages", 
				ConstructMessageData(Content, Hide, AllowMentions, Embeds)
			);
		}
		static Task<HttpResponseMessage> InteractionResponse(string Content, string ID, string Token, bool Hide = false, snowflake AllowMentions = 1, object[]? Embeds = null)
		{
			return client.PostAsJsonAsync
			(
				$"https://discord.com/api/v{API_Ver}/interactions/{ID}/{Token}/callback?with_response=0", 
				new 
				{
					type = 4,
					data = ConstructMessageData(Content, Hide, AllowMentions, Embeds)
				}
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool IsImportant(string User)
		{
			return User is "1185254272825311294" or "700796664276844612" or "1208464439238463561";
		}


		/// <summary>
		/// Color theming information.
		/// </summary>
		public const ConsoleColor DataColor = ConsoleColor.DarkCyan, ErrorColor = ConsoleColor.Red, NormalColor = ConsoleColor.Gray;


		static Task<HttpResponseMessage>? response;
		static bool SendHeartbeatNow = false;
		static Thread? Heartbeats = null;

		public static void Main()
		{
			// I don't want to lose some important logs.
			if(OperatingSystem.IsWindows())
				Console.BufferHeight = Math.Max(Math.Max(Console.WindowHeight, Console.BufferHeight), 1600);
		//Start:

			Console.WriteLine("Retrieving gateway.");
			var gatewayr = client.GetAsync($"https://discord.com/api/v{API_Ver}/gateway");

			gatewayr.Wait();

			var gateway = gatewayr.Result.Content.ReadAsStringAsync().Result;

			var dataj = JsonObject.Parse(gateway);
			Console.ForegroundColor = DataColor;
			Console.WriteLine(gateway);
			ResetConsoleColor();
			gateway = $"{dataj["url"].GetValue<string>()}/?v={API_Ver}&encoding=json";
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
						var Dataj = new JsonObject
						{
							["op"] = 1
						};

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
			dataj["d"]["intents"] = GUILD_MODERATION | MESSAGE_CONTENT | GUILD_MESSAGES;
			dataj["d"]["compress"] = false;
			dataj["d"]["large_threshold"] = 50;
			
			dataj["d"]["properties"] = new JsonObject();
			dataj["d"]["properties"]["os"] = Environment.OSVersion.ToString();
			dataj["d"]["properties"]["browser"] = "Chairbot";
			dataj["d"]["properties"]["device"] = ".NET Framework";

			var presence = new JsonObject();
			presence["since"] = null;
			var BotStatus = new JsonObject();
			presence["activities"] = new JsonArray(BotStatus);

			/*if(File.Exists(Environment.CurrentDirectory+"\\StreamUrl.txt"))
			{
				var StreamStatus = new JsonObject();

				StreamStatus["name"] = "YouTube";
				StreamStatus["details"] = "Chair";
				StreamStatus["state"] = "Alex Smith";
				StreamStatus["url"] = File.ReadAllText(Environment.CurrentDirectory+"\\StreamUrl.txt");
				StreamStatus["type"] = 1;

				((JsonArray)presence["activities"]).Add(StreamStatus);
			}*/
			
			BotStatus["name"] = "Status";
			#if DEBUG
			BotStatus["state"] = "Debug";
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
						ResetConsoleColor();
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

			Console.WriteLine(dataj);

			// Convert the JSON data to UTF8 data and send it through the gateway to identify the bot.
			gate.SendAsync(Encoding.UTF8.GetBytes(dataj.ToString()), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

			RegisterCommands();

			Console.WriteLine("Begin main loop.");
			#if NobodyLolPinger
			uint NextNobodyLol = 0;
			#endif

			// I have it somewhere so if I get an unexpected disconnect then I restart the entire bot, I don't know why Discord randomly disconnects the bot from the gateway and it's annoying.
			while(true)
			{
				if(gate.CloseStatus.HasValue)
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
				ResetConsoleColor();

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
							case "MESSAGE_CREATE":
							{
								string content = dataj["d"]["content"].GetValue<string>(),
								channel = dataj["d"]["channel_id"].GetValue<string>();

								if(channel == "1530084807638388747")
								{
									var Messages = client.GetJson($"https://discord.com/api/v{API_Ver}/channels/1530084807638388747/messages?limit=2");
									//Probably improve this, allowing users to add words along with the number.
									if(!uint.TryParse(Messages[1]["content"].GetValue<string>(), out var Previous) || (uint.TryParse(content, out uint Counting) && Counting == Previous+1))
									{
										//SendMessage((Counting+1).ToString(), channel);
									}
									else
									{
										response = client.DeleteAsync($"https://discord.com/api/v{API_Ver}/channels/{channel}/messages/{dataj["d"]["id"]}");
										Console.WriteLine("DOES NOT FOLLOW: \"{0}\"", content);
									}
								}
								
								#if NobodyLolPinger
								if((uint)Environment.TickCount > NextNobodyLol && Random.Shared.Next(10) >= 8 && content.Contains("nobody", StringComparison.InvariantCultureIgnoreCase))
								{
									response = SendMessage("<@1434184285954375680>", channel); // Someone asked me to make it so when VogelBoys sees the text "nobody" it will ping NobodyLol. DON'T TURN THIS ON 😭
									NextNobodyLol = (uint)Environment.TickCount+(60*1000); //12u*1000*60*60*60
								}
								#endif

								string[] Bad = File.ReadAllLines(Environment.CurrentDirectory+"\\Bad.txt");
								for(int I = 0; I < Bad.Length; I+= 2)
								{
									if(content.Contains(Bad[I], StringComparison.InvariantCultureIgnoreCase))
									{
										response = SendMessage(Bad[I+1].Replace("{0}", $"<@{dataj["d"]["author"]["id"].GetValue<string>()}>"), channel);
									}
								}
							}
							break;
							case "INTERACTION_CREATE":
							try
							{
								JsonNode Data = dataj["d"];

								snowflake Cmd_ID = snowflake.Parse(Data["data"]["id"].GetValue<string>());
								Commands.Command Command;

								foreach(var i in CommandInfo)
								{
									if(i.ID == Cmd_ID)
									{
										Command = i;
										goto Found;
									}
								}
								InteractionResponse("The command was not found.", Data["id"].GetValue<string>(), Data["token"].GetValue<string>());
								break;
								Found:

								string ID = Data["id"].GetValue<string>(), Token = Data["token"].GetValue<string>(), 
									CommandRunner = Data["member"]["user"]["id"].GetValue<string>();
								JsonArray? Options = Data["data"]["options"] as JsonArray;

								Command.Code((Content, Hide, AllowMentions, Embeds) => InteractionResponse(Content, Data["id"].GetValue<string>(), Data["token"].GetValue<string>(), Hide, AllowMentions, Embeds), CommandRunner, Options, Data);

								Console.ForegroundColor = DataColor;
								Console.WriteLine(response.Result);
								Console.ResetColor();
							}
							catch(Exception e)
							{
								Console.ForegroundColor = ErrorColor;
								Console.WriteLine(e);
								ResetConsoleColor();

								response = InteractionResponse
								(
									$"🪑👎🏾 An error occured.\n-# {e.ToString().Replace("\n", "\n-# ")}", 
									dataj["d"]["id"].GetValue<string>(), dataj["d"]["token"].GetValue<string>()
								);
							}
							break;
							case "GUILD_AUDIT_LOG_ENTRY_CREATE":
							{
								string channel = "1502956749852770397";

								switch(dataj["d"]["guild_id"].GetValue<string>())
								{
									case "1482770008340824125":
									channel = "1502956749852770397";
									break;
									case "1436910502939725907":
									channel = "1436910504877490232";
									break;
									case "1492500577975013446":
									channel = "1492500579199488083";
									break;
								}

								switch(dataj["d"]["action_type"].GetValue<int>())
								{ 
									case 22: //Ban
									{
										//SendMessage($"<@{dataj["d"]["target_id"].GetValue<string>()}>", "1502956749852770397");

										var ImgByte = new ByteArrayContent(File.ReadAllBytes("C:\\Users\\Wyatt\\source\\repos\\VogelBoys Discord Bot\\Chairbot\\COLLECTION.jpg"));

										var JsonData = JsonContent.Create
										(
											new
											{
												tts = false,
												content = $"-# <@{dataj["d"]["target_id"].GetValue<string>()}>",
												enforce_nonce = false,
												flags = 0,
												attachments = new[] 
												{
													new 
													{
														id = 0,
														filename = "COLLECTION.jpg",
													},
												},
											}
										);

										ImgByte.Headers.Add("content-type", "image/jpeg");

										var Content = new MultipartFormDataContent("boundry")
										{
											{JsonData, "payload_json"},
											{ImgByte, "files[0]", "COLLECTION.jpg"},
										};

										response = client.PostAsync($"https://discord.com/api/v{API_Ver}/channels/{channel}/messages", Content);
										response.Wait();
									}
									break;
									/*case 23: //Unban
									SendMessage($"<@{dataj["d"]["target_id"].GetValue<string>()}>", "1502956749852770397");
									break;*/
								}
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
					ResetConsoleColor();
					break;
				}

				Thread.Yield();
			}
	Start:;
		}
	}
}
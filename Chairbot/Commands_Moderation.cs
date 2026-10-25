using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;

namespace Discord_Bot
{
	using snowflake = System.UInt64;

	public static partial class Program
	{
		public static partial class Commands
		{
			public static class Moderation
			{
				public static Task<System.Net.Http.HttpResponseMessage> TimeoutUser(string User, string Guild, DateTime EndTime, string? Reason = null)
				{
					JsonContent json = JsonContent.Create(new
					{
						communication_disabled_until = EndTime.ToString("u"),
					});

					if(!string.IsNullOrWhiteSpace(Reason))
						json.Headers.Add("X-Audit-Log-Reason", HttpUtility.UrlEncode(Reason).Replace('+', ' '));

					return client.PatchAsync($"https://discord.com/api/v{API_Ver}/guilds/{Guild}/members/{User}", json);
				}

				public static void TimeoutLength(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
				{
					string User = Options[0]["value"].GetValue<string>();
					int Days = Options[1]["value"].GetValue<int>();
					int Hours = Options[2]["value"].GetValue<int>();
					int Minutes = Options[3]["value"].GetValue<int>();
					int Seconds = Options[4]["value"].GetValue<int>();
					string Reason = Options[5]["value"].GetValue<string>();

					//Don't time out Jory >:(
					if(IsImportant(User) && User != CommandRunner)
					{
						if(CommandRunner == "660663312584802334")
						{
							response = ResponseHandler
							(
								"https://cdn.discordapp.com/emojis/396769429775581184.gif?size=4096&animated=true&name=Meet%20The%20Demoman"
							);
							return;
						}

						response = ResponseHandler
						(
							"<a:MarioDance:1505958917480648824>"
						);
						User = CommandRunner;
						Reason+= "    TRIED TO TIME OUT IMPORTANT USER ):<";
					}

					/*if(User == CommandRunner && User == "660663312584802334")
					{
						response = ResponseHandler
						(
							"STOP DOING THAT WYATT"
						);
						return;
					}*/

					TimeSpan Length = new TimeSpan(Days, Hours, Minutes, Seconds);

					response = TimeoutUser(User, data["guild_id"].GetValue<string>(), DateTime.UtcNow+Length, $"{Reason} - {data["member"]["user"]["username"].GetValue<string>()}");

					if(response.Result.IsSuccessStatusCode)
						response = ResponseHandler
						(
							//$"Timed out <@{User}> for {Length:%d days, %h hours, %m minutes, %s seconds.}.", 
							$"Timed out <@{User}> for {Length}.\nReason: {Reason}"
						);
					else
					{
						string ErrorReason = null;
						switch(response.Result.StatusCode)
						{
							case (System.Net.HttpStatusCode)400:
							ErrorReason = "\n-# You can't usually time a user out for longer than 28 days.";
							break;
							case (System.Net.HttpStatusCode)403:
							ErrorReason = "\n-# The bot doesn't have permission to timeout, or the user's highest rank is above the bot.";
							break;
							case (System.Net.HttpStatusCode)404:
							ErrorReason = "\n-# The user is not in the server.";
							break;
						}
						response = ResponseHandler
						(
							//$"Timed out <@{User}> for {Length:%d days, %h hours, %m minutes, %s seconds.}.", 
							$"Could not time out <@{User}> for {Length}.\nReason: {Reason}\n\nError: {response.Result.ReasonPhrase} ({(int)response.Result.StatusCode}){ErrorReason}"
						);
					}
				}
			

				public static void Warn(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
				{
					string User = Options[0]["value"].GetValue<string>();
					string Server = data["guild_id"].GetValue<string>();

					if(IsImportant(User) && Server == "1436910502939725907")
					{
						response = ResponseHandler
						(
							$"This user is protected from timeouts and warnings."
						);
						return;
					}

					byte Points;
					try
					{
						Points = (byte)Options[1]["value"].GetValue<int>();
							
						if(Points > 255)
							throw null;
					}
					catch
					{
						response = ResponseHandler
						(
							"The penalty value is out of range. Please put in a value between 0-255."
						);
						return;
					}
					string Reason = Options[2]["value"].GetValue<string>();

					var Data = $"{Environment.CurrentDirectory}\\Server\\{Server}\\UserInfo\\{User}";
					if(!Directory.Exists(Data))
						Directory.CreateDirectory(Data);
					Data+= "\\Warns";

					FileStream Stream = new FileStream(Data, FileMode.OpenOrCreate, FileAccess.ReadWrite);

					Stream.Position = Math.Max(Stream.Length, 1);
					
					// Write the warning in the log.

					// Person warning the user
					Stream.Write( BitConverter.GetBytes( snowflake.Parse( CommandRunner ) ) );
					// The penalty of this warning.
					Stream.WriteByte(Points);
					// Reason bytes
					var StringBytes = Encoding.UTF8.GetBytes(Reason);
					Stream.Write(BitConverter.GetBytes((ushort)StringBytes.Length));
					// Reason
					Stream.Write(StringBytes);
					// Timestamp
					Stream.Write(BitConverter.GetBytes(DateTimeToDiscordDateTime(DateTime.UtcNow)));
					// EX Length
					Stream.WriteByte(0);

					//Update the points.
					Stream.Position = 0;
					byte NewPoints = (byte)(Points+Stream.ReadByte());
					Stream.Position = 0;
					Stream.WriteByte(NewPoints);

					Stream.Dispose();

					response = ResponseHandler
					(
						$"Warned <@{User}>.\nReason: {Reason}\nPenalty: {Points}\nTotal Penalty Point{NumberCounterNameEnd(NewPoints)}: {NewPoints}"
					);

					
					TimeSpan Length;

					var Channel = data["channel_id"].GetValue<string>();

					if(Points == 0)
						return;
					else if(NewPoints >= 6)
					{
						Length = new TimeSpan(28, 0, 0, 0);
						SendMessage("Ban this user.", Channel);
					}
					else if(NewPoints >= 5)
						Length = new TimeSpan(7, 0, 0, 0);
					else if(NewPoints >= 4)
						Length = new TimeSpan(24, 0, 0);
					else if(NewPoints >= 3)
						Length = new TimeSpan(0, 60, 0);
					else
						return;

					var NextTimeout = DateTime.UtcNow+Length;
					if(client.GetJson($"https://discord.com/api/v{API_Ver}/guilds/{Server}/members/{User}").AsObject().TryGetPropertyValue("communication_disabled_until", out var TimeoutLeftJ))
					{
						var PreviousTimeout = TimeoutLeftJ?.GetValue<string?>();
						if(!string.IsNullOrWhiteSpace(PreviousTimeout))
						{
							if(PreviousTimeout != null && DateTime.Parse(PreviousTimeout) > NextTimeout)
							{
								return;
							}
						}
					}

					TimeoutUser(User, Server, NextTimeout, $"Automatic action taken for penalty points. {NewPoints} point{NumberCounterNameEnd(NewPoints)}, warn reason: \"{Reason}\" - {data["member"]["user"]["username"].GetValue<string>()}");

					if(response.Result.IsSuccessStatusCode)
					{
						SendMessage($"Automatically timed <@{User}> out for {Length}.", Channel);
						
						//if(
					}
				}
			
				public static void ViewWarns(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
				{
					string User = Options[0]["value"].GetValue<string>();

					var Server = data["guild_id"].GetValue<string>();
					var Data = $"{Environment.CurrentDirectory}\\Server\\{Server}\\UserInfo\\{User}\\Warns";
					if(!File.Exists(Data))
					{
						ResponseHandler
						(
							"No warning data exists for this user in this server."
						);
						return;
					}

					FileStream Stream = new FileStream(Data, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024, FileOptions.SequentialScan);

					var TotalPenaltyPoints = Stream.ReadByte();
					string Text = $"<@{User}> has {TotalPenaltyPoints} penalty point{NumberCounterNameEnd(TotalPenaltyPoints)}.";
					string NewText = Text;

					List<object>? Embeds = Options.Count <= 1 || !Options[1]["value"].GetValue<bool>() ? new(4) : null;

					byte[] bytes = new byte[sizeof(ulong)];
					byte ExBytes;
					for(int i = 1; Stream.Position != Stream.Length; i++)
					{
						snowflake Warner;
						byte Penalty;
						string reason;

						Stream.ReadExactly(bytes, 0, sizeof(snowflake));
						Text = $"{Text}\n\n-Warning {i} from <@{Warner = BitConverter.ToUInt64(bytes)}>-\nPenalty: {Penalty = (byte)Stream.ReadByte()}\nReason: ";
						Stream.ReadExactly(bytes, 0, sizeof(ushort));

						byte[] Reason = new byte[BitConverter.ToUInt16(bytes)];
						Stream.ReadExactly(Reason, 0, Reason.Length);
						Text+= reason = Encoding.UTF8.GetString(Reason, 0, Reason.Length);

						Stream.ReadExactly(bytes, 0, sizeof(ulong));
						var Date = BitConverter.ToUInt64(bytes);
						Text+= $"\n<t:{Date}:S>, <t:{Date}:R>";

						ExBytes = (byte)Stream.ReadByte();
						Stream.Position+= ExBytes;

						/*Embeds?.Add(new
						{
							title = $"Warning {i}",
							description = reason,
							color = 13959168,
							timestamp = DiscordDateTimeToDateTime(Date).ToString("u"),

							fields = new object[]
							{
								new 
								{
									name = "Penalty",
									value = Penalty.ToString(),
								},
								new 
								{
									name = "Warner",
									value = $"<@{Warner}>",
								}
							}
						});*/
						JsonNode? WarnerName, WarnerAvatar, WarnedName, WarnedAvatar;

						try
						{
							if(Warner != 0 && Embeds != null && i <= 10)
							{
								var WarnerDataServer = client.GetJson($"https://discord.com/api/v{API_Ver}/guilds/{Server}/members/{Warner}").AsObject();
								var WarnerData = WarnerDataServer["user"];

								if(!WarnerDataServer.TryGetPropertyValue("nick", out WarnerName) || WarnerName == null)
									WarnerName = WarnerData["global_name"];

								if(!WarnerDataServer.TryGetPropertyValue("avatar", out WarnerAvatar) || WarnerAvatar == null)
									WarnerAvatar = $"https://cdn.discordapp.com/avatars/{Warner}/{WarnerData["avatar"].GetValue<string>()}.png";
								else
									WarnerAvatar = $"https://cdn.discordapp.com/guilds/{Server}/users/{Warner}/avatars/{WarnerAvatar.GetValue<string>()}.png";
							}
							else
							{
								WarnerAvatar = WarnerName = null;
							}
						}
						catch(AggregateException)
						{
							WarnerAvatar = WarnerName = null;
						}

						if(i == 1)
						{
							try
							{
								var WarnedDataServer = client.GetJson($"https://discord.com/api/v{API_Ver}/guilds/{Server}/members/{User}").AsObject();
								var WarnedData = WarnedDataServer["user"];

								if(!WarnedDataServer.TryGetPropertyValue("nick", out WarnedName) || WarnedName == null)
									WarnedName = WarnedData["global_name"];

								if(!WarnedDataServer.TryGetPropertyValue("avatar", out WarnedAvatar) || WarnedAvatar == null)
									WarnedAvatar = $"https://cdn.discordapp.com/avatars/{User}/{WarnedData["avatar"].GetValue<string>()}.png";
								else
									WarnedAvatar = $"https://cdn.discordapp.com/guilds/{Server}/users/{User}/avatars/{WarnedAvatar.GetValue<string>()}.png";
							}
							catch
							{
								WarnedAvatar = WarnedName = null;
							}
						}
						else
							WarnedAvatar = WarnedName = null;

						Embeds?.Add(new
						{
							title = $"Warning {i}",
							description = Warner == 0 ? null : $"From <@{Warner}>",
							color = 13959168,
							timestamp = DiscordDateTimeToDateTime(Date).ToString("u"),

							fields = new object[]
							{
								new 
								{
									name = "Reason",
									value = reason,
								},
								new 
								{
									name = "Penalty",
									value = Penalty.ToString(),
								},
							},
							footer = new
							{
								text = WarnerName,
								icon_url = WarnerAvatar,
							},
							author = new
							{
								name = WarnedName,
								icon_url = WarnedAvatar,
							},
						});
					}
					Stream.Dispose();

					if(Embeds != null && Embeds.Count > 10)
						Embeds = null;
					else
						for(int i = 0; i < Embeds.Count; i++)
						{
							var NEW = JsonSerializer.SerializeToNode(Embeds[i]);
							NEW["color"] = (235-(178*i/(Embeds.Count-1))) << 16;
							Embeds[i] = NEW;
						}

					response = ResponseHandler
					(
						Embeds == null ? Text : NewText, 
						AllowMentions: snowflake.Parse(User), Embeds: Embeds?.ToArray()
					);
				}
				
				public static void ViewAllWarnings(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
				{
					var Data = $"{Environment.CurrentDirectory}\\Server\\{data["guild_id"].GetValue<string>()}\\UserInfo";
					
					string? Str = null;
					if(Directory.Exists(Data))
					{
						FileStream file;
						foreach(var i in Directory.GetDirectories(Data))
						{
							file = new FileStream(i+"\\Warns", FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.SequentialScan);
							var TotalPenaltyPoints = file.ReadByte();
							Str = $"{Str}<@{i.Substring(i.LastIndexOf('\\')+1)}>, {TotalPenaltyPoints} penalty point{NumberCounterNameEnd(TotalPenaltyPoints)}.\n";
							file.Dispose();
						}
					}
					Str??= "There are no warnings in this server.";

					response = ResponseHandler
					(
						Str, AllowMentions: 0
					);
				}

				public static void ViewMyWarns(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
				{
					ViewWarns((Content, Hide, AllowMentions, Embeds) => ResponseHandler(Content, true, AllowMentions, Embeds), CommandRunner, new JsonArray
					{
						JsonSerializer.SerializeToNode(new
						{
							value = CommandRunner,
						})
					}, data);
				}
			}
		}
	}
}

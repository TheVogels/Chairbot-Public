using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
			public struct Command
			{
				public snowflake ID;
				public string Name;
				public CommandCode Code;


				public JsonNode? data;

				public delegate void CommandCode(ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data);
				public delegate Task<System.Net.Http.HttpResponseMessage> ResponseHandler(string Content, bool Hide = false, snowflake AllowMentions = 1, object[]? Embeds = null);
			}

			public static class Misc
			{
				public static void Ping(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
				{
					response = ResponseHandler("🪑👍🏾");
				}
	
				public static void Stats(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
				{
					if(!OperatingSystem.IsWindows())
					{
						response = ResponseHandler
						(
							"The bot isn't running on Windows, so this command isn't available.", 
							Hide: true
						);
					}

					[System.Runtime.InteropServices.DllImport("ucrtbase.dll")]
					static extern Int32 clock();
	
					response = ResponseHandler
					(
						//This looks ugly.
$@"OS: {Environment.OSVersion} (x{(Environment.Is64BitOperatingSystem ? "64" : "86")})
Runtime: Microsoft .NET Core {Environment.Version} {BuildMode} (x{(Environment.Is64BitProcess ? "64" : "86")})
Uptime: {new TimeSpan(clock()*TimeSpan.TicksPerMillisecond):g}
Server uptime: {new TimeSpan(Environment.TickCount*TimeSpan.TicksPerMillisecond):g}
Discord API version {API_Ver}", 
	
						Hide: true
					);
				}

				public static void PassReaction(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
				{
					string message = Options[0]["value"].GetValue<string>();
					string reaction = Options[1]["value"].GetValue<string>();
					if(reaction.StartsWith('<'))
						reaction = reaction.Substring(1, reaction.Length-2);

					var slash = message.LastIndexOf('/');
					var slash2 = message.LastIndexOf('/', slash-1)+1;
					message = $"https://discord.com/api/v{API_Ver}/channels/{message.Substring(slash2, slash-slash2)}/messages/{message.Substring(slash+1)}/reactions/{HttpUtility.UrlEncode(reaction)}";
					
					var react = client.GetJson($"{message}?type=0&after={snowflake.Parse(APP_ID)-1}&limit=1").AsArray();
					Console.WriteLine(react);

					if(react.Count != 0 && react[0]["id"].GetValue<string>() == APP_ID)
					{
						response = ResponseHandler("Uhm, it seems that I already reacted with that!", true);
						return;
					}

					message+= "/@me";

					WriteResultToConsole(client.PutAsync(message, null));
					new System.Threading.Thread(() =>
					{
						System.Threading.Thread.Sleep(4000);
						client.DeleteAsync(message);
					}).Start();

					response = ResponseHandler("Added reaction. Expires in 4 seconds.", true);
				}

				public static class Status
				{
					public static (bool IsLocked, DateTime? UnlockTime) IsStatusLocked()
					{
						if(File.Exists(Environment.CurrentDirectory+"\\StatusLock.bit"))
						{
							var dat = File.ReadAllBytes(Environment.CurrentDirectory+"\\StatusLock.bit");
							if(Convert.ToBoolean(dat[0]))
							{
								if(dat.Length > 1)
								{
									var EndTime = BitConverter.ToInt64(dat, 1);
									if(DateTime.UtcNow.Ticks > EndTime)
										return (false, null);
									else
									{
										return (true, new DateTime(EndTime));
									}
								}

								return (true, null);
							}
						}
						return (false, null);
					}

					static bool LockHandler(Command.ResponseHandler ResponseHandler, string CommandRunner, bool IncludeTestUsers = false)
					{
						bool Approved = GetIsApprovedForLock(snowflake.Parse(CommandRunner), IncludeTestUsers);
						
						if(!Approved)
						{
							var LockStatus = IsStatusLocked();
							if(LockStatus.IsLocked)
							{
								response = ResponseHandler
								(
									LockStatus.UnlockTime.HasValue ? 
									$"The status is currently locked. The status will unlock <t:{DateTimeToDiscordDateTime(LockStatus.UnlockTime.Value)}:R>" 
									:
									"The status is currently locked."
								);
								return true;
							}
						}

						return false;
					}

					public static void SetStatus(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
					{
						if(LockHandler(ResponseHandler, CommandRunner))
							return;

						string status = Options[0]["value"].GetValue<string>();

						File.WriteAllText(Environment.CurrentDirectory+"\\Status.txt", status);

						response = ResponseHandler
						(
							$"Set the status to \"{status}\""
						);
					}

					public static void ClearStatus(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
					{
						if(LockHandler(ResponseHandler, CommandRunner))
							return;

						new FileStream(Environment.CurrentDirectory+"\\Status.txt", FileMode.Create, FileAccess.Write, FileShare.Read).Dispose();

						response = ResponseHandler
						(
							"Cleared the status."
						);
					}

					public static void LockStatus(Command.ResponseHandler ResponseHandler, string CommandRunner, JsonArray? Options, JsonNode data)
					{
						var User = snowflake.Parse(CommandRunner);
						bool Approved = GetIsApprovedForLock(User);
						if(!Approved && data["channel"]["guild_id"].GetValue<string>() != "1436910502939725907")
						{
							response = ResponseHandler
							(
								"You are not approved, or you are running this in Chaircord and do not have SUPER mod."
							);
							return;
						}

						bool Lock = Options[0]["value"].GetValue<bool>();
						var Time = Options.Count > 1 ? Options[1]["value"].GetValue<string>() : null;

						byte[] LockBytes = new byte[Time == null ? 1 : 1+sizeof(long)];
						LockBytes[0] = Convert.ToByte(Lock);
						int Minutes = -1;
						if(Time != null)
						{
							if(int.TryParse(Time, out Minutes))
								Array.Copy(BitConverter.GetBytes(DateTime.UtcNow.Ticks+(Minutes*TimeSpan.TicksPerMinute)), 0, LockBytes, 1, sizeof(long));
							else
							{
								Array.Copy(BitConverter.GetBytes(DateTime.Parse(Time).Ticks), 0, LockBytes, 1, sizeof(long));
								Minutes = -1;
							}
						}

						File.WriteAllBytes(Environment.CurrentDirectory+"\\StatusLock.bit", LockBytes);

						response = ResponseHandler
						(
							$"{(Lock ? "Locked the status" : "Unlocked the status")}{(Time == null ? string.Empty : (Minutes == -1 ? $" until <t:{DateTimeToDiscordDateTime(DateTime.Parse(Time))}:S>" : $" for {Minutes} minutes"))}."
						);
					}
				}
			}
		}
	}
}

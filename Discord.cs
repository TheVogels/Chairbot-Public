namespace Discord_Bot
{
	public static partial class Program
	{
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
	}
}

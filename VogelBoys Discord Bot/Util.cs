using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Discord_Bot
{
	using static Discord_Bot.Program;

	public static class Util
	{
		/*[DefaultValue(Normal)]
		public enum LogType : byte
		{
			Normal,
			Data,
			Error,
		}

		/// <inheritdoc cref="Console.WriteLine(string?)"/>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Log(string? value, LogType log = LogType.Normal)
		{
			if(log != LogType.Normal)
				Console.ForegroundColor = log switch
				{
					LogType.Data => DataColor,
					LogType.Error => ErrorColor,
				};

			Console.WriteLine();

			Console.ResetColor();
		}*/

		/// <summary>
		/// Finds and retreives the node in this object that contains the key with the same value and name.
		/// </summary>
		/// <param name="data">this</param>
		/// <param name="SubKey"></param>
		/// <returns>The node, if it was found. If not found then this will return null.</returns>
		public static JsonNode? Find(this JsonNode? data, string SubKey, string? Value)
		{
			if(data is JsonArray array)
			{
				foreach(var i in array)
				{
					if(i is JsonObject obj && obj.TryGetPropertyValue(SubKey, out JsonNode val) && val.GetValue<string?>() == Value)
					{
						return obj;
					}
				}
			}

			return null;
		}
	}
}

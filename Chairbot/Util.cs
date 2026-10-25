using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;

namespace Discord_Bot
{
	using static Discord_Bot.Program;
	using System.Threading.Tasks;

	public static class Extensions
	{
		/// <inheritdoc cref="HttpClient.GetAsync(string?)"/>
		public static JsonNode? GetJson(this HttpClient client, string requestUri)
		{
			return JsonNode.Parse(client.GetStringAsync(requestUri).Result);
		}

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

		public static T[] ToArray<T>(this ITuple tuple)
		{
			T[] Array = new T[tuple.Length];
			for(int i = 0; i < Array.Length; i++)
				Array[i] = (T)tuple[i];

			return Array;
		}
	}

	public static partial class Program
	{
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ResetConsoleColor()
		{
			//Console.ResetColor();
			Console.ForegroundColor = NormalColor;
		}

		public static ulong DateTimeToDiscordDateTime(DateTime date)
		{
			//return (date-DateTime.UnixEpoch).Ticks/TimeSpan.TicksPerSecond;
			return (ulong)(date-DateTime.UnixEpoch).Ticks/(ulong)TimeSpan.TicksPerSecond;
		}

		public static DateTime DiscordDateTimeToDateTime(ulong date)
		{
			return new DateTime(((long)date*TimeSpan.TicksPerSecond)+DateTime.UnixEpoch.Ticks);
		}

		public static string? NumberCounterNameEnd(int number) => number == 1 ? null : "s";
	}
}

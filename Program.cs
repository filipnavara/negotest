using Dasync.Collections;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text;

internal sealed class HttpEventListener : EventListener
{
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // Allow internal HTTP logging
        if (eventSource.Name == "Private.InternalDiagnostics.System.Net.Http")
        {
            EnableEvents(eventSource, EventLevel.LogAlways);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // Log whatever other properties you want, this is just an example
        var sb = new StringBuilder().Append($"{eventData.TimeStamp:HH:mm:ss.fffffff}[{eventData.EventName}] ");
        for (int i = 0; i < eventData.Payload?.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(eventData.PayloadNames?[i]).Append(": ").Append(eventData.Payload[i]);
        }
        try {
            Console.WriteLine(sb.ToString());
        } catch { }
    }
}

namespace PCT20018NtlmTest
{
	internal class Program
	{
		static void Main(string[] args)
		{
			// Keep the listener around while you want the logging to continue, dispose it after.
			var listener = new HttpEventListener();
			DoSomething().Wait();
			GC.KeepAlive(listener);
		}

		static async Task DoSomething()
		{
			var baseUrl = "http://13.94.243.22/";
			var messageHandler = new HttpClientHandler
			{
				Credentials = new NetworkCredential("AzureUser", "mysuperstrongpwd.1", ".")
			};
			var httpClient = new HttpClient(messageHandler)
			{
				BaseAddress = new Uri(baseUrl),
			};

			var collection = new List<string>();
			for (var a = 0; a < 1000; a++)
			{
				collection.Add(a.ToString());
			}

			await collection.ParallelForEachAsync(async x =>
			{
				try
				{
					var result = await httpClient.GetAsync("/");
					result.EnsureSuccessStatusCode();
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}, maxDegreeOfParallelism: 20);
		}
	}
}

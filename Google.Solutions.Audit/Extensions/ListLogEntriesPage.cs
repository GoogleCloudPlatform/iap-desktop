using Google.Solutions.LogAnalysis.Events;
using Newtonsoft.Json;
using System;

namespace Google.Solutions.LogAnalysis.Extensions
{
    internal static class ListLogEntriesPage
    {
        internal static string Read(
            JsonReader reader,
            Action<EventBase> callback)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    // Read entire array, parsing and consuming 
                    // records one by one.
                    foreach (var record in EventFactory.Read(reader))
                    {
                        callback(record);
                    }
                }
                else if (reader.TokenType == JsonToken.String && reader.Path == "nextPageToken")
                {
                    return (string)reader.Value;
                }
            }

            return null;
        }
    }
}

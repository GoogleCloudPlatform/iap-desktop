using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LogAnalysis.Anonymizer
{
    class Program
    {
        private static string ExtractProjectIdFromLogName(string logName)
        {
            //
            // LogName has the format
            // projects/<project-ud>/logs/*'
            //
            var parts = logName.Split('/');
            if (parts.Length < 4)
            {
                throw new ArgumentException(
                    "Enountered unexpected LogName format: " + logName);
            }

            return parts[1];
        }

        private static void ScrubAndEmitEntry(TextReader reader, Formatting formatting)
        {
            using (var jsonReader = new JsonTextReader(reader))
            {
                var record = JObject.Load(jsonReader);

                var logName = record["logName"];
                if (logName == null)
                {
                    Console.WriteLine("Invalid format: entry does not have a logName");
                    Environment.Exit(1);
                }
                var projectId = ExtractProjectIdFromLogName(logName.Value<string>());

                // Scrub request metadata.
                if (record["protoPayload"] != null)
                {
                    record["protoPayload"]["requestMetadata"] = null;
                }

                // Scrub authenticationInfo.
                if (record["protoPayload"] != null)
                {
                    record["protoPayload"]["authenticationInfo"] = null;
                }

                Console.WriteLine(record
                    .ToString(formatting)
                    .Replace(projectId, "project-1"));
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: <program> <file> [/multidoc]");
                Environment.Exit(1);
            }

            bool multidoc = args.Length > 1 && args[1] == "/multidoc";
            var inputFile = args[0];

            using (var reader = inputFile == "-" ? Console.In : new StreamReader(File.OpenRead(inputFile)))
            {
                if (multidoc)
                {
                    while (true)
                    {
                        var line = reader.ReadLine();
                        if (line != null)
                        {
                            ScrubAndEmitEntry(new StringReader(line), Formatting.None);
                        }
                    }
                }
                else
                {
                    ScrubAndEmitEntry(reader, Formatting.Indented);
                }
            }

        }
    }
}

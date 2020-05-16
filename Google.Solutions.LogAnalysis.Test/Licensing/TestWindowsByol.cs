using Google.Solutions.LogAnalysis.Events;
using Google.Solutions.LogAnalysis.History;
using Google.Solutions.LogAnalysis.Logs;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.LogAnalysis.Test.Licensing
{
    [TestFixture]
    public class TestWindowsByol
    {
        [OneTimeSetUp]
        public void Init()
        {
            // TODO: download file
            var logFile = @"C:\dev\57-byol-tracking\ByolTracker\davecheng-sandbox2.json.gz";

            var builder = new InstanceSetHistoryBuilder();
            //builder.AddExistingInstance(
            //    3918016710321588795,
            //    new VmI)


            using (var reader = new StreamReader(
                new GZipStream(
                    File.OpenRead(logFile), 
                    CompressionLevel.Fastest)))
            {
                // Each line contains one log entry.
                while (!reader.EndOfStream)
                {
                    using (var recordReader = new JsonTextReader(new StringReader(reader.ReadLine())))
                    {
                        builder.Process(LogRecord.Deserialize(recordReader).ToEvent());
                    }
                }
            }

            builder.Build();
        }
    }
}

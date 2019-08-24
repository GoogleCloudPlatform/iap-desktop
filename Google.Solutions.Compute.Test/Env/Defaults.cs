using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Test.Env
{
    public static class Defaults
    {
        public static readonly string ProjectId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
        public static readonly string Zone = "us-central1-a";
    }
}

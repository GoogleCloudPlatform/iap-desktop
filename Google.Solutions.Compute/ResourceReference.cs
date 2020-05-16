using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Compute
{
    public abstract class ResourceReference
    {
        private const string ComputeGoogleapisPrefix = "https://compute.googleapis.com/compute/v1/";
        private const string GoogleapisUrlPrefix = "https://www.googleapis.com/compute/v1/";

        protected static string StripUrlPrefix(string resourceReference)
        {
            if (resourceReference.StartsWith(ComputeGoogleapisPrefix))
            {
                return resourceReference.Substring(ComputeGoogleapisPrefix.Length);
            }
            else if (resourceReference.StartsWith(GoogleapisUrlPrefix))
            {
                return resourceReference.Substring(GoogleapisUrlPrefix.Length);
            }
            else
            {
                return resourceReference;
            }
        }
    }
}

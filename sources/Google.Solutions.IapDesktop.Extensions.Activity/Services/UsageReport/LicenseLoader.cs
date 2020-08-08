//
// Copyright 2019 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.UsageReport
{
    internal static class LicenseLoader
    {
        private static LicenseLocator TryGetRelevantLicenseFromImage(Image imageInfo)
        {
            var locators = imageInfo.Licenses
                .EnsureNotNull()
                .Select(license => LicenseLocator.FromString(license));

            // Images can contain more than one license, and liceses like 
            // "/compute/v1/projects/compute-image-tools/global/licenses/virtual-disk-import"
            // are not helpful here. So do some filtering.
            if (locators.FirstOrDefault(l => l.IsWindowsByolLicense()) is LicenseLocator byolLocator)
            {
                return byolLocator;
            }
            else if (locators.FirstOrDefault(l => l.IsWindowsLicense()) is LicenseLocator winLocator)
            {
                return winLocator;
            }
            else
            {
                return locators.FirstOrDefault();
            }
        }

        public static async Task LoadLicenseAnnotationsAsync(
            ReportArchive annotatedSet,
            IComputeEngineAdapter computeEngineAdapter,
            CancellationToken cancellationToken)
        {
            foreach (var image in annotatedSet.History.Instances
                    .Where(i => i.Image != null)
                    .Select(i => i.Image)
                    .Distinct())
            {
                try
                {
                    Image imageInfo = await computeEngineAdapter
                        .GetImageAsync(image, cancellationToken)
                        .ConfigureAwait(false);

                    // Images can contain more than one license, and liceses like 
                    // "/compute/v1/projects/compute-image-tools/global/licenses/virtual-disk-import"
                    // are not helpful here. So do some filtering.

                    var license = TryGetRelevantLicenseFromImage(imageInfo);
                    annotatedSet.AddLicenseAnnotation(
                        image,
                        license);

                    TraceSources.IapDesktop.TraceVerbose("License for {0} is {1}", image, license);
                }
                catch (ResourceNotFoundException) when (image.ProjectId == "windows-cloud")
                {
                    // That image might not exist anymore, but we know it's
                    // a Windows SPLA image.
                    annotatedSet.AddLicenseAnnotation(
                        image,
                        OperatingSystemTypes.Windows,
                        LicenseTypes.Spla);

                    TraceSources.IapDesktop.TraceVerbose(
                        "License for {0} could not be found, but must be Windows/SPLA", image);
                }
                catch (ResourceNotFoundException e)
                {
                    // Unknown or inaccessible image, skip.
                    TraceSources.IapDesktop.TraceWarning(
                        "License for {0} could not be found: {0}", image, e);
                }
                catch (ResourceAccessDeniedException e)
                {
                    // Unknown or inaccessible image, skip.
                    TraceSources.IapDesktop.TraceWarning(
                        "License for {0} could not be accessed: {0}", image, e);
                }
            }
        }
    }
}

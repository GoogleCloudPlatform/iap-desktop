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

using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Util;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.Services.SchedulingReport
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
            ImagesResource imagesResource)
        {
            foreach (var image in annotatedSet.History.Instances
                    .Where(i => i.Image != null)
                    .Select(i => i.Image)
                    .Distinct())
            {
                try
                {
                    Image imageInfo;
                    if (image.Name.StartsWith("family/"))
                    {
                        imageInfo = await imagesResource
                            .GetFromFamily(image.ProjectId, image.Name.Substring(7))
                            .ExecuteAsync();
                    }
                    else
                    {
                        imageInfo = await imagesResource
                            .Get(image.ProjectId, image.Name)
                            .ExecuteAsync();
                    }

                    // Images can contain more than one license, and liceses like 
                    // "/compute/v1/projects/compute-image-tools/global/licenses/virtual-disk-import"
                    // are not helpful here. So do some filtering.

                    annotatedSet.AddLicenseAnnotation(
                        image,
                        TryGetRelevantLicenseFromImage(imageInfo));
                }
                catch (Exception e) when (
                    e.Unwrap() is GoogleApiException apiEx &&
                    apiEx.Error != null &&
                    apiEx.Error.Code == 404 &&
                    image.ProjectId == "windows-cloud")
                {
                    // That image might not exist anymore, but we know it's
                    // a Windows SPLA image.
                    annotatedSet.AddLicenseAnnotation(
                        image,
                        OperatingSystemTypes.Windows,
                        LicenseTypes.Spla);
                }
                catch (Exception e) when (
                    e.Unwrap() is GoogleApiException apiEx &&
                    apiEx.Error != null &&
                    (apiEx.Error.Code == 404 || apiEx.Error.Code == 403))
                {
                    // Unknown or inaccessible image, skip.
                }
            }
        }
    }
}

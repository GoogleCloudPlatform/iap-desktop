﻿//
// Copyright 2023 Google LLC
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
using Google.Solutions.IapDesktop.Core.ClientModel.Traits;
using Moq;
using NUnit.Framework;

namespace Google.Solutions.IapDesktop.Core.Test.ClientModel.Traits
{
    [TestFixture]
    public class TestTraitDetector
    {
        private const string SampleWindowsLicenseString
            = "ttps://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2003";

        //---------------------------------------------------------------------
        // Default traits.
        //---------------------------------------------------------------------

        [Test]
        public void WhenInstanceIsWindowsByGuestOsFeature_ThenDetectTraitsReturnsTraits()
        {
            var instance = new Instance()
            {
                Disks = new[]
                {
                    new AttachedDisk()
                    {
                        GuestOsFeatures = new []
                        {
                            new GuestOsFeature()
                            {
                                Type = "WINDOWS"
                            }
                        }
                    }
                }
            };

            var traits = TraitDetector.DetectTraits(instance);
            CollectionAssert.Contains(traits, InstanceTrait.Instance);
            CollectionAssert.Contains(traits, WindowsTrait.Instance);
            CollectionAssert.DoesNotContain(traits, LinuxTrait.Instance);
        }

        [Test]
        public void WhenInstanceIsWindowsByLicense_ThenDetectTraitsReturnsTraits()
        {
            var instance = new Instance()
            {
                Disks = new[]
                {
                    new AttachedDisk()
                    {
                        Licenses = new []
                        {
                            SampleWindowsLicenseString
                        }
                    }
                }
            };

            var traits = TraitDetector.DetectTraits(instance);
            CollectionAssert.Contains(traits, InstanceTrait.Instance);
            CollectionAssert.Contains(traits, WindowsTrait.Instance);
            CollectionAssert.DoesNotContain(traits, LinuxTrait.Instance);
        }

        [Test]
        public void WhenInstanceIsLinux_ThenDetectTraitsReturnsTraits()
        {
            var instance = new Instance();

            var traits = TraitDetector.DetectTraits(instance);
            CollectionAssert.Contains(traits, InstanceTrait.Instance);
            CollectionAssert.DoesNotContain(traits, WindowsTrait.Instance);
            CollectionAssert.Contains(traits, LinuxTrait.Instance);
        }

        //---------------------------------------------------------------------
        // Custom detector.
        //---------------------------------------------------------------------

        [Test]
        public void WhenCustomDetectorRegistered_ThenDetectTraitsReturnsCustomTraits()
        {
            var customTrait = new Mock<ITrait>();
            var detector = new Mock<ITraitDetector>();
            detector
                .Setup(d => d.DetectTraits(It.IsAny<Instance>()))
                .Returns(new[] { customTrait.Object });

            TraitDetector.RegisterCustomDetector(detector.Object);

            var instance = new Instance();
            var traits = TraitDetector.DetectTraits(instance);
            CollectionAssert.Contains(traits, customTrait.Object);
        }
    }
}

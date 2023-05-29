//
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
using Google.Solutions.Apis.Compute;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Traits
{
    /// <summary>
    /// Detector for custom traits.
    /// </summary>
    public interface ITraitDetector
    {
        /// <summary>
        /// Detect and list traits of an instance. Return an empty collection
        /// if no traits were found.
        /// </summary>
        IEnumerable<ITrait> DetectTraits(Instance instance);
    }

    public static class TraitDetector
    {
        private static readonly ConcurrentBag<ITraitDetector> finders 
            = new ConcurrentBag<ITraitDetector>();


        static TraitDetector()
        {
            RegisterCustomDetector(new DefaultDetector());
        }

        public static void RegisterCustomDetector(ITraitDetector finder)
        {
            finder.ExpectNotNull(nameof(finder));
            finders.Add(finder);
        }

        public static IReadOnlyCollection<ITrait> DetectTraits(
            Instance instance)
        {
            return finders
                .SelectMany(f => f.DetectTraits(instance))
                .ToList();
        }

        //---------------------------------------------------------------------
        // Defaults.
        //---------------------------------------------------------------------

        private class DefaultDetector : ITraitDetector
        {
            public IEnumerable<ITrait> DetectTraits(Instance instance)
            {
                yield return InstanceTrait.Instance;
                yield return instance.IsWindowsInstance()
                    ? (ITrait)WindowsTrait.Instance
                    : (ITrait)LinuxTrait.Instance;
            }
        }
    }
}

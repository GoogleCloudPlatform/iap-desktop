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

using System;
using System.Collections.Generic;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    /// <summary>
    /// A target that you can connect a transport to.
    /// </summary>
    public interface IProtocolTarget
    {
        /// <summary>
        /// Display name of the target.
        /// </summary>
        string TargetName { get; }

        /// <summary>
        /// Traits of this target that can be used to determine
        /// applicable protocols.
        /// </summary>
        IEnumerable<IProtocolTargetTrait> Traits { get; }
    }

    /// <summary>
    /// Represents some trait of the target.
    /// </summary>
    public interface IProtocolTargetTrait : IEquatable<IProtocolTargetTrait>
    {
        /// <summary>
        /// Description, suitable for displaying.
        /// </summary>
        string DisplayName { get; }
    }
}

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

using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    /// <summary>
    /// A locator that idenfifies a target that can be used with
    /// a certain protocol and parameters.
    /// </summary>
    public abstract class ProtocolTargetLocator : IEquatable<ProtocolTargetLocator>
    {
        /// <summary>
        /// Protocol used by this locator.
        /// </summary>
        public abstract IProtocol Protocol { get; }

        /// <summary>
        /// Protocol scheme used by this locator.
        /// </summary>
        public abstract string Scheme { get; }

        /// <summary>
        /// Base resource referenced by this locator.
        /// </summary>
        public ResourceLocator Resource { get; }

        /// <summary>
        /// Query string parameters that might contain connection settings.
        /// 
        /// NB. The NameValueCollection is case-insensitive.
        /// </summary>
        public NameValueCollection Parameters { get; }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        protected ProtocolTargetLocator(
            ResourceLocator resource,
            NameValueCollection parameters)
        {
            this.Resource = resource.ExpectNotNull(nameof(resource));
            this.Parameters = parameters.ExpectNotNull(nameof(parameters));
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        private static bool ParametersEqual(
            NameValueCollection that,
            NameValueCollection other)
        {
            if (that is null)
            {
                return other is null;
            }

            return other != null &&
                that.AllKeys.Length == other.AllKeys.Length &&
                that.AllKeys.All(k => that
                    .GetValues(k)
                    .EnsureNotNull()
                    .SequenceEqual(
                        other.GetValues(k).EnsureNotNull()));
        }

        public virtual bool Equals(ProtocolTargetLocator? other)
        {
            return other != null &&
                other is ProtocolTargetLocator locator &&
                Equals(locator.Scheme, this.Scheme) &&
                Equals(locator.Protocol, this.Protocol) &&
                Equals(locator.Resource, this.Resource) &&
                ParametersEqual(locator.Parameters, this.Parameters);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ProtocolTargetLocator);
        }

        public override int GetHashCode()
        {
            return
                this.Scheme.GetHashCode() ^
                this.Resource.GetHashCode() ^
                this.Parameters.Count;
        }

        public static bool operator ==(ProtocolTargetLocator? obj1, ProtocolTargetLocator? obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(ProtocolTargetLocator? obj1, ProtocolTargetLocator? obj2)
        {
            return !(obj1 == obj2);
        }
    }
}

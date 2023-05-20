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

using Google.Solutions.IapDesktop.Core.ClientModel.Protocol;
using System;
using System.Linq;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Traits
{
    public class InstanceTrait : IProtocolTargetTrait
    {
        private const string Expression = "isInstance()";

        public InstanceTrait()
        {
        }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        public override int GetHashCode()
        {
            return 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as InstanceTrait);
        }

        public bool Equals(IProtocolTargetTrait other)
        {
            return other is InstanceTrait && other != null;
        }

        public static bool operator ==(InstanceTrait obj1, InstanceTrait obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(InstanceTrait obj1, InstanceTrait obj2)
        {
            return !(obj1 == obj2);
        }

        //---------------------------------------------------------------------
        // Overrides.
        //---------------------------------------------------------------------

        public override string ToString()
        {
            return Expression;
        }

        //---------------------------------------------------------------------
        // Parse.
        //---------------------------------------------------------------------

        public static bool TryParse(string expression, out InstanceTrait trait)
        {
            if (expression != null &&
                Expression == new string(expression
                    .ToCharArray()
                    .Where(c => !char.IsWhiteSpace(c))
                    .ToArray()))
            {
                trait = new InstanceTrait();
                return true;
            }
            else
            {
                trait = null;
                return false;
            }
        }
    }
}

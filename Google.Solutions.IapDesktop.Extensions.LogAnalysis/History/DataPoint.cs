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

using System;

namespace Google.Solutions.IapDesktop.Extensions.LogAnalysis.History
{
    public struct DataPoint
    {
        public DateTime Timestamp;
        public int Value;

        public DataPoint(DateTime timestamp, int value)
        {
            this.Timestamp = timestamp;
            this.Value = value;
        }

        public override int GetHashCode()
        {
            return
                this.Timestamp.GetHashCode() ^
                this.Value;
        }

        public override string ToString()
        {
            return $"({this.Timestamp}, {this.Value})";
        }

        public bool Equals(DataPoint other)
        {
            return !object.ReferenceEquals(other, null) &&
                this.Timestamp == other.Timestamp &&
                this.Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DataPoint &&
                Equals((DataPoint)obj);
        }

        public static bool operator ==(DataPoint obj1, DataPoint obj2)
        {
            if (object.ReferenceEquals(obj1, null))
            {
                return object.ReferenceEquals(obj2, null);
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(DataPoint obj1, DataPoint obj2)
        {
            return !(obj1 == obj2);
        }
    }
}

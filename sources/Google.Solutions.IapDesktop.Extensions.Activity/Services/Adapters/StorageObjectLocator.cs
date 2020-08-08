﻿//
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

namespace Google.Solutions.IapDesktop.Extensions.Activity.Services.Adapters
{
    public class StorageObjectLocator
    {
        public string Bucket { get; }
        public string ObjectName { get; }

        public StorageObjectLocator(string bucket, string objectName)
        {
            this.Bucket = bucket;
            this.ObjectName = objectName;
        }

        public override int GetHashCode()
        {
            return
                this.Bucket.GetHashCode() ^
                this.ObjectName.GetHashCode();
        }

        public override string ToString()
        {
            return $"gs://{this.Bucket}/{this.ObjectName}";
        }

        public bool Equals(StorageObjectLocator other)
        {
            return other is object &&
                this.Bucket == other.Bucket &&
                this.ObjectName == other.ObjectName;
        }

        public override bool Equals(object obj)
        {
            return obj is StorageObjectLocator locator && Equals(locator);
        }

        public static bool operator ==(StorageObjectLocator obj1, StorageObjectLocator obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(StorageObjectLocator obj1, StorageObjectLocator obj2)
        {
            return !(obj1 == obj2);
        }
    }
}

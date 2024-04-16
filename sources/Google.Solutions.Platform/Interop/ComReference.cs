//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Util;
using System;
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.Interop
{
    public struct ComReference<T> : IDisposable
        where T : class
    {
        public T Object { get; }

        internal ComReference(T obj)
        {
            this.Object = obj.ExpectNotNull(nameof(obj));
        }

        public void Dispose()
        {
            Marshal.ReleaseComObject(this.Object);
        }
    }

    public static class ComReference
    {
        public static ComReference<TInterface> For<TInterface>(TInterface obj)
            where TInterface : class
        {
            return new ComReference<TInterface>(obj);
        }
    }
}

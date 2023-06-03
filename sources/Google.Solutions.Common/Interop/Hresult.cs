﻿//
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

namespace Google.Solutions.Common.Interop
{
    public enum HRESULT : int
    {
        S_OK = 0,
        S_FALSE = 1,
        E_UNEXPECTED = unchecked((int)0x8000ffff),
    }

    public static class HresultExtensions
    {
        public static bool Succeeded(this HRESULT hr)
        {
            return hr >= 0;
        }

        public static bool Failed(this HRESULT hr)
        {
            return hr < 0;
        }
    }
}

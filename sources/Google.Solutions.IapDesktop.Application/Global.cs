//
// Copyright 2020 Google LLC
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

using Google.Solutions.Common.Net;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Google.Solutions.IapDesktop.Application.Test")]

namespace Google.Solutions.IapDesktop.Application
{
    public static class Globals
    {
        public const string FriendlyName = "IAP Desktop - Identity-Aware Proxy for Remote Desktop and SSH";

        // TODO: delete
        public const string SettingsKeyPath = @"Software\Google\IapDesktop\1.0";

        // TODO: delete
        public const string PoliciesKeyPath = @"Software\Policies\Google\IapDesktop\1.0";

        public static UserAgent UserAgent { get; }

        public static Version Version { get; }

        static Globals()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version;
            UserAgent = new UserAgent("IAP-Desktop", Version);
        }

        public static bool IsTestCase => Assembly.GetEntryAssembly() == null;
    }
}
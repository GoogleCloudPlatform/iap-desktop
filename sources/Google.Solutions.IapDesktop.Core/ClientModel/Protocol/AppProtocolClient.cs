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

using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Core.ClientModel.Transport;
using System;
using System.Diagnostics;
using System.IO;

namespace Google.Solutions.IapDesktop.Core.ClientModel.Protocol
{
    /// <summary>
    /// A thick client application.
    /// </summary>
    public interface IAppProtocolClient : IEquatable<IAppProtocolClient>
    {
        /// <summary>
        /// Indicates if the application is available on this system.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Launch the application and let it the transport.
        /// </summary>
        Process Launch(ITransport endpoint);
    }


    public class AppProtocolClient : IAppProtocolClient
    {
        /// <summary>
        /// Path to the executable to be launched. 
        /// </summary>
        public string Executable { get; }

        /// <summary>
        /// Optional: Arguments to be passed. Arguments can contain the
        /// following placeholders:
        /// 
        ///   %port% - contains the local port to connect to
        ///   %host% - contain the locat IP address to connect to
        ///   
        /// </summary>
        public string Arguments { get; }

        internal protected AppProtocolClient(string executable, string arguments)
        {
            this.Executable = executable.ExpectNotEmpty(nameof(executable));
            this.Arguments = arguments;
        }

        public override string ToString()
        {
            return this.Executable +
                (this.Arguments == null ? string.Empty : " " + this.Arguments);
        }

        //-----------------------------------------------------------------
        // IApplication.
        //-----------------------------------------------------------------

        public bool IsAvailable
        {
            get => true;
        }

        public Process Launch(ITransport endpoint) 
        {
            return Process.Start(new ProcessStartInfo()
            {
                FileName = this.Executable,
                Arguments = this.Arguments,
                UseShellExecute = true
            });
        }

        //-----------------------------------------------------------------
        // Equality.
        //-----------------------------------------------------------------

        public bool Equals(IAppProtocolClient other)
        {
            return other is AppProtocolClient cmd &&
                Equals(cmd.Executable, this.Executable) &&
                Equals(cmd.Arguments, this.Arguments);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AppProtocolClient);
        }

        public override int GetHashCode()
        {
            return
                this.Executable.GetHashCode() ^
                (this.Arguments?.GetHashCode() ?? 0);
        }

        public static bool operator ==(AppProtocolClient obj1, AppProtocolClient obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(AppProtocolClient obj1, AppProtocolClient obj2)
        {
            return !(obj1 == obj2);
        }
    }
}

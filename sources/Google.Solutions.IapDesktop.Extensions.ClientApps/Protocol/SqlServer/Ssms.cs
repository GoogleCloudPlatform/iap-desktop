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
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Interop;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Google.Solutions.IapDesktop.Extensions.ClientApps.Protocol.SqlServer
{
    /// <summary>
    /// SQL Server Management Studio executable.
    /// </summary>
    internal sealed class Ssms
    {
        private const string SsmsFileExtension = ".ssmssln";

        public string ExecutablePath { get; }

        private Ssms(string executablePath)
        {
            this.ExecutablePath = executablePath;
        }

        //---------------------------------------------------------------------
        // Publics.
        //---------------------------------------------------------------------

        /// <summary>
        /// Create command line arguments based on
        /// https://learn.microsoft.com/en-us/sql/ssms/ssms-utility?view=sql-server-ver16
        /// </summary>
        public string GetCommandLineArguments(
            Authentication authentication,
            string serverName,
            uint port)
        {
            serverName.ExpectNotEmpty(nameof(serverName));

            var authFlag = authentication == Authentication.Windows
                ? " -E"
                : string.Empty;

            return $"-S {serverName},{port}{authFlag}";
        }

        /// <summary>
        /// Try to find a local installation of SSMS.
        /// </summary>
        public static bool TryFind(out Ssms ssms)
        {
            //
            // Try to locate SSMS by identifying the executable
            // associated with the .ssmssln file extension. This
            // approach is less sensitive to version differences
            // than trying to locate SSMS file or registry entries
            // directlty.
            //

            uint bufferSize = 0;
            var hr = NativeMethods.AssocQueryString(
                NativeMethods.ASSOCF.NONE,
                NativeMethods.ASSOCSTR.EXECUTABLE,
                SsmsFileExtension,
                null,
                null,
                ref bufferSize);

            if (hr != HRESULT.S_FALSE || bufferSize == 0)
            {
                ApplicationTraceSources.Default.TraceVerbose(
                    "The file extension {0} is not associated with any " +
                    "executable, SSMS doesn't seem to be installed (HR: {1})",
                    SsmsFileExtension,
                    hr);

                ssms = null;
                return false;
            }

            var buffer = new StringBuilder((int)bufferSize);
            hr = NativeMethods.AssocQueryString(
                NativeMethods.ASSOCF.NONE,
                NativeMethods.ASSOCSTR.EXECUTABLE,
                ".ssmssln",
                null,
                buffer,
                ref bufferSize);
            if (hr.Failed())
            {
                ApplicationTraceSources.Default.TraceError(
                    "Reading file association data failed: {0}", hr);

                ssms = null;
                return false;
            }

            var executablePath = buffer.ToString();
            if (!executablePath.EndsWith("ssms.exe", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(executablePath))
            {
                ApplicationTraceSources.Default.TraceInformation(
                    "The file extension {0} is associated with {1}, " +
                    "which is not a valid path to ssms.exe",
                    SsmsFileExtension,
                    executablePath);

                ssms = null;
                return false;
            }

            ssms = new Ssms(executablePath);
            return true;
        }

        //---------------------------------------------------------------------
        // Inner classes.
        //---------------------------------------------------------------------

        public enum Authentication
        {
            SqlServer,
            Windows
        }

        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern HRESULT AssocQueryString(
                ASSOCF flags, 
                ASSOCSTR str, 
                string pszAssoc, 
                string pszExtra, 
                [Out] StringBuilder pszOut, 
                ref uint pcchOut);

            public enum ASSOCF
            { 
                NONE = 0
            }

            public enum ASSOCSTR
            {
                EXECUTABLE = 2
            }
        }
    }
}

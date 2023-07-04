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
using System.Runtime.InteropServices;

namespace Google.Solutions.Platform.Dispatch
{
    /// <summary>
    /// A NT/WTS session.
    /// </summary>
    public interface IWtsSession : IEquatable<IWtsSession>
    {
        uint Id { get; }
    }

    public class WtsSession : IWtsSession
    {

        private WtsSession(uint sessionId)
        {
            this.Id = sessionId;
        }

        //---------------------------------------------------------------------
        // IWtsSession.
        //---------------------------------------------------------------------

        public uint Id { get; }

        //---------------------------------------------------------------------
        // Equality.
        //---------------------------------------------------------------------

        public bool Equals(IWtsSession other)
        {
            return other != null && other.Id == this.Id;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IWtsSession);
        }

        public override int GetHashCode()
        {
            return (int)this.Id;
        }

        public static bool operator ==(WtsSession obj1, WtsSession obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(WtsSession obj1, WtsSession obj2)
        {
            return !(obj1 == obj2);
        }

        //---------------------------------------------------------------------
        // Factory methods.
        //---------------------------------------------------------------------

        public static WtsSession GetCurrent()
        {
            if (!NativeMethods.ProcessIdToSessionId(
                NativeMethods.GetCurrentProcessId(),
                out var sessionId))
            {
                throw DispatchException.FromLastWin32Error(
                    "Determining session ID for current process failed");
            }

            return new WtsSession(sessionId);
        }

        public static WtsSession FromProcessId(uint processId)
        {
            if (!NativeMethods.ProcessIdToSessionId(
                processId,
                out var sessionId))
            {
                throw DispatchException.FromLastWin32Error(
                    $"Determining session ID for PID {processId} failed");
            }

            return new WtsSession(sessionId);
        }


        //---------------------------------------------------------------------
        // P/Invoke.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern uint GetCurrentProcessId();

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ProcessIdToSessionId(
                uint dwProcessId,
                out uint pSessionId);
        }
    }
}

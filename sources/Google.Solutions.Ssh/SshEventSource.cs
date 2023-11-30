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

using System.Diagnostics.Tracing;
using System.Security.Policy;

namespace Google.Solutions.Ssh
{
    /// <summary>
    /// ETW event source.
    /// </summary>
    [EventSource(Name = ProviderName, Guid = ProviderGuid)]
    public sealed class SshEventSource : EventSource
    {
        public const string ProviderName = "Google-Solutions-Ssh";
        public const string ProviderGuid = "7FCCFB8B-ABEC-4ADB-B994-E631DD56AA8C";

        public static SshEventSource Log { get; } = new SshEventSource();

        //---------------------------------------------------------------------
        // Connection.
        //---------------------------------------------------------------------

        [Event(1, Level = EventLevel.Verbose)]
        internal void ConnectionHandshakeInitiated(string endpoint)
            => WriteEvent(1, endpoint);

        [Event(2, Level = EventLevel.Verbose)]
        internal void ConnectionHandshakeCompleted(string endpoint)
            => WriteEvent(2, endpoint);

        [Event(3, Level = EventLevel.Warning)]
        internal void ConnectionErrorEncountered(int error)
            => WriteEvent(3, error);

        //---------------------------------------------------------------------
        // Channel.
        //---------------------------------------------------------------------

        [Event(10, Level = EventLevel.Verbose)]
        internal void ShellChannelOpened(string term)
            => WriteEvent(10, term);

        [Event(11, Level = EventLevel.Verbose)]
        internal void SftpChannelOpened()
            => WriteEvent(11);

        [Event(12, Level = EventLevel.Verbose)]
        internal void ChannelReadInitiated(int bufferSize)
            => WriteEvent(12, bufferSize);

        [Event(13, Level = EventLevel.Verbose)]
        internal void ChannelReadCompleted(int bytesRead)
            => WriteEvent(13, bytesRead);

        [Event(14, Level = EventLevel.Verbose)]
        internal void ChannelWriteInitiated(int bufferSize)
            => WriteEvent(14, bufferSize);

        [Event(15, Level = EventLevel.Verbose)]
        internal void ChannelWriteCompleted(int bytesRead)
            => WriteEvent(15, bytesRead);

        [Event(16, Level = EventLevel.Verbose)]
        internal void ChannelCloseInitiated()
            => WriteEvent(16);

        //---------------------------------------------------------------------
        // Authentication.
        //---------------------------------------------------------------------

        [Event(20, Level = EventLevel.Verbose)]
        internal void PublicKeyAuthenticationInitiated(string username)
            => WriteEvent(20, username);

        [Event(21, Level = EventLevel.Verbose)]
        internal void PublicKeyChallengeReceived()
            => WriteEvent(21);

        [Event(22, Level = EventLevel.Verbose)]
        internal void PublicKeyAuthenticationCompleted()
            => WriteEvent(22);

        [Event(23, Level = EventLevel.Verbose)]
        internal void KeyboardInteractiveAuthenticationInitiated()
            => WriteEvent(23);

        [Event(24, Level = EventLevel.Verbose)]
        internal void KeyboardInteractivePromptReceived(
            string? name, 
            string? instruction)
            => WriteEvent(24, name, instruction);

        [Event(25, Level = EventLevel.Warning)]
        internal void KeyboardInteractiveChallengeAborted(
            string exception)
            => WriteEvent(25);

        [Event(26, Level = EventLevel.Verbose)]
        internal void KeyboardInteractiveAuthenticationCompleted()
            => WriteEvent(26);
    }
}

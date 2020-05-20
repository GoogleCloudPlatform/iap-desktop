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

using Google.Apis.Compute.v1;
using Google.Solutions.Common;
using Google.Solutions.Common.Diagnostics;
using System.Threading.Tasks;

namespace Google.Solutions.Compute.Extensions
{
    /// <summary>
    /// Extend 'InstancesResource' by a 'GetSerialPortOutputStream' method.
    /// </summary>
    public static class GetSerialPortOutputStreamExtensions
    {
        /// <summary>
        /// Read serial port output as a continuous stream.
        /// </summary>
        public static SerialPortStream GetSerialPortOutputStream(
            this InstancesResource resource,
            VmInstanceReference instanceRef,
            ushort port)
        {
            return new SerialPortStream(resource, instanceRef, port);
        }
    }

    public class SerialPortStream
    {
        private readonly InstancesResource instancesResource;
        private readonly VmInstanceReference instance;
        private readonly ushort port;

        // Offset of next character to be read.
        private long nextOffset = 0;

        public SerialPortStream(
            InstancesResource instancesResource,
            VmInstanceReference instanceRef,
            ushort port)
        {
            this.instancesResource = instancesResource;
            this.port = port;
            this.instance = instanceRef;
        }

        public async Task<string> ReadAsync()
        {
            var request = this.instancesResource.GetSerialPortOutput(
                this.instance.ProjectId,
                this.instance.Zone,
                this.instance.InstanceName);
            request.Port = this.port;
            request.Start = this.nextOffset;
            var output = await request.ExecuteAsync().ConfigureAwait(false);

            TraceSources.Compute.TraceVerbose(
                "Read {0} chars from serial port [start={1}, next={2}]",
                output.Contents == null ? 0 : output.Contents.Length,
                output.Start.Value,
                output.Next.Value);

            // If there is no new data, then output.Next == this.nextOffset
            // and output.Contents is an empty string.
            this.nextOffset = output.Next.Value;
            return output.Contents;
        }
    }
}

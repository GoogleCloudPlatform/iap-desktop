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
using System;
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
            string project,
            string zone,
            string instance,
            ushort port)
        {
            return GetSerialPortOutputStream(
                resource,
                new VmInstanceReference(project, zone, instance),
                port);
        }

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
        private string lastBuffer = string.Empty;

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
            var output = await request.ExecuteAsync().ConfigureAwait(false);

            // N.B. The first call will return a genuinely new buffer
            // of output. On subsequent calls, we will receive the same
            // output again, potenially with some extra data at the end.
            string newOutput = null;
            if (output.Contents.Length > this.lastBuffer.Length)
            {
                // New data received. 
                newOutput = output.Contents.Substring(this.lastBuffer.Length);
            }
            else if (output.Contents == this.lastBuffer)
            {
                // Nothing happened since last read.
                return string.Empty;
            }
            else if (output.Contents.Length == this.lastBuffer.Length)
            {
                // We must have reached the max buffer size. Assuming the buffers
                // still overlap, we can try to stitch things together.
                int lastBufferTailLength = Math.Min(128, this.lastBuffer.Length);
                var lastBufferTail = this.lastBuffer.Substring(
                    this.lastBuffer.Length - lastBufferTailLength,
                    lastBufferTailLength);

                int indexOfLastBufferTailInOutput = output.Contents.LastIndexOf(lastBufferTail);
                if (indexOfLastBufferTailInOutput > 0)
                {
                    newOutput = output.Contents.Substring(indexOfLastBufferTailInOutput + lastBufferTailLength);
                }
                else
                {
                    // Seems like there is no overlap -- just return everyting then.
                    newOutput = output.Contents;
                }
            }

            this.lastBuffer = output.Contents;
            return newOutput;
        }
    }
}

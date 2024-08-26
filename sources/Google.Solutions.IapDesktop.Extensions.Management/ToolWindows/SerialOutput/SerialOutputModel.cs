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

using Google.Apis.Auth.OAuth2.Responses;
using Google.Solutions.Apis.Compute;
using Google.Solutions.Apis.Locator;
using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Text;
using Google.Solutions.Common.Util;
using Google.Solutions.IapDesktop.Application;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.SerialOutput
{
    public class SerialOutputModel
    {
        private readonly StringBuilder buffer = new StringBuilder();
        private readonly IAsyncReader<string> stream;

        public string Output => this.buffer.ToString();
        public string DisplayName { get; }

        private SerialOutputModel(string displayName, IAsyncReader<string> stream)
        {
            this.DisplayName = displayName;
            this.stream = stream;
        }

        private async Task<string> ReadAndBufferAsync(CancellationToken token)
        {
            var newOutput = await this.stream
                .ReadAsync(token)
                .ConfigureAwait(false);
            newOutput = newOutput.Replace("\n", "\r\n");

            // Add to buffer so that we do not lose the data when model
            // switching occurs.
            this.buffer.Append(newOutput);

            return newOutput;
        }

        public static async Task<SerialOutputModel> LoadAsync(
            string displayName,
            IComputeEngineClient adapter,
            InstanceLocator instanceLocator,
            ushort portNumber,
            CancellationToken token)
        {
            // The serial port log can contain VT100 control sequences, but
            // they are limited to trivial command such as "clear screen". 
            // As there is not much value in preserving these, use an
            // AnsiTextReader to filter out all escape sequences.

            var stream = new XtermReader(
                adapter.GetSerialPortOutput(instanceLocator, portNumber));
            var model = new SerialOutputModel(displayName, stream);

            // Read all existing output.
            while (await model.ReadAndBufferAsync(token).ConfigureAwait(false) != string.Empty)
            {
            }

            return model;
        }

        public Task TailAsync(Action<string> newOutputFunc, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                ApplicationTraceSource.Log.TraceVerbose("Start polling serial output");

                var exceptionCaught = false;
                while (!exceptionCaught)
                {
                    //
                    // Check if we can continue to tail.
                    //
                    if (token.IsCancellationRequested)
                    {
                        ApplicationTraceSource.Log.TraceVerbose("Stop polling serial output");
                        break;
                    }

                    string? newOutput;
                    try
                    {
                        ApplicationTraceSource.Log.TraceVerbose("Polling serial output...");
                        newOutput = await ReadAndBufferAsync(token).ConfigureAwait(false);
                    }
                    catch (TokenResponseException e)
                    {
                        newOutput = "Reading from serial port failed - session timed out " +
                            $"({e.Error.ErrorDescription})";
                        exceptionCaught = true;
                    }
                    catch (Exception e) when (e.IsCancellation())
                    {
                        //
                        // This is deliberate, so do not emit anything to output.
                        //
                        newOutput = null;
                    }
                    catch (Exception e)
                    {
                        newOutput = $"Reading from serial port failed: {e.Unwrap().Message}";
                        exceptionCaught = true;
                    }

                    //
                    // By the time we read the data, the form might have begun closing. In this
                    // case, updating the UI would cause an exception.
                    //
                    if (!token.IsCancellationRequested && newOutput != null && !string.IsNullOrEmpty(newOutput))
                    {
                        newOutputFunc(newOutput);
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        // Do not let the exception escape, instead handle the cancellation
                        // in the next iteration.
                    }
                }
            });
        }
    }
}

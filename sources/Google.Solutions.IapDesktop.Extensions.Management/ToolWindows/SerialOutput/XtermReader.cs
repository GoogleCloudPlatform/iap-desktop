//
// Copyright 2024 Google LLC
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

using Google.Solutions.Common.Text;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Management.ToolWindows.SerialOutput
{
    /// <summary>
    /// Reader that removes Xterm control sequences from a stream
    /// that are commonly encountered in serial console streams. This
    /// is done on a best-effort basis.
    /// </summary>
    internal class XtermReader : IAsyncReader<string>
    {
        /// <summary>
        /// Regex patterns for control sequences that are being sanitized.
        /// </summary>
        private static readonly string[] ControlSequencePatterns = new[] {
            "\u001b\\[2J",                  // Clear the screen.
            "\u001b\\[[A-K]",               // Common VT-52 sequences.
            "\u001b\\[\\d{1,2};\\d{1,2}H",  // Set cursor position.
            "\u001b\\[=3h",                 // Set the terminal to a application keypad mode.
            "\u001b\\[\\d{1,2}m",           // Set Foreground or background colors.
        };

        private static readonly Regex allControlSequencePatterns = new Regex(
            string.Join("|", ControlSequencePatterns));

        private readonly IAsyncReader<string> reader;

        public XtermReader(IAsyncReader<string> reader)
        {
            this.reader = reader;
        }

        //----------------------------------------------------------------------
        // IAsyncReader.
        //----------------------------------------------------------------------

        public async Task<string> ReadAsync(CancellationToken token)
        {
            //
            // NB. It's possible that a control sequence straddles
            //     two chunks. But as we're operating on a best-effort
            //     basis, it's okay to ignore that.
            //
            var chunk = await this.reader
                .ReadAsync(token)
                .ConfigureAwait(false);

            return allControlSequencePatterns.Replace(chunk, string.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.reader.Dispose();
            }
        }
    }
}

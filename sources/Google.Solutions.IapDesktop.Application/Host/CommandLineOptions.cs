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

using Google.Solutions.IapDesktop.Application.Util;
using System;
using System.Windows.Forms;

namespace Google.Solutions.IapDesktop.Application.Host
{
    public class CommandLineOptions
    {
        /// <summary>
        /// URL of VM to connect to (for browser integration).
        /// </summary>
        public IapRdpUrl StartupUrl { get; private set; } = null;

        /// <summary>
        /// Enable logging.
        /// </summary>
        public bool IsLoggingEnabled { get; private set; } = false;

        /// <summary>
        /// Custom profile to load.
        /// </summary>
        public string Profile { get; private set; } = null;

        private CommandLineOptions()
        {
        }

        public static CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "/debug")
                {
                    options.IsLoggingEnabled = true;
                }
                else if (args[i] == "/url" && i + 1 < args.Length)
                {
                    //
                    // Certain legacy browsers do not properly quote URLs when passing them
                    // as command line arguments. If the URL contains a space, it might be
                    // delivered as two separate arguments.
                    //
                    var url = string.Join(" ", args[++i]).Trim();

                    try
                    {
                        options.StartupUrl = IapRdpUrl.FromString(url);
                    }
                    catch (UriFormatException e)
                    {
                        throw new InvalidCommandLineException(
                            "Invalid startup URL:\n\n" + e.Message);
                    }
                }
                else if (args[i] == "/profile" && i + 1 < args.Length)
                {
                    options.Profile = args[++i]
                        .Trim()
                        .NullIfEmptyOrWhitespace();
                }
                else
                {
                    throw new InvalidCommandLineException(
                        $"Unrecognized command line option '{args[0]}'");
                }
            }

            return options;
        }

        public static CommandLineOptions ParseOrExit(string[] args)
        {
            try
            {
                return Parse(args);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    e.Message,
                    "IAP Desktop",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Environment.Exit(1);

                throw new InvalidOperationException();
            }
        }
    }

    public class InvalidCommandLineException : ArgumentException
    {
        public InvalidCommandLineException(string message)
            : base(message)
        {
        }
    }
}

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

using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CA1032 // Implement standard exception constructors

namespace Google.Solutions.IapDesktop.Extensions.Session.Controls
{
    public abstract class RdpException : ApplicationException
    {
        protected RdpException()
        {
        }

        protected RdpException(string message) : base(message)
        {
        }
        protected RdpException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public override string ToString()
        {
            return this.Message;
        }
    }

    public class RdpLogonException : RdpException
    {
        private static readonly Dictionary<int, string> knownErrors = new Dictionary<int, string>
        {
            //
            // Documented error descrriptions from 
            // https://docs.microsoft.com/en-us/windows/win32/termserv/imstscaxevents-onlogonerror
            //
            {-7, "Winlogon is displaying the Disconnect Refused dialog box."},
            {-6, "Winlogon is displaying the No Permissions dialog box."},
            {-5, "Winlogon is displaying the Session Contention dialog box."},
            {-3, "Winlogon is ending silently."},
            {-2, "Winlogon is continuing with the logon process."},
            {-4, "Winlogon is displaying the Reconnect dialog box."},
            {-1, "The user was denied access."},
            {0,  "The logon failed because the logon credentials are not valid."},
            {2,  "Another logon or post-logon error occurred. The Remote Desktop client displays a " +
                 "logon screen to the user."},
            {1,  "The password is expired. The user must update their password to continue logging on."},
            {3,  "The Remote Desktop client displays a dialog box that contains important information for the user."},
            {-1073741714, "The user name and authentication information are valid, but authentication " +
                 "was blocked due to restrictions on the user account, such as time-of-day restrictions."},
            {-1073741715, "The attempted logon is not valid. This is due to either an incorrect user " +
                  "name or incorrect authentication information."},
            {-1073741276, "The password is expired. The user must update their password to continue logging on."}
        };

        public int ErrorCode { get; }

        public bool IsIgnorable => this.ErrorCode < -1;

        public override string Message
        {
            get
            {

                if (knownErrors.TryGetValue(this.ErrorCode, out var message))
                {
                    return message;
                }
                else
                {
                    return $"Logon failed with unknown error code {this.ErrorCode}";
                }
            }
        }

        public RdpLogonException(int errorCode) : base()
        {
            this.ErrorCode = errorCode;
        }
    }

    public class RdpFatalException : RdpException
    {
        private static readonly Dictionary<int, string> knownErrors = new Dictionary<int, string>
        {
            //
            // Documented error descrriptions from 
            // https://docs.microsoft.com/en-us/windows/win32/termserv/imstscaxevents-onfatalerror
            //
            {0, "An unknown error has occurred." },
            {2, "An out-of-memory error has occurred." },
            {3, "A window-creation error has occurred." },
            {7, "An unrecoverable error has occurred during client connection." },
            {10, "Winsock initialization error." }
        };

        public int ErrorCode { get; }

        public override string Message
        {
            get
            {
                if (knownErrors.TryGetValue(this.ErrorCode, out var message))
                {
                    return message;
                }
                else
                {
                    return $"Logon failed with unknown error code {this.ErrorCode}";
                }
            }
        }

        public RdpFatalException(int errorCode)
        {
            this.ErrorCode = errorCode;
        }
    }

    public class RdpDisconnectedException : RdpException
    {
        private static readonly Dictionary<int, string> knownErrors = new Dictionary<int, string>
        {
            //
            // Documented error descrriptions from 
            // https://docs.microsoft.com/en-us/windows/win32/termserv/imstscaxevents-ondisconnected
            //
            {0, "No information is available."},
            {1, "Local disconnection. This is not an error code."},
            {2, "Remote disconnection by user. This is not an error code."},
            {3, "Remote disconnection by server. This is not an error code."},
            {260, "DNS name lookup failure"},
            {262, "Out of memory."},
            {263, "Authentication failure"},
            {264, "Connection timed out."},
            {516, "Unable to establish a connection"},
            {518, "Out of memory."},
            {520, "Host not found error."},
            {772, "Windows Sockets send call failed."},
            {774, "Out of memory."},
            {776, "The IP address specified is not valid."},
            {1028, "Windows Sockets recv call failed."},
            {1030, "Security data is not valid."},
            {1032, "Internal error."},
            {1286, "The encryption method specified is not valid."},
            {1288, "DNS lookup failed."},
            {1540, "Windows Sockets gethostbyname call failed."},
            {1542, "Server security data is not valid."},
            {1544, "Internal timer error."},
            {1796, "Time-out occurred."},
            {1798, "Failed to unpack server certificate."},
            {2052, "Bad IP address specified."},
            {2055, "Login failed."},
            {2056, "License negotiation failed."},
            {2308, "Connection to server lost."},
            {2310, "Internal security error."},
            {2312, "Licensing time-out."},
            {2566, "Internal security error."},
            {2567, "The specified user has no account."},
            {2822, "Encryption error."},
            {2825, "The remote computer requires Network Level Authentication, which your computer does not support." },
            {2823, "The account is disabled."},
            {3078, "Decryption error."},
            {3079, "The account is restricted."},
            {3080, "Decompression error."},
            {3334, "System ran out of resources. Consider disabling bitmap caching."},
            {3335, "The account is locked out."},
            {3591, "The account is expired."},
            {3847, "The password is expired."},
            {4360, "Connection to server was lost." },
            {4615, "The user password must be changed before logging on for the first time."},
            {5639, "The policy does not support delegation of credentials to the target server."},
            {5895, "Delegation of credentials to the target server is not allowed unless mutual " +
                   "authentication has been achieved."},
            {6151, "No authority could be contacted for authentication. The domain name of the authenticating " +
                   "party could be wrong, the domain could be unreachable, or there might have " +
                   "been a trust relationship failure."},
            {6919, "The received certificate is expired."},
            {7175, "An incorrect PIN was presented to the smart card."},
            {7943, "Login aborted" },
            {8455, "The server authentication policy does not allow connection requests using " +
                   "saved credentials. The user must enter new credentials."},
            {8711, "The smart card is blocked."}
        };

        public int DisconnectReason { get; }

        public bool IsTimeout =>
            this.DisconnectReason == 3 ||
            this.DisconnectReason == 264;

        public bool IsUserDisconnectedLocally =>
            this.DisconnectReason == 1;

        public bool IsUserDisconnectedRemotely =>
            this.DisconnectReason == 2;

        public bool IsIgnorable =>
            this.DisconnectReason <= 3 ||
            this.DisconnectReason == 263 ||  // Dismissed server auth warning.
            this.DisconnectReason == 7943;   // Dismissed login prompt.

        public bool IsLogonAborted =>
            this.DisconnectReason == 7943;

        private static string CreateMessage(int disconnectReason, string description)
        {
            var message = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(description))
            {
                message.Append(description);
                message.Append("\n\n");
            }

            if (knownErrors.TryGetValue(disconnectReason, out var reasonText))
            {
                message.Append(reasonText);
            }

            if (disconnectReason == 3 || disconnectReason == 264)
            {
                message.Append(
                    " Consider increasing the connection " +
                    "timeout in the connection settings.");
            }

            if (message.Length == 0)
            {
                message.Append("Disconnected with unknown error code");
            }

            message.Append($"\n\nError code: {disconnectReason}");

            return message.ToString();
        }

        public RdpDisconnectedException(int disconnectReason, string description)
            : base(CreateMessage(disconnectReason, description))
        {
            this.DisconnectReason = disconnectReason;
        }
    }
}

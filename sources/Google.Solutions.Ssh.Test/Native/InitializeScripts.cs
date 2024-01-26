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

namespace Google.Solutions.Ssh.Test.Native
{
    internal static class InitializeScripts
    {
        internal const string InstallEchoServer =
            "apt-get install -y xinetd          \n" +
            "cat << EOF > /etc/xinetd.d/echo    \n" +
            "service echo                       \n" +
            "{                                  \n" +
            "    disable         = no           \n" +
            "    type            = INTERNAL     \n" +
            "    id              = echo-stream  \n" +
            "    socket_type     = stream       \n" +
            "    protocol        = tcp          \n" +
            "    user            = root         \n" +
            "    wait            = no           \n" +
            "}                                  \n" +
            "EOF\n" +
            "\n" +
            "service xinetd restart";

        internal const string AllowNeitherEcdsaNorRsaForHostKey =
            "echo HostKeyAlgorithms ssh-ed25519-cert-v01@openssh.com >> /etc/ssh/sshd_config\n" +
            "echo HostbasedAcceptedKeyTypes ssh-ed25519-cert-v01@openssh.com >> /etc/ssh/sshd_config\n" +
            "service sshd restart";

        internal const string AllowEcdsaOnlyForPubkey =
            "echo PubkeyAcceptedKeyTypes ecdsa-sha2-nistp256,ecdsa-sha2-nistp384,ecdsa-sha2-nistp521 >> /etc/ssh/sshd_config\n" +
            "service sshd restart";

        internal const string EcdsaNistp256HostKey =
            "rm -f /etc/ssh/ssh_host_ecdsa_key*\n" +
            "ssh-keygen -q -N \"\" -t ecdsa -b 256 -f /etc/ssh/ssh_host_ecdsa_key\n" +
            "systemctl restart sshd";


        internal const string EcdsaNistp384HostKey =
            "rm -f /etc/ssh/ssh_host_ecdsa_key*\n" +
            "ssh-keygen -q -N \"\" -t ecdsa -b 384 -f /etc/ssh/ssh_host_ecdsa_key\n" +
            "systemctl restart sshd";


        internal const string EcdsaNistp521HostKey =
            "rm -f /etc/ssh/ssh_host_ecdsa_key*\n" +
            "ssh-keygen -q -N \"\" -t ecdsa -b 521 -f /etc/ssh/ssh_host_ecdsa_key\n" +
            "systemctl restart sshd";
    }
}

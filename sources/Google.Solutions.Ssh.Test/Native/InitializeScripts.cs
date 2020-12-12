using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}

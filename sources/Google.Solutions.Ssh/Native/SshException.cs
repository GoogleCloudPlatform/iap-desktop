using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Native
{
    public class SshException : Exception
    {
        public SshException(string message) : base(message)
        {
        }
    }

    public class SshNativeException : SshException
    {
        private static IDictionary<LIBSSH2_ERROR, string> messages
            = new Dictionary<LIBSSH2_ERROR, string>()
            {
                { LIBSSH2_ERROR.SOCKET_NONE, "The socket is invalid"},
                { LIBSSH2_ERROR.BANNER_SEND, "Unable to send banner to remote host"},
                { LIBSSH2_ERROR.KEX_FAILURE, "Encryption key exchange with the remote host failed"},
                { LIBSSH2_ERROR.SOCKET_SEND, "Unable to send data on socket"},
                { LIBSSH2_ERROR.SOCKET_DISCONNECT, "The socket was disconnected"},
                { LIBSSH2_ERROR.PROTO, "An invalid SSH protocol response was received on the socket"},
                { LIBSSH2_ERROR.INVAL, "The requested method type was invalid" },
                { LIBSSH2_ERROR.METHOD_NONE, "No method has been set" },
                { LIBSSH2_ERROR.BAD_USE, "Invalid address of algs" },
                { LIBSSH2_ERROR.ALLOC, "Allocation of memory failed" },
                { LIBSSH2_ERROR.METHOD_NOT_SUPPORTED, "The requested method is not supported" },
            };

        public LIBSSH2_ERROR ErrorCode { get; }


        private static string MessageFromCode(LIBSSH2_ERROR code)
        {
            if (messages.TryGetValue(code, out var message))
            {
                return message;
            }
            else
            {
                return $"Unknown SSH error {code}";
            }
        }

        public SshNativeException(LIBSSH2_ERROR code) 
            : base(MessageFromCode(code))
        {
            Debug.Assert(code != LIBSSH2_ERROR.NONE);
            Debug.Assert(code != LIBSSH2_ERROR.EAGAIN);

            this.ErrorCode = code;
        }
    }

    public enum LIBSSH2_ERROR : Int32
    {
        NONE = 0,
        SOCKET_NONE = -1,
        BANNER_RECV = -2,
        BANNER_SEND = -3,
        INVALID_MAC = -4,
        KEX_FAILURE = -5,
        ALLOC = -6,
        SOCKET_SEND = -7,
        KEY_EXCHANGE_FAILURE = -8,
        TIMEOUT = -9,
        HOSTKEY_INIT = -10,
        HOSTKEY_SIGN = -11,
        DECRYPT = -12,
        SOCKET_DISCONNECT = -13,
        PROTO = -14,
        PASSWORD_EXPIRED = -15,
        FILE = -16,
        METHOD_NONE = -17,
        AUTHENTICATION_FAILED = -18,
        PUBLICKEY_UNRECOGNIZED = -18,
        PUBLICKEY_UNVERIFIED = -19,
        CHANNEL_OUTOFORDER = -20,
        CHANNEL_FAILURE = -21,
        CHANNEL_REQUEST_DENIED = -22,
        CHANNEL_UNKNOWN = -23,
        CHANNEL_WINDOW_EXCEEDED = -24,
        CHANNEL_PACKET_EXCEEDED = -25,
        CHANNEL_CLOSED = -26,
        CHANNEL_EOF_SENT = -27,
        SCP_PROTOCOL = -28,
        ZLIB = -29,
        SOCKET_TIMEOUT = -30,
        SFTP_PROTOCOL = -31,
        REQUEST_DENIED = -32,
        METHOD_NOT_SUPPORTED = -33,
        INVAL = -34,
        INVALID_POLL_TYPE = -35,
        PUBLICKEY_PROTOCOL = -36,
        EAGAIN = -37,
        BUFFER_TOO_SMALL = -38,
        BAD_USE = -39,
        COMPRESS = -40,
        OUT_OF_BOUNDARY = -41,
        AGENT_PROTOCOL = -42,
        SOCKET_RECV = -43,
        ENCRYPT = -44,
        BAD_SOCKET = -45,
        KNOWN_HOSTS = -46,
        CHANNEL_WINDOW_FULL = -47,
        KEYFILE_AUTH_FAILED = -48,
    }
}

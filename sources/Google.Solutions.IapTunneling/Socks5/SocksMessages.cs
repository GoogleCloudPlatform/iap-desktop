using Google.Solutions.IapTunneling.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Google.Solutions.IapTunneling.Socks5
{
    internal class MessageBuffer
    {
        public byte[] Buffer { get; }

        public MessageBuffer(byte[] buffer)
        {
            this.Buffer = buffer;
        }
    }

    internal enum AuthenticationMethod : Byte
    {
        NoAuthenticationRequired = 0x0,
        GssApi = 0x1,
        UsernamePassword = 0x2,
        NoAcceptableMethods = 0xFF
    }

    internal struct NegotiateMethodRequest
    {
        public const ushort MaxMethods = 255;
        public const ushort MaxSize = MaxMethods + 2 * sizeof(byte);

        public readonly byte Version;
        public readonly AuthenticationMethod[] Methods;

        public NegotiateMethodRequest(byte version, AuthenticationMethod[] methods)
        {
            this.Version = version;
            this.Methods = methods;
        }
    }

    internal struct NegotiateMethodResponse
    {
        public readonly AuthenticationMethod Method;

        public NegotiateMethodResponse(AuthenticationMethod method)
        {
            this.Method = method;
        }
    }

    internal enum Command : byte
    {
        Connect = 1,
        Bind = 2,
        UdpAssociate = 3
    }

    internal enum AddressType : byte
    {
        IPv4 = 1,
        DomainName = 3,
        IPv6 = 4
    }

    internal struct ConnectionRequest
    {
        public readonly byte Version;
        public readonly Command Command;
        public readonly AddressType AddressType;
        public readonly byte[] DestinationAddress;
        public readonly ushort DestinationPort;

        public ConnectionRequest(
            byte version,
            Command command,
            AddressType addressType,
            byte[] destinationAddress,
            ushort destinationPort)
        {
            this.Version = version;
            this.Command = command;
            this.AddressType = addressType;
            this.DestinationAddress = destinationAddress;
            this.DestinationPort = destinationPort;
        }
    }

    internal enum ConnectionReply : byte
    {
        Succeeded = 0,
        GeneralServerFailure = 1,
        ConnectionNotAllowed = 2,
        NetworkUnreachable = 3,
        HostUnreachable = 4,
        ConnectionRefused = 5,
        TimeoutExpired = 6,
        CommandNotSupported = 7,
        AddressTypeNotSupported = 8
    }

    internal struct ConnectionResponse
    {
        public readonly byte Version;
        public readonly ConnectionReply Reply;
        public readonly AddressType AddressType;
        public readonly byte[] ServerAddress;
        public readonly ushort ServerPort;

        public ConnectionResponse(
            byte version,
            ConnectionReply reply,
            AddressType addressType,
            byte[] serverAddress,
            ushort serverPort)
        {
            this.Version = version;
            this.Reply = reply;
            this.AddressType = addressType;
            this.ServerAddress = serverAddress;
            this.ServerPort = serverPort;
        }
    }
}

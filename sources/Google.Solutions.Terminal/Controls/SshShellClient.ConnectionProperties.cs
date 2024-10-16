using Google.Solutions.Ssh;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;

namespace Google.Solutions.Terminal.Controls
{
    public partial class SshShellClient
    {
        protected const string SshCategory = "SSH";

        private IPEndPoint? serverEndpoint;
        private ISshCredential? credential;
        private string? banner;
        private TimeSpan connectionTimeout = TimeSpan.FromSeconds(30);
        private CultureInfo? locale;
        private IKeyboardInteractiveHandler keyboardInteractiveHandler
            = new DefaultKeyboardInteractiveHandler();

        /// <summary>
        /// Endpoint to connect to.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public IPEndPoint? ServerEndpoint
        {
            get => this.serverEndpoint;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.serverEndpoint = value;
            }
        }

        /// <summary>
        /// User credential to authenticate with.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public virtual ISshCredential? Credential
        {
            get => this.credential;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.credential = value;
            }
        }

        /// <summary>
        /// Handler for password/input prompts.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public IKeyboardInteractiveHandler KeyboardInteractiveHandler
        {
            get => this.keyboardInteractiveHandler;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.keyboardInteractiveHandler = value;
            }
        }

        /// <summary>
        /// Client banner to send to server.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public string? Banner
        {
            get => this.banner;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.banner = value;
            }
        }

        /// <summary>
        /// Timeout for establishing an SSH connection.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public TimeSpan ConnectionTimeout
        {
            get => this.connectionTimeout;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.connectionTimeout = value;
            }
        }

        /// <summary>
        /// LC_ALL locale.
        /// </summary>
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category(SshCategory)]
        public CultureInfo? Locale
        {
            get => this.locale;
            set
            {
                ExpectState(ConnectionState.NotConnected);
                this.locale = value;
            }
        }

        /// <summary>
        /// Type of terminal ($TERM) to use.
        /// </summary>
        public string TerminalType
        {
            get; set;
        } = "xterm";

        //---------------------------------------------------------------------
        // Inner types.
        //---------------------------------------------------------------------

        /// <summary>
        /// Default handler, cancels all input.
        /// </summary>
        private class DefaultKeyboardInteractiveHandler : IKeyboardInteractiveHandler
        {
            public string? Prompt(string caption, string instruction, string prompt, bool echo)
            {
                throw new OperationCanceledException();
            }

            public IPasswordCredential PromptForCredentials(string username)
            {
                throw new OperationCanceledException();
            }
        }
    }
}

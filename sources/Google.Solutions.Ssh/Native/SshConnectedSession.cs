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

using Google.Solutions.Common.Diagnostics;
using Google.Solutions.Common.Security;
using Google.Solutions.Common.Util;
using Google.Solutions.Ssh.Cryptography;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Google.Solutions.Ssh.Native
{
    /// <summary>
    /// An connected Libssh2 session.
    /// </summary>
    public class SshConnectedSession : IDisposable
    {
        internal const int KeyboardInteractiveRetries = 3;

        // NB. This object does not own this handle and should not dispose it.
        private readonly SshSession session;

        private readonly Socket socket;
        private bool disposed = false;

        public Socket Socket => this.socket;

        private static int HostKeyHashLength(LIBSSH2_HOSTKEY_HASH hashType)
        {
            switch (hashType)
            {
                case LIBSSH2_HOSTKEY_HASH.MD5:
                    return 16;

                case LIBSSH2_HOSTKEY_HASH.SHA1:
                    return 16;

                case LIBSSH2_HOSTKEY_HASH.SHA256:
                    return 32;

                default:
                    throw new ArgumentException(nameof(hashType));
            }
        }

        //---------------------------------------------------------------------
        // Ctor.
        //---------------------------------------------------------------------

        internal SshConnectedSession(SshSession session, Socket socket)
        {
            this.session = session;
            this.socket = socket;
        }

        //---------------------------------------------------------------------
        // Banner.
        //---------------------------------------------------------------------

        public string? GetRemoteBanner()
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var bannerPtr = NativeMethods.libssh2_session_banner_get(
                    this.session.Handle);

                return bannerPtr == IntPtr.Zero
                    ? null
                    : Marshal.PtrToStringAnsi(bannerPtr);
            }
        }


        //---------------------------------------------------------------------
        // Algorithms.
        //---------------------------------------------------------------------

        public string[] GetActiveAlgorithms(LIBSSH2_METHOD methodType)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
            if (!Enum.IsDefined(typeof(LIBSSH2_METHOD), methodType))
            {
                throw new ArgumentException(nameof(methodType));
            }

            using (SshTraceSource.Log.TraceMethod().WithParameters(methodType))
            {
                var stringPtr = NativeMethods.libssh2_session_methods(
                    this.session.Handle,
                    methodType);

                if (stringPtr == IntPtr.Zero)
                {
                    return Array.Empty<string>();
                }
                else
                {
                    var algorithmList = Marshal.PtrToStringAnsi(stringPtr);
                    return algorithmList.Split(',').ToArray();
                }
            }
        }

        //---------------------------------------------------------------------
        // Host key.
        //---------------------------------------------------------------------

        public byte[]? GetRemoteHostKeyHash(LIBSSH2_HOSTKEY_HASH hashType)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
            if (!Enum.IsDefined(typeof(LIBSSH2_HOSTKEY_HASH), hashType))
            {
                throw new ArgumentException(nameof(hashType));
            }

            using (SshTraceSource.Log.TraceMethod().WithParameters(hashType))
            {
                var hashPtr = NativeMethods.libssh2_hostkey_hash(
                    this.session.Handle,
                    hashType);

                if (hashPtr == IntPtr.Zero)
                {
                    return null;
                }
                else
                {
                    var hash = new byte[HostKeyHashLength(hashType)];
                    Marshal.Copy(hashPtr, hash, 0, hash.Length);
                    return hash;
                }
            }
        }

        public byte[]? GetRemoteHostKey()
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var keyPtr = NativeMethods.libssh2_session_hostkey(
                    this.session.Handle,
                    out var keyLength,
                    out var _);

                if (keyPtr == IntPtr.Zero || keyLength.ToInt32() <= 0)
                {
                    return null;
                }
                else
                {
                    var key = new byte[keyLength.ToInt32()];
                    Marshal.Copy(keyPtr, key, 0, keyLength.ToInt32());
                    return key;
                }
            }
        }

        public LIBSSH2_HOSTKEY_TYPE GetRemoteHostKeyType()
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSource.Log.TraceMethod().WithoutParameters())
            {
                var keyPtr = NativeMethods.libssh2_session_hostkey(
                    this.session.Handle,
                    out var _,
                    out var type);

                if (keyPtr == IntPtr.Zero)
                {
                    return LIBSSH2_HOSTKEY_TYPE.UNKNOWN;
                }
                else
                {
                    return type;
                }
            }
        }

        //---------------------------------------------------------------------
        // User auth.
        //---------------------------------------------------------------------

        public bool IsAuthenticated
        {
            get
            {
                using (SshTraceSource.Log.TraceMethod().WithoutParameters())
                {
                    this.session.Handle.CheckCurrentThreadOwnsHandle();

                    return NativeMethods.libssh2_userauth_authenticated(
                        this.session.Handle) == 1;
                }
            }
        }

        public string[] GetAuthenticationMethods(string username)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();

            using (SshTraceSource.Log.TraceMethod().WithParameters(username))
            {
                var stringPtr = NativeMethods.libssh2_userauth_list(
                    this.session.Handle,
                    username,
                    username.Length);

                if (stringPtr == IntPtr.Zero)
                {
                    //
                    // This is an error, not an empty list.
                    //
                    throw this.session.CreateException(this.session.LastError);
                }
                else
                {
                    return Marshal
                        .PtrToStringAnsi(stringPtr)
                        .Split(',')
                        .ToArray();
                }
            }
        }

        private SshAuthenticatedSession AuthenticateWithKeyboard(
            ISshCredential credential,
            IKeyboardInteractiveHandler keyboardHandler,
            string defaultPromptName)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(credential, nameof(credential));
            Precondition.ExpectNotNull(keyboardHandler, nameof(keyboardHandler));

            Exception? interactiveCallbackException = null;

            void InteractiveCallback(
                IntPtr namePtr,
                int nameLength,
                IntPtr instructionPtr,
                int instructionLength,
                int numPrompts,
                IntPtr promptsPtr,
                IntPtr responsesPtr,
                IntPtr context)
            {
                var name = NativeMethods.PtrToString(
                    namePtr,
                    nameLength,
                    Encoding.UTF8);
                var instruction = NativeMethods.PtrToString(
                    instructionPtr,
                    nameLength,
                    Encoding.UTF8);
                var prompts = NativeMethods.PtrToStructureArray<
                        NativeMethods.LIBSSH2_USERAUTH_KBDINT_PROMPT>(
                    promptsPtr,
                    numPrompts);

                SshEventSource.Log.KeyboardInteractivePromptReceived(name, instruction);

                //
                // NB. libssh2 allocates the responses structure for us, but frees
                // the embedded text strings using its allocator.
                // 
                // NB. libssh2 assumes text to be encoded in UTF-8.
                //
                Debug.Assert(SshSession.Alloc != null);

                var responses = new NativeMethods.LIBSSH2_USERAUTH_KBDINT_RESPONSE[prompts.Length];
                for (var i = 0; i < prompts.Length; i++)
                {
                    var promptText = NativeMethods.PtrToString(
                        prompts[i].TextPtr,
                        prompts[i].TextLength,
                        Encoding.UTF8);

                    SshTraceSource.Log.TraceVerbose("Keyboard/interactive prompt: {0}", promptText);

                    //
                    // NB. Name and instruction are often null or empty:
                    //
                    //  - OS Login 2SV sets the prompt text, but leaves name and
                    //    instruction empty.
                    //  - When keyboard-interactive is used to handle password-
                    //    authentication, the prompt text contains "Password:",
                    //    and name and instruction are empty.
                    //
                    string? responseText = null;
                    try
                    {
                        responseText = keyboardHandler.Prompt(
                            name ?? defaultPromptName,
                            instruction ?? string.Empty,
                            promptText ?? string.Empty,
                            prompts[i].Echo != 0);
                    }
                    catch (Exception e)
                    {
                        SshTraceSource.Log.TraceError(
                            "Authentication callback threw exception", e);

                        SshEventSource.Log.KeyboardInteractiveChallengeAborted(e.FullMessage());

                        //
                        // Don't let the exception escape into unmanaged code,
                        // instead return null and let the enclosing method
                        // rethrow the exception once we're back on a managed
                        // callstack.
                        //
                        interactiveCallbackException = e;
                    }

                    responses[i] = new NativeMethods.LIBSSH2_USERAUTH_KBDINT_RESPONSE();
                    if (responseText == null)
                    {
                        responses[i].TextLength = 0;
                        responses[i].TextPtr = IntPtr.Zero;
                    }
                    else
                    {
                        var responseTextBytes = Encoding.UTF8.GetBytes(responseText);
                        responses[i].TextLength = responseTextBytes.Length;
                        responses[i].TextPtr = SshSession.Alloc!(
                            new IntPtr(responseTextBytes.Length),
                            IntPtr.Zero);
                        Marshal.Copy(
                            responseTextBytes,
                            0,
                            responses[i].TextPtr,
                            responseTextBytes.Length);
                    }
                }

                NativeMethods.StructureArrayToPtr(
                    responsesPtr,
                    responses);
            }

            using (SshTraceSource.Log.TraceMethod().WithParameters(credential.Username))
            {
                var result = LIBSSH2_ERROR.NONE;

                //
                // Temporarily change the timeout since we must give the
                // user some time to react.
                //
                var originalTimeout = this.session.Timeout;
                this.session.Timeout = this.session.KeyboardInteractivePromptTimeout;

                try
                {
                    //
                    // Retry to account for wrong user input.
                    //
                    for (var retry = 0; retry < KeyboardInteractiveRetries; retry++)
                    {
                        SshEventSource.Log.KeyboardInteractiveAuthenticationInitiated();

                        result = (LIBSSH2_ERROR)NativeMethods.libssh2_userauth_keyboard_interactive_ex(
                            this.session.Handle,
                            credential.Username,
                            credential.Username.Length,
                            InteractiveCallback,
                            IntPtr.Zero);

                        if (result == LIBSSH2_ERROR.NONE)
                        {
                            break;
                        }
                        else if (interactiveCallbackException != null)
                        {
                            //
                            // Restore exception thrown in callback.
                            //
                            throw interactiveCallbackException;
                        }
                    }
                }
                finally
                {
                    //
                    // Restore timeout.
                    //
                    this.session.Timeout = originalTimeout;
                }

                if (result == LIBSSH2_ERROR.NONE)
                {
                    SshEventSource.Log.KeyboardInteractiveAuthenticationCompleted();

                    return new SshAuthenticatedSession(this.session);
                }
                else
                {
                    throw this.session.CreateException(result);
                }
            }
        }

        private SshAuthenticatedSession AuthenticateWithPassword(
            IPasswordCredential credential,
            IKeyboardInteractiveHandler keyboardHandler)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(credential, nameof(credential));

            var passwordChangeCallbackInvocations = 0;
            int PasswordChangeCallback(
                IntPtr session,
                IntPtr newPasswordPtr,
                IntPtr newPasswordLengthPtr,
                IntPtr context)
            {
                //
                // We don't support password changes. Leaving parameters
                // unchanged will abort the process.
                //
                passwordChangeCallbackInvocations++;
                return -1;
            }

            using (SshTraceSource.Log.TraceMethod().WithParameters(credential.Username))
            {
                SshEventSource.Log.PasswordAuthenticationInitiated(credential.Username);

                var username = credential.Username;
                var password = credential.Password.AsClearText();

                if (string.IsNullOrEmpty(password))
                {
                    //
                    // Prompt user to edit or amend credentials.
                    //
                    // NB. This callback might throw an exception when
                    // canceled by the user.
                    //
                    var newCredential = keyboardHandler.PromptForCredentials(credential);

                    username = newCredential.Username;
                    password = newCredential.Password.AsClearText();
                }

                var result = (LIBSSH2_ERROR)NativeMethods.libssh2_userauth_password_ex(
                    this.session.Handle,
                    username,
                    username.Length,
                    password,
                    password.Length,
                    PasswordChangeCallback);

                if (result == LIBSSH2_ERROR.NONE)
                {
                    SshEventSource.Log.PasswordAuthenticationCompleted();

                    return new SshAuthenticatedSession(this.session);
                }
                else if (passwordChangeCallbackInvocations > 0)
                {
                    throw new UnsupportedAuthenticationMethodException(
                        "The password expired and must be changed",
                        this.session.CreateException(result));
                }
                else
                {
                    throw this.session.CreateException(result);
                }
            }
        }

        private SshAuthenticatedSession AuthenticateWithPublicKey(
            IAsymmetricKeyCredential credential,
            IKeyboardInteractiveHandler keyboardHandler)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(credential, nameof(credential));
            Precondition.ExpectNotNull(keyboardHandler, nameof(keyboardHandler));

            int Sign(
                IntPtr session,
                out IntPtr signaturePtr,
                out IntPtr signatureLength,
                IntPtr challengePtr,
                IntPtr challengeLength,
                IntPtr context)
            {
                Debug.Assert(context == IntPtr.Zero);
                Debug.Assert(session == this.session.Handle.DangerousGetHandle());

                SshEventSource.Log.PublicKeyChallengeReceived();

                //
                // Read the challenge.
                //
                var challengeBuffer = new byte[challengeLength.ToInt32()];
                Marshal.Copy(challengePtr, challengeBuffer, 0, challengeBuffer.Length);

                var challenge = new AuthenticationChallenge(challengeBuffer);
                var signature = credential.Signer.Sign(challenge);

                //
                // Copy data back to a buffer that libssh2 can free using
                // the allocator specified in libssh2_session_init_ex.
                //
                signatureLength = new IntPtr(signature.Length);
                signaturePtr = SshSession.Alloc(signatureLength, IntPtr.Zero);
                Marshal.Copy(signature, 0, signaturePtr, signature.Length);

                return (int)LIBSSH2_ERROR.NONE;
            }

            using (SshTraceSource.Log.TraceMethod().WithParameters(credential.Username))
            {
                var publicKey = credential.Signer.PublicKey.WireFormatValue;

                SshEventSource.Log.PublicKeyAuthenticationInitiated(credential.Username);

                var result = (LIBSSH2_ERROR)NativeMethods.libssh2_userauth_publickey(
                    this.session.Handle,
                    credential.Username,
                    publicKey,
                    new IntPtr(publicKey.Length),
                    Sign,
                    IntPtr.Zero);

                if (result == LIBSSH2_ERROR.NONE)
                {
                    SshEventSource.Log.PublicKeyAuthenticationCompleted();

                    return new SshAuthenticatedSession(this.session);
                }
                else if (result == LIBSSH2_ERROR.PUBLICKEY_UNVERIFIED)
                {
                    //
                    // Public key wasn't sufficient for authentication. There
                    // might be additional MFA challenges we need to handle.
                    //
                    var requiredMethods = GetAuthenticationMethods(credential.Username);

                    SshTraceSource.Log.TraceVerbose(
                        "Server responded that public key is unverified, " +
                        "additionally requires {0}",
                        string.Join(", ", requiredMethods));

                    if (requiredMethods.FirstOrDefault() == AuthenticationMetods.KeyboardInteractive)
                    {
                        return AuthenticateWithKeyboard(
                            credential,
                            keyboardHandler,
                            "2-step verification");
                    }
                    else
                    {
                        throw new UnsupportedAuthenticationMethodException(
                            $"The server requires {string.Join(", ", requiredMethods)} as " +
                            "additional authentication methods, which is currently not supported.",
                            this.session.CreateException(result));
                    }
                }
                else
                {
                    throw this.session.CreateException(result);
                }
            }
        }

        public SshAuthenticatedSession Authenticate(
            ISshCredential credential,
            IKeyboardInteractiveHandler keyboardHandler)
        {
            this.session.Handle.CheckCurrentThreadOwnsHandle();
            Precondition.ExpectNotNull(credential, nameof(credential));
            Precondition.ExpectNotNull(keyboardHandler, nameof(keyboardHandler));

            using (SshTraceSource.Log.TraceMethod().WithParameters(credential.Username))
            {
                //
                // Query authentication methods supported by server.
                //
                // NB. The server might require a sequence of authentication methods,
                // for example:
                //
                //  AuthenticationMethods publickey password,keyboard-interactive
                //
                // In this case, GetAuthenticationMethods returns publickey and password,
                // but not keyboard-interactive.
                //
                var authenticationMethods = GetAuthenticationMethods(credential.Username);

                if (authenticationMethods.Contains(AuthenticationMetods.PublicKey) &&
                    credential is IAsymmetricKeyCredential keyCredential)
                {
                    return AuthenticateWithPublicKey(keyCredential, keyboardHandler);
                }
                else if (authenticationMethods.Contains(AuthenticationMetods.Password) &&
                    credential is IPasswordCredential passwordCredential)
                {
                    return AuthenticateWithPassword(passwordCredential, keyboardHandler);
                }
                else if (authenticationMethods.Contains(AuthenticationMetods.KeyboardInteractive))
                {
                    return AuthenticateWithKeyboard(
                        credential,
                        keyboardHandler,
                        "Interactive authentication");
                }
                else
                {
                    throw new UnsupportedAuthenticationMethodException(
                        "The supplied credential is incompatible with the " +
                        "authentication methods supported by the server:\n\n - " +
                        string.Join("\n - ", authenticationMethods));
                }
            }
        }

        //---------------------------------------------------------------------
        // Dispose.
        //---------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.session.Handle.CheckCurrentThreadOwnsHandle();

                var result = (LIBSSH2_ERROR)NativeMethods.libssh2_session_disconnect_ex(
                    this.session.Handle,
                    SSH_DISCONNECT.BY_APPLICATION,
                    null,
                    null);

                Debug.Assert(result == LIBSSH2_ERROR.NONE);
                this.socket.Dispose();
                this.disposed = true;
            }
        }
    }
}

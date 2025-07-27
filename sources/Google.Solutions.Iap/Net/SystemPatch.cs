//
// Copyright 2019 Google LLC
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

using Google.Solutions.Common.Util;
using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace Google.Solutions.Iap.Net
{
    /// <summary>
    /// Patches that alter the default .NET configuration by
    /// modiying private or internal data structures.
    /// 
    /// The process to install/uninstall patches is not thread-
    /// safe and is best done during application startup.
    /// </summary>
    public abstract class SystemPatch
    {
        /// <summary>
        /// Install the patch.
        /// </summary>
        public abstract void Install();

        /// <summary>
        /// Unnstall the patch.
        /// </summary>
        public abstract void Uninstall();

        /// <summary>
        /// Determine if the patch is active.
        /// </summary>
        public abstract bool IsInstalled { get; }

        /// <summary>
        /// Allow callers to modify the "User-Agent" header of HTTP requests.
        /// </summary>
        public static readonly SystemPatch UnrestrictUserAgentHeader
            = new UnrestrictHeaderPatch("User-Agent");

        /// <summary>
        /// Install a custom IWebRequestCreate implementation
        /// that extracts the userinfo part of an URL (http://userinfo@host/) 
        /// and sets it as Host header.
        /// </summary>
        public static readonly SystemPatch SetUsernameAsHostHeaderForWssRequests
            = new SetUsernameAsHostHeaderPatch("wss:");

        //---------------------------------------------------------------------
        // Inner classes for installing and uninstalling patches.
        //---------------------------------------------------------------------

        /// <summary>
        /// Make a "restricted" HTTP header unrestricted so that it can be modified.
        /// </summary>
        internal class UnrestrictHeaderPatch : SystemPatch
        {
            private bool installed;
            private readonly string headerName;

            public UnrestrictHeaderPatch(string headerName)
            {
                this.headerName = headerName;
            }

            public override bool IsInstalled => this.installed;

            public override void Install()
            {
                if (!this.IsInstalled)
                {
                    SetHeaderRestriction(this.headerName, false);
                }

                this.installed = true;
            }

            public override void Uninstall()
            {
                if (this.IsInstalled)
                {
                    SetHeaderRestriction(this.headerName, true);
                }

                this.installed = false;
            }

            private static void SetHeaderRestriction(
                string header, 
                bool restricted)
            {
                //
                // Headers like "User-Agent" are considered a "restricted header"
                // by .NET. While most HTTP clases allow you to specify the header
                // by using a special property, the ClientWebSocket class does not.
                // 
                // To overcome this limitation, the only option is to patch
                // the libraries internal configuration. As this requires
                // accessing internal and private fields, it might break at
                // any time.
                // 
                // It is therefore important that any code using restricted
                // headers is written so that it gracefully handles failures.
                //
                // The list of restricted headers is stored in a global 
                // HeaderInfoTable object.
                //
                if (typeof(WebHeaderCollection).GetField(
                    "HInfo",
                    BindingFlags.NonPublic | BindingFlags.Static) 
                        is FieldInfo hInfoField)
                {
                    var headerInfoTable = hInfoField.GetValue(null);

                    //
                    // Get the HeaderHashTable field (instance of Hashtable).
                    //
                    if (headerInfoTable.GetType().GetField(
                        "HeaderHashTable",
                        BindingFlags.NonPublic | BindingFlags.Static)
                            is FieldInfo headersField)
                    {
                        if (headersField.GetValue(null) is Hashtable headers)
                        {
                            //
                            // Modify the HeaderInfo object stored in the hashtable.
                            //
                            var headerInfo = headers[header];
                            if (headerInfo != null &&
                                headerInfo.GetType().GetField(
                                    "IsRequestRestricted",
                                    BindingFlags.NonPublic | BindingFlags.Instance) 
                                        is FieldInfo restrictedField)
                            {
                                restrictedField.SetValue(headerInfo, restricted);
                                return;
                            }
                        }
                    }
                }

                throw new InvalidOperationException(
                    "Changing the header configuration failed");
            }
        }

        /// <summary>
        /// CLientWebSocket (and possibly other classes) don't let callers
        /// override the Host header for requests.
        /// 
        /// As a nasty workaround, install a custom IWebRequestCreate 
        /// implementation that extracts the userinfo part of an URL 
        /// (http://userinfo@host/) and sets it as Host header.
        /// </summary>
        internal class SetUsernameAsHostHeaderPatch :
            SystemPatch, IWebRequestCreate
        {
            private readonly string prefix;
            private IWebRequestCreate? originalFactory;

            public SetUsernameAsHostHeaderPatch(string prefix)
            {
                this.prefix = prefix;
                Debug.Assert(this.prefix.EndsWith(":"));
            }

            //---------------------------------------------------------------------
            // Overrides.
            //---------------------------------------------------------------------

            public override bool IsInstalled
            {
                get =>  this.originalFactory != null;
            }

            public override void Install()
            {
                if (!this.IsInstalled)
                {
                    this.originalFactory = ReplaceWebRequestPrefixRegistration(
                        this.prefix,
                        f => this);
                }

                Debug.Assert(this.IsInstalled);
            }

            public override void Uninstall()
            {
                if (this.IsInstalled)
                {
                    if (this.originalFactory == null)
                    {
                        throw new InvalidOperationException();
                    }

                    ReplaceWebRequestPrefixRegistration(
                        this.prefix,
                        _ => this.originalFactory);
                    this.originalFactory = null;
                }

                Debug.Assert(!this.IsInstalled);
            }

            //---------------------------------------------------------------------
            // Request factory methods.
            //---------------------------------------------------------------------

            public WebRequest Create(Uri uri)
            {
                if (this.originalFactory == null)
                {
                    throw new InvalidOperationException();
                }

                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    Debug.Assert(!uri.UserInfo.Contains(":"));
                    Debug.Assert(uri.Host != uri.UserInfo);

                    //
                    // Create a request without the userinfo,
                    // then overwrite the Host property.
                    //
                    var request = (HttpWebRequest)this.originalFactory
                        .Create(new UriBuilder(uri)
                        {
                            UserName = null,
                            Password = null
                        }.Uri);

                    request.Host = uri.UserInfo;
                    return request;
                }
                else
                {
                    return this.originalFactory.Create(uri);
                }
            }

            private static IWebRequestCreate ReplaceWebRequestPrefixRegistration(
                string prefix,
                Func<IWebRequestCreate, IWebRequestCreate> replaceFunc)
            {
                prefix.ExpectNotEmpty(nameof(prefix));
                Debug.Assert(prefix.EndsWith(":"));

                var prop = typeof(WebRequest).GetProperty(
                    "PrefixList",
                    BindingFlags.NonPublic | BindingFlags.Static);

                if (!(prop?.GetValue(null) is ArrayList prefixes))
                {
                    throw new InvalidOperationException(
                        "Accessing WebRequest.PrefixList failed");
                }

                foreach (var entry in prefixes)
                {
                    var prefixField = entry.GetType().GetField("Prefix");
                    if (prefixField == null)
                    {
                        throw new InvalidOperationException(
                            "Accessing WebRequestPrefixElement.Prefix failed");
                    }

                    if (prefixField.GetValue(entry) is string prefixValue && 
                        prefixValue == prefix)
                    {
                        var creatorProperty = entry
                            .GetType()
                            .GetProperty("Creator");
                        if (creatorProperty == null)
                        {
                            throw new InvalidOperationException(
                                "Accessing WebRequestPrefixElement.Creator failed");
                        }

                        if (!(creatorProperty.GetValue(entry) 
                            is IWebRequestCreate original))
                        {
                            throw new InvalidOperationException(
                                "WebRequestPrefixElement.Creator uses " +
                                "an unexpected type");

                        }

                        creatorProperty.SetValue(entry, replaceFunc(original));

                        return original;
                    }
                }

                throw new ArgumentException("Prefix is not registered");
            }
        }
    }
}

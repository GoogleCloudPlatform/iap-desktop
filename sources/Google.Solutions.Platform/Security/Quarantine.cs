//
// Copyright 2022 Google LLC
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

using Google.Solutions.Common.Threading;
using Google.Solutions.Common.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Google.Solutions.Platform.Security
{
    /// <summary>
    /// Windows Defender wrapper for scanning, quarantining, 
    /// and marking downloaded files with a MOTW (mark of the web).
    /// 
    /// For details, see Chromium source: /content/browser/download/quarantine_win.cc
    /// </summary>
    public interface IQuarantine
    {
        /// <summary>
        /// Scan a file and apply a mark-of-the-web that indicates which zone
        /// (for ex, internet) the file originated from.
        /// </summary>
        Task ScanAsync(
            IntPtr owner,
            FileInfo filePath);

        /// <summary>
        /// Scan a file and apply a mark-of-the-web that indicates which zone
        /// (for ex, internet) the file originated from.
        /// </summary>
        Task ScanAsync(
            IntPtr owner,
            FileInfo filePath,
            Uri source,
            Guid clientGuid);
    }

    public class Quarantine : IQuarantine
    {
        private const uint E_FAILED = unchecked(0x80004005);
        private const uint INET_E_SECURITY_PROBLEM = unchecked(0x800c000e);

        public static readonly Guid DefaultClientGuid =
            new Guid("79ab36ca-bdae-4c10-86ac-a0025a9c0a2d");
        public static readonly Uri DefaultSource = new Uri("about:internet");

        public Task ScanAsync(
            IntPtr owner,
            FileInfo filePath)
        {
            return ScanAsync(
                owner,
                filePath,
                DefaultSource,
                DefaultClientGuid);
        }

        public Task ScanAsync(
            IntPtr owner,
            FileInfo filePath,
            Uri source,
            Guid clientGuid)
        {
            filePath.ExpectNotNull(nameof(filePath));
            source.ExpectNotNull(nameof(source));

            Debug.Assert(source == DefaultSource ||
                source.Scheme.StartsWith("http") ||
                source.Scheme == "file");

            //
            // NB. Scanning can be slow, but needs to be done on a STA-enabled thread.
            //
            return StaTask.RunAsync(() =>
            {
                var attachment = (IAttachmentExecute)new AttachmentServices();
                try
                {
                    //
                    // Set a client GUID that identifies the current application. Might
                    // be used to persistently suppress prompts.
                    //
                    attachment.SetClientGuid(clientGuid);
                    attachment.SetLocalPath(filePath.FullName);

                    //
                    // Set source to the actual URL or the generic "internet" source.
                    // The source determines the zone, which will then be written
                    // to the MOTW.
                    //
                    attachment.SetSource(source.ToString());

                    if (owner == IntPtr.Zero)
                    {
                        attachment.Save();
                    }
                    else
                    {
                        attachment.SaveWithUI(owner);
                    }
                }
                catch (COMException e) when ((uint)e.HResult == E_FAILED)
                {
                    //
                    // Indicates that Defender (or some other AV) found an infection.
                    // Typically, this leads to the file being auto-deleted.
                    //
                    throw new QuarantineException(
                        $"The file {filePath.Name} has been blocked because it contains " +
                        "a virus or other malware");
                }
                catch (COMException e) when ((uint)e.HResult == INET_E_SECURITY_PROBLEM)
                {
                    //
                    // Indicates that a policy has blocked this download, possibly
                    // because the source has been set as "restricted".
                    //
                    throw new QuarantineException(
                        $"The file {filePath.Name} has been blocked by policy");
                }
                catch (COMException e)
                {
                    throw new QuarantineException(
                        $"The file {filePath.Name} could not be scanned for malware", e);
                }
                finally
                {
                    Marshal.ReleaseComObject(attachment);
                }
            });
        }

        /// <summary>
        /// Determine the zone a file is originating from inspecting its mark-of-the-web.
        /// </summary>
        public static Task<Zone> GetSourceZoneAsync(Uri url)
        {
            //
            // NB. Zone lookups can be slow, but need to be done on a STA-enabled thread.
            //
            return StaTask.RunAsync<Zone>(() =>
            {
                var securityManager = (IInternetSecurityManager)new InternetSecurityManager();
                try
                {
                    uint zoneId = 0;
                    securityManager.MapUrlToZone(url.ToString(), ref zoneId, 0);
                    return (Zone)zoneId;
                }
                finally
                {
                    Marshal.ReleaseComObject(securityManager);
                }
            });
        }

        public static Task<Zone> GetZoneAsync(string filePath)
        {
            return GetSourceZoneAsync(new Uri(filePath));
        }

        public enum Zone : uint
        {
            LocalMachine = 0,
            LocalIntranet = 1,
            Trusted = 2,
            Internet = 3,
            Restricted = 4
        }

        //---------------------------------------------------------------------
        // Interop classes.
        //---------------------------------------------------------------------

        [ComImport]
        [Guid("73DB1241-1E85-4581-8E4F-A81E1D0F8C57")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAttachmentExecute
        {
            int SetClientTitle(string pszTitle);

            int SetClientGuid(ref Guid guid);

            int SetLocalPath(string pszLocalPath);

            int SetFileName(string pszFileName);

            int SetSource(string pszSource);

            int SetReferrer(string pszReferrer);

            int CheckPolicy();

            int Prompt(IntPtr hwnd, ATTACHMENT_PROMPT prompt, out ATTACHMENT_ACTION paction);

            int Save();

            int Execute(IntPtr hwnd, string pszVerb, out IntPtr phProcess);

            int SaveWithUI(IntPtr hwnd);

            int ClearClientState();
        }

        [ComImport]
        [Guid("4125DD96-E03A-4103-8F70-E0597D803B9C")]
        private class AttachmentServices
        {
        }

        private enum ATTACHMENT_PROMPT
        {
            NONE = 0x0000,
            SAVE = 0x0001,
            EXEC = 0x0002,
            EXEC_OR_SAVE = 0x0003,
        }

        private enum ATTACHMENT_ACTION
        {
            CANCEL = 0x0000,
            SAVE = 0x0001,
            EXEC = 0x0002,
        }

        [ComImport]
        [Guid("79EAC9EE-BAF9-11CE-8C82-00AA004BA90B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IInternetSecurityManager
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int SetSecuritySite([In] IntPtr pSite);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetSecuritySite([Out] IntPtr pSite);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int MapUrlToZone(
                [In, MarshalAs(UnmanagedType.LPWStr)] string pwszUrl,
                ref uint pdwZone,
                uint dwFlags);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetSecurityId(
                [MarshalAs(UnmanagedType.LPWStr)] string pwszUrl,
                [MarshalAs(UnmanagedType.LPArray)] byte[] pbSecurityId,
                ref uint pcbSecurityId,
                uint dwReserved);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int ProcessUrlAction(
                [In, MarshalAs(UnmanagedType.LPWStr)] string pwszUrl,
                uint dwAction, out byte pPolicy,
                uint cbPolicy,
                byte pContext,
                uint cbContext,
                uint dwFlags,
                uint dwReserved);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int QueryCustomPolicy(
                [In, MarshalAs(UnmanagedType.LPWStr)] string pwszUrl,
                ref Guid guidKey,
                ref byte ppPolicy,
                ref uint pcbPolicy,
                ref byte pContext,
                uint cbContext,
                uint dwReserved);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int SetZoneMapping(
                uint dwZone,
                [In, MarshalAs(UnmanagedType.LPWStr)] string lpszPattern,
                uint dwFlags);

            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int GetZoneMappings(
                uint dwZone,
                out System.Runtime.InteropServices.ComTypes.IEnumString ppenumString,
                uint dwFlags);
        }

        [ComImport]
        [Guid("7b8a2d94-0ac9-11d1-896c-00c04fb6bfc4")]
        private class InternetSecurityManager
        {
        }
    }

    public class QuarantineException : Exception
    {
        public QuarantineException(string message)
            : base(message)
        {
        }

        public QuarantineException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

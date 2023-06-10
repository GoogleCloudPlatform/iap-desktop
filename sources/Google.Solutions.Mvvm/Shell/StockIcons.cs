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

using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Google.Solutions.Mvvm.Shell
{
    public static class StockIcons
    {
        /// <summary>
        /// Icon IDs, see SHSTOCKICONID.
        /// </summary>
        public enum IconId
        {
            DocNoAssoc = 0,
            DocAssoc = 1,
            Application = 2,
            Folder = 3,
            Folderopen = 4,
            Drive525 = 5,
            Drive35 = 6,
            DriveRemove = 7,
            DriveFixed = 8,
            DriveNet = 9,
            DriveNetDisabled = 10,
            DriveCd = 11,
            DriveRam = 12,
            World = 13,
            Server = 15,
            Printer = 16,
            MyNetwork = 17,
            Find = 22,
            Help = 23,
            Share = 28,
            Link = 29,
            Slowfile = 30,
            Recycler = 31,
            RecyclerFull = 32,
            MediaCdAudio = 40,
            Lock = 47,
            Autolist = 49,
            Printernet = 50,
            ServerShare = 51,
            PrinterFax = 52,
            PrinterFaxnet = 53,
            PrinterFile = 54,
            Stack = 55,
            MediaSvcd = 56,
            StuffedFolder = 57,
            DriveUnknown = 58,
            DriveDvd = 59,
            MediaDvd = 60,
            MediaDvdram = 61,
            MediaDvdrw = 62,
            MediaDvdr = 63,
            MediaDvdrom = 64,
            MediaCdAudioplus = 65,
            MediaCdrw = 66,
            MediaCdr = 67,
            MediaCdburn = 68,
            MediaBlankcd = 69,
            MediaCdrom = 70,
            AudioFiles = 71,
            ImageFiles = 72,
            VideoFiles = 73,
            MixedFiles = 74,
            FolderBack = 75,
            FolderFront = 76,
            Shield = 77,
            Warning = 78,
            Info = 79,
            Error = 80,
            Key = 81,
            Software = 82,
            Rename = 83,
            Delete = 84,
            MediaAudiodvd = 85,
            MediaMoviedvd = 86,
            MediaEnhancedcd = 87,
            MediaEnhanceddvd = 88,
            MediaHddvd = 89,
            MediaBluray = 90,
            MediaVcd = 91,
            MediaDvdplusr = 92,
            MediaDvdplusrw = 93,
            DesktoPpc = 94,
            MobilePc = 95,
            Users = 96,
            MediaSmartmedia = 97,
            MediaCompactFlash = 98,
            DeviceCellphone = 99,
            DeviceCamera = 100,
            DeviceVideoCamera = 101,
            DeviceAudioPlayer = 102,
            NetworkConnect = 103,
            Internet = 104,
            Zipfile = 105,
            Settings = 106,
            DriveHddvd = 132,
            DriveBd = 133,
            MediaHddvdrom = 134,
            MediaHddvdr = 135,
            MediaHddvdram = 136,
            MediaBdrom = 137,
            MediaBdr = 138,
            MediaBdre = 139,
            ClusteredDrive = 140
        };

        public enum IconSize : uint
        {
            Large = 0,
            Small = NativeMethods.SHGFI_SMALLICON
        }

        public static Image GetIcon(IconId iconId, IconSize size)
        {
            var iconInfo = new NativeMethods.SHSTOCKICONINFO()
            {
                cbSize = NativeMethods.SHSTOCKICONINFO.StructSize
            };

            var hr = NativeMethods.SHGetStockIconInfo(
                iconId,
                NativeMethods.SHGFI_ICON | (uint)size,
                ref iconInfo);
            if (hr != 0)
            {
                throw new COMException(
                    $"The stock icon {iconId} could not be loaded",
                    hr);
            }

            using (var icon = Icon.FromHandle(iconInfo.hIcon))
            {
                return icon.ToBitmap();
            }
        }

        //---------------------------------------------------------------------
        // P/Invoke declarations.
        //---------------------------------------------------------------------

        private static class NativeMethods
        {
            internal const uint SHGFI_ICON = 0x100;
            internal const uint SHGFI_SMALLICON = 0x1;

            private const int MAX_PATH = 260;

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct SHSTOCKICONINFO
            {
                public static uint StructSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>();

                public uint cbSize;
                public IntPtr hIcon;
                public int iSysIconIndex;
                public int iIcon;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
                public string szPath;
            }

            [DllImport("Shell32.dll", SetLastError = false)]
            internal static extern int SHGetStockIconInfo(
                IconId siid,
                uint uFlags,
                ref SHSTOCKICONINFO psii);
        }
    }
}

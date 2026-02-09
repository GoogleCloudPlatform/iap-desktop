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

using Google.Apis.Compute.v1.Data;
using Google.Solutions.Apis.Locator;
using Google.Solutions.IapDesktop.Extensions.Management.GuestOs.Inventory;
using Google.Solutions.Testing.Application.Test;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Management.Test.GuestOs.Inventory
{
    [TestFixture]
    public class TestWindowsGuestOsInfo : ApplicationFixtureBase
    {
        private static readonly InstanceLocator SampleLocator =
            new InstanceLocator("project-1", "zone-1", "instance-1");

        private static readonly List<GuestAttributesEntry> SampleAttributes =
            new List<GuestAttributesEntry>()
            {
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "Architecture",
                    Value =  "x86_64"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "Hostname",
                    Value =  "windows-1"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "InstalledPackages",
                    Value =  "H4sIAAAAAAAA/+xYb2/juPF+//sUhF7tAaZMUhRFBTjcZnd/iwZ7f4pNrkV7WBQUNbKJyKQrUs4ahwP6Nfr1+kkK"+
                        "SrYTb5zE8QYtChywyAYRZ/jM8Jlnhvw1mTk3g5Cc/fJr8qNaQHKWaOjCDGwySc47PU/Oks9S/E3wZJL8CTpvnE3OEprSlL"+
                        "ymyW+TndnG0WNWLKXFIbMWsHaLZR8Ag50ZC1j1weF+WasA3a1L61T8ZR8IS+kxHmujZtb5YLR/IjKWktfkCIedWUGHK9W2"+
                        "zj2RLZHSNHtN5dFeZzP1ZP7psxyurNGPuiRpmeavGT/apYVwvVq8cODL1VI9hfT5XlfGee3NS3ldQFC1Cgp73ZlleJxQjDBC"+
                        "KCtTQo7j/tLdQOfn0LaPMp+lR/rza7/sYPmosyyl9Lg6Wvmn6oem7Kgs3hhbu5sjkpc/ljzntbONmWE1A/uE/ERnnLCUbMT"+
                        "r0yS56dWgfVcmtNHrz4PmoMZ16M8jPvQOGrA1dOjcBrMyXe+RssEsVHujOkDLVoXGdQuE0Yc3nORMsAy92uyKeEplGgmQUv"+
                        "JNMknewciZEdHV3Hi0VPpazQDdmLZFo+Y9d+9//eOfHsX0Ogs2eOQsCnNAvYcOLZSeGwtpMkneqgAz1xnwydkvyQ9Gd867Jh"+
                        "zYJZlsMuGTTzu79cW7wVDqrNFaclxwSjBXZY5lJUusqBCK0JJwKZJJouu8aWoKGMqMYQ6ZwlVTcExl1ZCKVlUts+j9w5vz"+
                        "Lhjdwsb9Jofx02W/XLou/Pzx++QsmYew9GfT6cyliy30VLvFtLlpjb2efhd/mvpbKViWlbsILt7FnkaoYGWjseCgMM+bDFe"+
                        "FrrBWlS7qXIgyy5JJ8hFWJh7bj/2igi45Y4RMku+VD+9g2br1Amx4O1d2Bldm4GBkFCY5JvSKkLPh318Hjm7ptFmA3vaLvl"+
                        "XBrADdYVj64/9fofedWsCN665RluYTxNMiZUjZGvFU7vHwEroVdIgRWg5//yw4evXhDc9zwTm9T65z5EH3nQlrZLzvAc2V"+
                        "RxWARaYGG0xjoEbGIoVumRB/jMzqXN3rgMJcBaRd39ZINQ3ogNau75Bf+wCLFP3F9Ugri+bQLqNN+GIFqtbIWB9U2xo7Qy"+
                        "HyfUPxpnOL251T9N51SA0kbiEAao0P0cQ1A5WHCPwIJ+IzVrd9PQZwx+sEeYDBQHnvtFEB6jvhfbDupoV6BuiN8oDUyLsUn"+
                        "TcBuoh7C3bfZ/ywUGs0VytAwaEOfFDdfiruldflNvnbQpokB07yQHmRRlFGaI15lhHMGyWxVFDiShYFL7IqnnYySZqCMMWl"+
                        "xiUtK8zzWuBSQYO14ozLOidZWRwsr5EvB8vrbDr149/2S2y/mHLQBW9EiXmdK8xZVuKSZyUWTJFS1Ixwqg4WEz2qmCh7oJi"+
                        "26ftBtUYb13t0uaXrR1i4lWrRlXPtUBgYrfJUslggsiQyOyC+46FHrtTuxrZO1ZPx2EN00vU26iigYBbDoes56OvxyDeNrB"+
                        "vK0NhYFlHrqzXyS9CmMXqClh2sVAs2oMUO7q66Xo30jfx+0yofoJugS+V9/D+W/g/r2rnFN8PvsbQ86mKAgJRd39lwqAbjU"+
                        "eN6W6fookHK3vm8/TIZghyiGnpMbfyyVWukkA8q9NF5PPJhlYXPYRNydB6Zf4fr28BTdI4s3KDVpsttivR2iwqQaxrooEaw"+
                        "gm6NFs6G+QAxurxRNsScLpTtVduuY7ZvPTi7v9tYgFFmtgc1CMVyPWpItLvTy7ZL3oIdbF23M4+7KIucbY2FHfjByR7dU3S"+
                        "144HxyLqAVExSqzRE2g7nruzQisemvJHLFF25A1q4H4ifD3LaRwE65OOekmw6xkfXtv3ysI6Io9WFyUpLSQCTvGSY66rBsi"+
                        "lzrIuyokVJaZ03ySTJRQkSZIO10DXmTEtcMpVhaKqKEUUa0eRfIUJjTX6FBjHGaF1nHGdlVWHeUIGrjBFcAS8b4EpUZX5Qg"+
                        "9jXadC4gKIDc+KB/owr5aFGl0OL8EO35iWnBb+vRhf3G8+m1bh2tet/xm63+282zCG8hRu8xvFTxRge7KImwOKJHrrTlQe4"+
                        "f3zzPGro/IrmOR7fg7PpQeZOoyJMt6Z7NJaSK0EJxVlFFeZVnuEKasCskZWoGsqLgh2kcXYcjctHaSzQF0PKcLDntasAvW+"+
                        "Vn6M/tmq96XPPo3kuqCCHmu7vQ+nvQ+mhoXTgy0l1tTHdqyvKKyoIB6wllREu4IrWDWZVXcgSNC2ywyPqMe1BYPJQXd0po8"+
                        "eu1yc8IIjnPSA8e/eTnhDeQWOsGYbNW0I9+q5wn1lAClkKJrEGIjHnWYErKDhmvMwryVnDsyqZHPfo8B96TcgLrjlkHBdKc"+
                        "MxrwbAqSY0Fk1RqTjIF5OTXhOLh14Rd9V7YAG1rZmD13oPCo6ce6cWYKARht/SiaUbLVJQkPcCuB2eSlfGjoDWmvSuAfWwD"+
                        "waEaBs0d9gU/QX65jmQbLzcuzKFDSxei3g/Tf2/jhQDqndKn6KcYWZS4Qd42Ahid304UJkSVj8N5BZv7Uf0/SdHNoTyfot8"+
                        "be31Rf5szIeg+Q2smadkIgXXOKOZKF1iSusI5oRkptaQFPf29q8Ake2KuOPzedWiEeEUlKb95apKQvw/MRw7M9zr8ib38wS"+
                        "YtT2/S8ovhF2hGBVeY5lmBuVTx+hYrhwraQMMpL6sDJD3mFenLFv1pkvy9geGl/63aMuix2+b0u+vK1N/yPGdlRu6Rb4wim"+
                        "SR/cOG9+TyEMzzFblZfbAXrp7g6n1I+jcCGWnkeAF6QQsp7AL445ntIdmb7SLIpZdNhxDsBiRQ0z45NxW71PgA+peLkVGwv"+
                        "UMcB2K5+OQA544zfB/DUWezMXhBJVhR5+XwkW7MXRMLLkhfPR7I1e0Ek23vAM5FszfaRiCmlpyNhuWAnINmYvSSSjeyekJP"+
                        "76iFG9YhIPv32f/8OAAD//w5p74o8IgAA"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "KernelRelease",
                    Value =  "10.0.17763.1282"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "KernelVersion",
                    Value =  "10.0.17763.1282 (WinBuild.160101.0800)"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "LastUpdated",
                    Value =  "2020-07-03T09:10:08Z"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "LongName",
                    Value =  "Microsoft Windows Server 2019 Datacenter"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "OSConfigAgentVersion",
                    Value =  "20200402.01"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "PackageUpdates",
                    Value =  "H4sIAAAAAAAA/3ySXWvrRhCG7/srhrlqQSuvZH1DaZP4xiRNIR+9aAhlvRpJS+RdsTuya0L++0G2z7k8oCvt8Mzzzsw"+
                    "n9s71xNi8feKj2hM25z8jCRe0s53pherJMkZ44/WADf5fFf8VGUb4D/lgnMUGU5lKWSR1LGUs/0zw6z3C46zO0BfD40J9Jj17wyf"+
                    "YWqZxND1ZTfA6tYoJOufhL6O9C65j2FBHtiUPN5bNwfg5gID72zQtykKm8Ou1MSTxOqnjUiax/A0j3FDQ3kx8cdrawGocgQcTYL6"+
                    "0YQeeDiYQ8EDQmZEC8KAYlCeYA7VLRUtMmuHcl0IEYTodlacIlG3B8UAeJsdk2ahxPMFsj8oytbCoL4Ux/L0kO7kZBnUgMBeRBb6"+
                    "oGKZ9BIZBK2sdw47A094dqI0xwjvF1DtvKGDzhhvqjDVLoOugAkb4sznh+w/Eabs5M0iWVV2kldAkK5Fl61LsqMxEmtX5rsrSLlv"+
                    "vMMJKrzutq0yUWSJFpupcVLuqFiopCiWTWmZVsdDvb288Gz3SFX9dyvL0PE+T8/z69IANDsxTaFar3sX778KxdvtVdxyN/Vj98WD"+
                    "sx7b9PU+LIsEIL/m2G2wwJyrLViWiKCSJTGZa7GSZC63rPFdFlmf1Yvy0bNI4+zjvd+SxSaWM8EEF3tA0utOeLN8Nyvb0Ys5nvRy"+
                    "pkKWQ6xcpm/P3L369f/3yLQAA//8TRIRGBAMAAA=="
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "ShortName",
                    Value =  "windows"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "Version",
                    Value =  "10.0.17763"
                }
            };

        [Test]
        public void FromGuestAttributes_WhenGuestAttributesEmpty_ThenReturnsDefaults()
        {
            var attributes = GuestOsInfo.FromGuestAttributes(
                SampleLocator,
                new List<GuestAttributesEntry>());

            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.Architecture, Is.Null);
            Assert.That(attributes.KernelRelease, Is.Null);
            Assert.That(attributes.KernelVersion, Is.Null);
            Assert.That(attributes.OperatingSystem, Is.Null);
            Assert.That(attributes.OperatingSystemFullName, Is.Null);
            Assert.That(attributes.OperatingSystemVersion, Is.Null);
            Assert.That(attributes.AgentVersion, Is.Null);
            Assert.That(attributes.LastUpdated, Is.Null);
        }

        [Test]
        public void OsInfoAttributes_WhenGuestAttributesPopulated()
        {
            var attributes = GuestOsInfo.FromGuestAttributes(SampleLocator, SampleAttributes);

            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.Architecture, Is.EqualTo("x86_64"));
            Assert.That(attributes.KernelRelease, Is.EqualTo("10.0.17763.1282"));
            Assert.That(attributes.KernelVersion, Is.EqualTo("10.0.17763.1282 (WinBuild.160101.0800)"));
            Assert.That(attributes.OperatingSystem, Is.EqualTo("windows"));
            Assert.That(attributes.OperatingSystemFullName, Is.EqualTo("Microsoft Windows Server 2019 Datacenter"));
            Assert.That(attributes.OperatingSystemVersion, Is.EqualTo(new Version(10, 0, 17763)));
            Assert.That(attributes.AgentVersion, Is.EqualTo("20200402.01"));
            Assert.That(
                attributes.LastUpdated?.ToUniversalTime(), Is.EqualTo(new DateTime(2020, 7, 3, 9, 10, 8, DateTimeKind.Utc)));
        }

        [Test]
        public void InstalledPackages_WhenGuestAttributesContainInstalledPackages()
        {
            var attributes = GuestOsInfo.FromGuestAttributes(SampleLocator, SampleAttributes);

            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.InstalledPackages, Is.Not.Null);

            // Googet
            Assert.That(attributes.InstalledPackages?.GoogetPackages.Count, Is.EqualTo(16));
            Assert.That(attributes.InstalledPackages?.GoogetPackages[0].Name, Is.EqualTo("certgen"));
            Assert.That(attributes.InstalledPackages?.GoogetPackages[0].Architecture, Is.EqualTo("x86_64"));
            Assert.That(attributes.InstalledPackages?.GoogetPackages[0].Version, Is.EqualTo("1.1.0@1"));

            // Wua
            Assert.That(attributes.InstalledPackages?.WuaPackages.Count, Is.EqualTo(8));
            Assert.That(
                attributes.InstalledPackages?.WuaPackages[0].Title, Is.EqualTo("Update for Windows Defender Antivirus antimalware platform - KB4052623 (Version 4.18.2001.10)"));
            Assert.That(
                attributes.InstalledPackages?.WuaPackages[0].Description, Is.EqualTo("This package will update Windows Defender Antivirus antimalware platform’s components on the user machine."));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].Categories.Count, Is.EqualTo(2));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].Categories[0], Is.EqualTo("Microsoft Defender Antivirus"));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].Categories[1], Is.EqualTo("Updates"));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].CategoryIDs.Count, Is.EqualTo(2));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].CategoryIDs[0], Is.EqualTo("8c3fcc84-7410-4a95-8b89-a166a0190486"));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].CategoryIDs[1], Is.EqualTo("cd5ffd1e-e932-4e3a-bf74-18bf0b1bbd83"));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].KBArticleIDs.Count, Is.EqualTo(1));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].KBArticleIDs[0], Is.EqualTo("4052623"));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].SupportURL, Is.EqualTo("https://go.microsoft.com/fwlink/?linkid=862339"));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].UpdateID, Is.EqualTo("c01629fc-64ea-45f3-b7cb-cabc7d566933"));
            Assert.That(attributes.InstalledPackages?.WuaPackages[0].RevisionNumber, Is.EqualTo(200));
            Assert.That(
                attributes.InstalledPackages?.WuaPackages?[0]?.LastDeploymentChangeTime?.ToUniversalTime(), Is.EqualTo(new DateTime(2020, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc)));

            // Qfe
            Assert.That(attributes.InstalledPackages?.QfePackages.Count, Is.EqualTo(10));
            Assert.That(
                attributes.InstalledPackages?.QfePackages?[0].Caption, Is.EqualTo("http://support.microsoft.com/?kbid=4552930"));
            Assert.That(attributes.InstalledPackages?.QfePackages[0].Description, Is.EqualTo("Update"));
            Assert.That(attributes.InstalledPackages?.QfePackages[0].HotFixID, Is.EqualTo("KB4552930"));
            Assert.That(
                attributes.InstalledPackages?.QfePackages?[0]?.InstalledOn?.ToUniversalTime(), Is.EqualTo(new DateTime(2020, 5, 14, 0, 0, 0, 0, DateTimeKind.Utc)));

            Assert.That(attributes.InstalledPackages?.AllPackages.Count(), Is.EqualTo(34));
        }

        [Test]
        public void AvailablePackages_WhenGuestAttributesContainInstalledPackages()
        {
            var attributes = GuestOsInfo.FromGuestAttributes(SampleLocator, SampleAttributes);

            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.AvailablePackages, Is.Not.Null);

            // Googet
            Assert.That(attributes.AvailablePackages?.GoogetPackages.Count, Is.EqualTo(1));
            Assert.That(attributes.AvailablePackages?.GoogetPackages[0].Name, Is.EqualTo("google-osconfig-agent"));
            Assert.That(attributes.AvailablePackages?.GoogetPackages[0].Architecture, Is.EqualTo("x86_64"));
            Assert.That(attributes.AvailablePackages?.GoogetPackages[0].Version, Is.EqualTo("20200619.00.0@1"));

            // Wua
            Assert.That(attributes.AvailablePackages?.WuaPackages.Count, Is.EqualTo(1));
            Assert.That(
                attributes.AvailablePackages?.WuaPackages[0].Title, Is.EqualTo("Security Intelligence Update for Microsoft Defender Antivirus - KB2267602 (Version 1.319.701.0)"));
            Assert.That(
                attributes.AvailablePackages?.WuaPackages[0].Description, Is.EqualTo("Install this update to revise the files that are used to detect viruses, spyware, and other potentially unwanted software. Once you have installed this item, it cannot be removed."));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].Categories.Count, Is.EqualTo(2));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].Categories[0], Is.EqualTo("Definition Updates"));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].Categories[1], Is.EqualTo("Microsoft Defender Antivirus"));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].CategoryIDs.Count, Is.EqualTo(2));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].CategoryIDs[0], Is.EqualTo("e0789628-ce08-4437-be74-2495b842f43b"));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].CategoryIDs[1], Is.EqualTo("8c3fcc84-7410-4a95-8b89-a166a0190486"));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].KBArticleIDs.Count, Is.EqualTo(1));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].KBArticleIDs[0], Is.EqualTo("2267602"));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].SupportURL, Is.EqualTo("https://go.microsoft.com/fwlink/?LinkId=52661"));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].UpdateID, Is.EqualTo("5ee77da1-660e-404c-b075-cc955a64549b"));
            Assert.That(attributes.AvailablePackages?.WuaPackages[0].RevisionNumber, Is.EqualTo(200));
            Assert.That(
                attributes.AvailablePackages?.WuaPackages?[0]?.LastDeploymentChangeTime?.ToUniversalTime(), Is.EqualTo(new DateTime(2020, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc)));

            // Qfe
            Assert.That(attributes.AvailablePackages?.QfePackages.Any(), Is.False);

            Assert.That(attributes.InstalledPackages?.AllPackages.Count(), Is.EqualTo(34));
        }
    }
}

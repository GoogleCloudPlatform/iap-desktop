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
    public class TestCentOsGuestOsInfo : ApplicationFixtureBase
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
                    Value =  "centos-1"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "InstalledPackages",
                    Value =  "H4sIAAAAAAAA/6RcW280rZG+35/hq10lIKDP3120m6soq0grRZFWK4vuZnp4pw98QNueN8p/X9EzY7sHhuK1L20" +
                    "NNIc6PFX1FP980mp6+u1///n033wST789vUotRmEMsssymqc/Pv1Jd8en357e6vK5zJ/++PR3oY1c5qffnliDSizG8ulff3w" +
                    "f3nIjzNlYMX0M5eO4G0cJJii/H9md9WqQ4WaMfxRTzDJECzf+ucRsN4deZimi4ynOcY7otvDnej98lG3HFfT5EhW4uF+9WpQ" +
                    "FvkszVN0P0/3EHx9UiZvnHFOU3Q8bZWu5Br7HMKWIbp90s+xuSS6mFy+z+ys2CcEVZojef96IPjosd99GlNyP+31dLI+OzDC" +
                    "tEPP2O/DXEzQOV9cvPlc4212NFqOc4+MJznGJ3CzbaX0e/jaNWnWog263xCxHlFGCaZ17W3/7iUbZxvUpx03T4AYRXOBWWI4" +
                    "ZIQ0lpBqkvZ9vtuZsXoA1ZThvcOHf3yhb2c/QhmrEAgMHNSCh9QJJX+VruBgPq5WjgU+CYFrm/ucVn54VN+a1/x26DoILVHt" +
                    "ipAWk3Yz4Nu24zkaJcURifqyrZLstwmiJKhw68R9KDMiuul1gvb1o/HOxW3wvDSTENabEv25tznP8vDJMcIlo6L5FL+OGbTM" +
                    "zudt9TSrKuhfjb59bG5cX5qxy7ttHJ+RyljbFG+G6QiXuzeF+Eqt5J/Sy2rixY5hgGpA6c+Ra9GiSk0ByPsSvj+AqIEH9Yg4" +
                    "G3oK7hsbXm6FvJ0BmakxQ1twPnPiMFB+EeSy2GWYMMc9cKaFH9NelX0eB/jauw8DbMX56GW4Iormv9EqMyEEKbkTE06F693l" +
                    "K0QmQO2dncXG5rufCc+RGTHzmg0DqbI9L3Ni5m88zVPiC2wtzsotCBzkKtBkvaE0F8q5hWJZhFGhYhbGID2IGFIowQipSYkL" +
                    "QELIkxvmz9S11Z01Ar9x+ogMLTHKUeXJx3cpiumU+yCF9N014N7NIUW2KS4Io9RVDDUit7UmcP4b/+7zM4j92ww9MiKYvClS" +
                    "UpDt0hO9EbTtEVl1PEympHotp7cBnwC/dzWF+H/nYHcV0BtFVgzJsOu9cphwEsdQJLLCQ16MQY9Rn5Zj4+5m7VRsRv5Ni8/A" +
                    "Xn0c8+fJORB3l/BYJCTDFDh4HjmL4KeNO2+EcyhDzxMN040VnUbvKMQ5ZGaGMNKxCbFPf5+YOd0BgCdPAAhxc6uKeN8c5DiF" +
                    "eLfojt2hchiVivUty8VkXz407MdvFfJ7mJPQsRnSQenrlOmKAGS5xxlBV5Dgj2JOIt364N3/+FRIXK1RX1EpLhwTuBcsYZJa" +
                    "DXU4zOmgh2nisl+HcyeclVqPkfia3oJTxNDBejedpWe0RdYsWKYC0xtlVNgLH3LdrAqp10M7FCKH1nMQ5ERtTnPuK/+lgv3q" +
                    "iwxRXs9ypGaKepC5KzGMPhs05zsk17N5F7Bx1Qlt5kB23MaDCCG0wczJaXv30/Y1ugQFkcCvfF7oTN0cGGlx2ReUuwrs7+5Q" +
                    "zrwJrHmWrhVq0RWpcBzkjfbRmVe4/sFtHWf5AHHmrLSxIbpIa5Y8m+c9lNsso/gLCsBxTH/FqNaXgk3xDroUHmWbxCn22uAY" +
                    "e96GG27td5Xe2Psr2bRpZGsCqcIkYvaaT6B5+y/EtLlYM57esxV4ytm3wvl9m1HUKUq7obtR5UAOY6KGI1hd3XrO8/alLL3o" +
                    "eZTvLIxh3Uz/uFuxglF6GNOtGw958lK3Sy9sZtTJ+JQRnzuh72NXFkabTUtn4EhpMMlzSi9sIGXvxIjuBJq6U0Ei8iDlB2Sg" +
                    "mTl6rq6t+bvai0mverTaC1EiOcuoHAy9T3G6x7bN59uCzznYbc0RG6BcRj9ALnCmK6OVefDP21rUQRGK+sr7ICYn5yOcOyCh" +
                    "WOMclay6Oz1ngXbh3UFrOFrL9FDGGB2lJfuhJ0x140HR047dMh1pG2Z0dqICjxW2qzEVZV/Nxl/mwYrJcIb3OVgL6y3Djx51" +
                    "SJayBkIpSh3t9+Hw2xvK4GW5c5IyyLCRcZsiekzENqwPQ4rxOqNNLJOuWOUjVoPqzruI7/9zps7JG2FWhcT0l4jSCfG078jj" +
                    "cJLjYwLhf2tA9mPbOnhmhBXaizfyEv4vX78sqD2BWRvzh1yhMvo4yrqabH/Dh/yjbf1CKumWaYrdBcemk4UE4idpdCujXY8r" +
                    "F2IOMO1QXxpS+A7rUhRCf+V6cfrE+dNsIKEHxjYyy/XJC9Cd0g+72bpGKb0/QNW9zkFq88nHcIyX/PhlmDjFfnMf9JhxG+jI" +
                    "4Ut3USZ6QUqOFO0q/FKAt4DOYs/jeOWz5M7VARUaC8wC43KyIXYDRFNMmlMftjqfL8X+xbDP1vI/rf+bG+uLP10HwJMNHgsg" +
                    "JcAIZppgSlHtn/cM4+AoZnFBQqechIS94y/5Wj6pMYjx8pc7UqdUoAYgXxYUDFF7ehvejnON2rgzVv1+ktuj1CJw13c4Lewc" +
                    "m3hQwcitvXI96DzX4ao8JglniCzL3vt3+lAoKnwkurhHwDjm242mrziREFZ4p/CAMIMgoRkkD07h0HKgNOa+MmR/mbnWBBHc" +
                    "QTKbbTvUrmHUhFyyyOZOG0UHa/pB1bcgmdjIRb9009n6G10GA4scCBmJLdDqYBbrW4haJ7CRwFP0E+GWCqyZQyV7jwKrAFOc" +
                    "o9ws7q2kTfA/J/CtXnY7LSoVrX1j5CtdR3el4n7OdicfcJaYVyjyb/3qUXXwgwzRA4LGTeuUWHNpgWvqDP6mkGjkQp0eVcpT" +
                    "takCs+sAegeSAEjcB/6rMJE18LGMO0Pi5CbPEHU2Oa+Znbbuj6E6XiBE03SzAwwE9FEWZ7yoc/JRQFTvDlFQBTZ/5HB96SU3" +
                    "64N1CEkVwhRp/teo0JPgm4iSp8dV8AmA2wVXh34sR1ggtgXCPbVwlb/BWCxjgQKsO6A9U6sreqxnVXf5okr3olh7KElAW4KA" +
                    "dTnKEItvMdxYbR+BvS4/+bDqugHIhdS4wQA9oteyHlNI6xSHHs63h5fojMPUf+P7rovsoPYJcKXTBnf+PnBTIi6BZ6MN6Wew" +
                    "W1UerzagMcoksNzMFrvrquu5IRGkV80zQltd9gYqi5m3JQ+QMZPVqvk7RSFxJR0nRNr1AuSCHPuM7Zt+g17aNm84Kb5yMsD/" +
                    "egFJalj/om43YYpTkaZwxDvDi3IXKQ9yBZLjB+XsofTe6G/Syxi0HwTnBuqOIhepQt5jpK8FSuyzGIi0GAdQ6cE4xQcxDKrZ" +
                    "Tz696y6bHF1DhEhXe8LuMayor5mHi9cb2A7MsdaCceeXHdMukViuQmIddLOgXNRkhJXtA9VFyFrPVMIkkkPEaOoF6aU7IBYY" +
                    "zhBO2GCMvSdaUReMHWlta9UbiQqaLUEqu4h2skecJZj7HFWahwPqdUZKSDt/oGCA7JqFCWm0+HpjoS3P08pCgcww7J+DrjEf" +
                    "0Efr0U6xDpIK0MRiKINmInyAfVtMAzvaoU+dhErONckScNwgsYZStmqG8GMN5E8r43K+jXzqAq7Jlnfwr4UCoRXCBCxzIC3p" +
                    "0pw0X/DCgP2Ah+tWgF8AbUOdY6btDiC3lh5x/cBYxQUHi79s0gtl+gkucXWs/+2Q7705wRqZ2EaR3ktOFc5rI+c1wg1ggQpe" +
                    "wZj1KgbwTg1JKxQAtaFwGvVgot5ThCtcBHRf2CKaasxAsOfaK63iiuMF5ANEPakqqjoXJ4ifdFinDqYsmSj/o4vYMBWu0DPY" +
                    "krAYoXhNclA6H177xmDhUDyoPIWr12fw+pmT+KK5u3633mUNltvg/oRTpeeTVAHEt3eqygYDYHHm/vCY5YrrZOu+ejjxh3w9" +
                    "Loe62rJiUBW6Mus+HEndm5ICjYJfuo8CXD4d4bT/b0tSZn1mdxatNcfYPWUlbUiBpgjrb+Hne8s/rhCZhec8tR4prSOTpRlX" +
                    "2Eeq7r+5WHbctFaYNJj6t4FZMnuW2jJiDyBx48feizsPS/hCdhcqHzAFTTwJvTCAwhRWqZY2y/QdfQeUJuKZLERwyFqECuBO" +
                    "9jaIC5jMJog1WWoSsXA/YdBeklkUArV492nc8mbOwE5RecqY9wF04tWCasgiwLS7UZWDHG2m5bPxE9J7zkWBjg3SPu34fP3w" +
                    "jDam2hlCvDLjRt7W6VdwfT9Jg5w+LR2wzD9yKF/BU4lHLFyKnfQQYL0dcde5GxqFs3435QXlN5rREua4vckqdKU4isz+daY3" +
                    "H6tx3K8Mo287Dyo+SrxSza/3LoaCdizEqbtBzzOgDMrtbA2hKa1yHzSHvIHnaAq9L3uiO4+XceS+APtTcCWP1gDjYjicJ8ji" +
                    "2LuRtD5c1eM1etu/+8Afg+MKNFlKldAZudqbwMyPvWScQEdU4R7nf33hr5borAvl5aOeNUeaj7guFE8pWEUzFNXd2T5tPSWG" +
                    "UuERlaLSJdadkmKEy+2wMdo3VrZx7+NwaXGOGCC5rrLub8ONqrzoGmbNxMSMA7xKI+N0ypTApIhbpE6k/sUEvYa7vTJKG9Cp" +
                    "UhFRUq7hRfcTgd3j12tRw4MYKYye5bxsP9oARlLPo/VznvKCDZVEwxTbtqi7Trt8/7U+ilEAl/pXFwf0EKX0hqfnxxxznT70" +
                    "Jv3ANkQlddARCtEedU6sylgNXRnCJi0Ahb2v9gasWwX4EBzwS3H4cdDjngS4uAIxq417wO25419EAeZJYG8O1nwBIiUQaCV4" +
                    "mltS4FGtquPRSoPv4IbGlYt/dodzPjRWzRXt0+EDInKdiWHeepM0W4nkxXKriyoYJ6MitVaMb5T7H/otHfFJc27gVITjHDaL" +
                    "k1iW4P15tzmZc4sJaXF+i8NDKeyfQF5VOLSP8LkBTfliK/do/OSUjulVLG8FcMX80zKsawNYbTPNL+iTQvJqS3H4IV/awEV" +
                    "muB7GjQ6fjR6ksb0dhkFQvJXQruELUZ5bf2qzjxxFrsG7lnFILYwQXlzbjAud18Ibfn7xAy4vQWvYxgkWJt6eoAu8b2TXyC" +
                    "sBWu8gDsUSnl9nyNloAoyRQMnlAzm+jb2bE2PktVMrKt/Rg7hUdwAdOckwz7teb9zuYhX1d9AnqLijxJVUQKEV88DPAnFegA" +
                    "tK3+deqzB+va8A4K9CdwTsFBLNbKBbo0NGC9/woOMg5x4ESyEbATqP3B1nY1/45BL1zkNBDN+gVIr41FWo8I3KQc59SlMgx8" +
                    "zPSnxkk8DGEaSSjbGewrQOHHqVKeBOodP7Uf1jo91GCtcESsw/O3w7xKcO7Lu4LS+xi8MrL+nQKIICyzX37XQYH2S6LnQaw3" +
                    "IbzK5TZVZV/Areb/v7ZOkNcyfJB6eDmO9P6M0NankA/CTGyL68LQjiffJQQnu98m4FbJXJcBzLZA5/AnunNM3ma9ZLUsNSgO" +
                    "khTXE0L1UvIdt/VLbezM4k9VDmn7516O70QOq0yu/XD+MzMa7Jbzr14O9opEkJcTXEoay+AlMvjzycSI1tOqkN+aFHBO1rWf" +
                    "bvT02EAY2qyPXgYYTQChilIZXz7icafE0/Jov2Kto+ylV28aHd9c7IIoGX5CiGaQFo/RORDi4t8oLYGwggpSBXm9PlcOqSPE" +
                    "aj5kE6X8P7ne29FHSsovfc0RvlSDa5h2lPL2/hzUw0uw4883c80L3HkeyHCJ0w0cX1aleEH8P0L6gxniJomgBRRnpUBVynNg" +
                    "rolGn5kQT7LjT6FetkBcf4jEtX9IbxIbVc+ivklcqbZVn72SXHHTqXkufLtDawyw397VMkcZas6ybsOOlCCaeYsu1eQfY1X6" +
                    "AhmWYYY8X3RVqT7TnVukvMgrI1TDigmdRBydMv0LDSkr7GHT6BniV1sTraWc5qRmpWDtKxvO86Dbnn9ZsXtljP4Urog7W2Mc" +
                    "Lns9hbvNQsGwfX4k7zpLOCH5S/4YR0XNNQPH2RIa9vJNqwQYHa8c8jABtgHPDInm98pDn16ZxNE0uEHNv0XsLa/VjUuvAeS" +
                    "0UC14+MBqxS+2sNXrBKebQ5XwP76p7/8+b/+/Hfgalnuf/a8Rl5PT3nyxDvVcRmG750mbxPKYtFXqDaJTyG1OmTqncmnotP" +
                    "3K1jXxax6HDRvWxGpSma4wTQQU73ICU1ylhPQJRiv/1wXwvUAcOkuz0KHuHR66YDqW7Y1q+TF1ZTf03Eur2olkRPDlLp9ye" +
                    "TbT2H5z2t9Z7rZKjCOBIsv6Vy0Rw9o9scElwVjqKE/ISXf2vUA8n3eDdqe72zWHnqAvMalunEBPSR0anu0b5MO5LhDfL7zOs" +
                    "FPiD6uuGxsERg3JNBFvsN3+sFNoow35JotvddX7xQeuOuPM/y/f/3b/wcAAP//OJoeX6djAAA="
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "KernelRelease",
                    Value =  "2.6.32-754.30.2.el6.x86_64"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "KernelVersion",
                    Value =  "#1 SMP Wed Jun 10 11:14:37 UTC 2020"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "LastUpdated",
                    Value =  "2020-07-03T09:10:08Z"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "LongName",
                    Value =  "CentOS 6.10 (Final)\n"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "OSConfigAgentVersion",
                    Value =  "20200709.00-g1.el6"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "PackageUpdates",
                    Value =  "H4sIAAAAAAAA/4TOTarCMBQG0PlbxjduLknal0pmbsChExEJ5TYG8wOpolK6dweCDgTdwOHMuF8S7G7GxiWGxYlr5ogG" +
                    "6zocYXFbmYPp0GDLdQolw0KToVaL/r+jVpEijgZL8xJ8KT6yKNNQ8hi8cJ7z+SuorJZayl63JJXwH+LzJMZQ09VVflsuxt+z/fL3C" +
                    "AAA//8AHzwc5gAAAA=="
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "ShortName",
                    Value =  "centos"
                },
                new GuestAttributesEntry() {
                    Namespace__ = "guestInventory",
                    Key =  "Version",
                    Value =  "6.10"
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
        public void FromGuestAttributes_WhenGuestAttributesPopulated_ThenOsInfoAttributesAreSet()
        {
            var attributes = GuestOsInfo.FromGuestAttributes(SampleLocator, SampleAttributes);

            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.Architecture, Is.EqualTo("x86_64"));
            Assert.That(attributes.KernelRelease, Is.EqualTo("2.6.32-754.30.2.el6.x86_64"));
            Assert.That(attributes.KernelVersion, Is.EqualTo("#1 SMP Wed Jun 10 11:14:37 UTC 2020"));
            Assert.That(attributes.OperatingSystem, Is.EqualTo("centos"));
            Assert.That(attributes.OperatingSystemFullName, Is.EqualTo("CentOS 6.10 (Final)\n"));
            Assert.That(attributes.OperatingSystemVersion, Is.EqualTo(new Version(6, 10)));
            Assert.That(attributes.AgentVersion, Is.EqualTo("20200709.00-g1.el6"));
            Assert.That(
                attributes.LastUpdated?.ToUniversalTime(), Is.EqualTo(new DateTime(2020, 7, 3, 9, 10, 8, DateTimeKind.Utc)));

            Assert.That(attributes.InstalledPackages?.AllPackages.Count(), Is.EqualTo(392));
        }

        [Test]
        public void FromGuestAttributes_WhenGuestAttributesContainInstalledPackages_ThenInstalledPackagesAttributeIsSet()
        {
            var attributes = GuestOsInfo.FromGuestAttributes(SampleLocator, SampleAttributes);

            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.InstalledPackages, Is.Not.Null);

            // RPM
            Assert.That(attributes.InstalledPackages?.RpmPackages.Count, Is.EqualTo(392));
            Assert.That(attributes.InstalledPackages?.RpmPackages[0].Name, Is.EqualTo("wireless-tools"));
            Assert.That(attributes.InstalledPackages?.RpmPackages[0].Architecture, Is.EqualTo("x86_64"));
            Assert.That(attributes.InstalledPackages?.RpmPackages[0].Version, Is.EqualTo("29-6.el6"));

            Assert.That(attributes.InstalledPackages?.RpmPackages[391].Name, Is.EqualTo("pm-utils"));
            Assert.That(attributes.InstalledPackages?.RpmPackages[391].Architecture, Is.EqualTo("x86_64"));
            Assert.That(attributes.InstalledPackages?.RpmPackages[391].Version, Is.EqualTo("1.2.5-11.el6"));

            Assert.That(attributes.InstalledPackages?.AllPackages.Count(), Is.EqualTo(392));
        }

        [Test]
        public void FromGuestAttributes_WhenGuestAttributesContainInstalledPackages_ThenAvailablePackagesAttributeIsSet()
        {
            var attributes = GuestOsInfo.FromGuestAttributes(SampleLocator, SampleAttributes);

            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.AvailablePackages, Is.Not.Null);

            // Yum
            Assert.That(attributes.AvailablePackages?.YumPackages.Count, Is.EqualTo(3));
            Assert.That(attributes.AvailablePackages?.YumPackages[0].Name, Is.EqualTo("kernel"));
            Assert.That(attributes.AvailablePackages?.YumPackages[0].Architecture, Is.EqualTo("x86_64"));
            Assert.That(attributes.AvailablePackages?.YumPackages[0].Version, Is.EqualTo("2.6.32-754.31.1.el6"));

            Assert.That(attributes.AvailablePackages?.YumPackages[1].Name, Is.EqualTo("google-osconfig-agent"));
            Assert.That(attributes.AvailablePackages?.YumPackages[1].Architecture, Is.EqualTo("x86_64"));
            Assert.That(attributes.AvailablePackages?.YumPackages[1].Version, Is.EqualTo("1:20200723.01-g1.el6"));

            Assert.That(attributes.AvailablePackages?.YumPackages[2].Name, Is.EqualTo("kernel-firmware"));
            Assert.That(attributes.AvailablePackages?.YumPackages[2].Architecture, Is.EqualTo("all"));
            Assert.That(attributes.AvailablePackages?.YumPackages[2].Version, Is.EqualTo("2.6.32-754.31.1.el6"));

            Assert.That(attributes.InstalledPackages?.AllPackages.Count(), Is.EqualTo(392));
        }
    }
}

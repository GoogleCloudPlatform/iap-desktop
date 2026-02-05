//
// Copyright 2021 Google LLC
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

using Google.Solutions.Platform.Net;
using NUnit.Framework;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Platform.Test.Net
{
    [TestFixture]
    public class TestChromeCertificateSelector
    {
        private static readonly X500DistinguishedName ComplexIssuerDn =
            new X500DistinguishedName("C=US,S=CA,L=MTV,O=Acme,OU=Sales,CN=Issuer");
        private static readonly X500DistinguishedName ComplexSubjectDn =
            new X500DistinguishedName("C=US,S=CA,L=MTV,O=Acme,OU=Sales,CN=Subject");

        private static readonly X500DistinguishedName SimpleIssuerDn =
            new X500DistinguishedName("CN=Issuer");
        private static readonly X500DistinguishedName SimpleSubjectDn =
            new X500DistinguishedName("CN=Subject");

        //---------------------------------------------------------------------
        // TryParse.
        //---------------------------------------------------------------------

        [Test]
        public void TryParse_WhenJsonMalformed_ThenTryParseReturnsFalse()
        {
            Assert.That(ChromeCertificateSelector.TryParse(
                "{asd'",
                out var _), Is.False);
            Assert.That(ChromeCertificateSelector.TryParse(
               "",
               out var _), Is.False);
        }

        //---------------------------------------------------------------------
        // IsMatch.
        //---------------------------------------------------------------------

        [Test]
        public void IsMatch_WhenUrlMismatches_ThenIsMatchReturnsFalse()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://[*.]google.com/', 
                    'filter':{}
                }");

            Assert.IsNotNull(selector);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);

            Assert.That(selector.IsMatch(
                new Uri("https://www.google.de"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null), Is.False);
        }

        [Test]
        public void IsMatch_WhenUrlAndFilterEmpty_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                }");

            Assert.IsNotNull(selector!);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null));
        }

        [Test]
        public void IsMatch_WhenUrlMatchesAndFilterEmpty_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://[*.]google.com/', 
                    'filter':{}
                }");

            Assert.IsNotNull(selector);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null));
        }

        //---------------------------------------------------------------------
        // IsMatch - Issuer match.
        //---------------------------------------------------------------------

        [Test]
        public void IsMatch_WhenUrlAndIssuerMatchesBySingleField_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://[*.]google.com/', 
                    'filter':{
                        'ISSUER': {
                            'CN': 'Issuer'
                        }
                    }
                }");

            Assert.IsNotNull(selector);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNotNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                ComplexIssuerDn,
                ComplexSubjectDn,
                null));

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                ComplexSubjectDn,
                null));
        }

        [Test]
        public void IsMatch_WhenUrlAndIssuerMatchByAllFields_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://[*.]google.com/', 
                    'filter':{
                        'ISSUER': {
                            'CN': 'Issuer',
                            'C': 'US',
                            'S': 'CA',
                            'L': 'MTV',
                            'O': 'Acme',
                            'OU': 'Sales'
                        }
                    }
                }");

            Assert.IsNotNull(selector);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNotNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                ComplexIssuerDn,
                SimpleSubjectDn,
                null));

            Assert.That(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null), Is.False);
        }

        [Test]
        public void IsMatch_WhenUrlAndIssuerMatchButSubjectMismatches_ThenIsMatchReturnsFalse()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://[*.]google.com/', 
                    'filter':{
                        'ISSUER': {
                            'CN': 'Issuer'
                        },
                        'SUBJECT': {
                            'CN': 'Xxx'
                        }
                    }
                }");

            Assert.IsNotNull(selector);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNotNull(selector.Filter.Issuer);
            Assert.IsNotNull(selector.Filter.Subject);

            Assert.That(selector.IsMatch(
                new Uri("https://www.google.com"),
                ComplexIssuerDn,
                ComplexSubjectDn,
                null), Is.False);
        }

        //---------------------------------------------------------------------
        // IsMatch - Subject match.
        //---------------------------------------------------------------------

        [Test]
        public void IsMatch_WhenUrlAndSubjectMatchesBySingleField_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://[*.]google.com/', 
                    'filter':{
                        'SUBJECT': {
                            'CN': 'Subject'
                        }
                    }
                }");

            Assert.IsNotNull(selector);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNotNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null));

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                ComplexSubjectDn,
                null));
        }

        [Test]
        public void IsMatch_WhenUrlAndSubjectMatchByAllFields_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://[*.]google.com/', 
                    'filter':{
                        'SUBJECT': {
                            'CN': 'Subject',
                            'C': 'US',
                            'S': 'CA',
                            'L': 'MTV',
                            'O': 'Acme',
                            'OU': 'Sales'
                        }
                    }
                }");

            Assert.IsNotNull(selector);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNotNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                ComplexSubjectDn,
                null));

            Assert.That(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null), Is.False);
        }

        //---------------------------------------------------------------------
        // IsMatch - Thumbprint match.
        //---------------------------------------------------------------------

        [Test]
        public void IsMatch_WhenThumbprintMatches_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://[*.]google.com/', 
                    'filter':{
                        'THUMBPRINT': 'abcd'
                    }
                }");

            Assert.IsNotNull(selector);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);
            Assert.IsNotNull(selector.Filter.Thumbprint);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                "ABCD"));

            Assert.That(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                "0123"), Is.False);
        }

        [Test]
        public void IsMatch_WhenThumbprintMatchesButSubjectDoesnt_ThenIsMatchReturnsFalse()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://[*.]google.com/', 
                    'filter':{
                        'THUMBPRINT': 'abcd',
                        'SUBJECT': {
                            'CN': 'Somethingelse'
                        }
                    }
                }");

            Assert.IsNotNull(selector);
            Assert.IsNotNull(selector!.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNotNull(selector.Filter.Subject);
            Assert.IsNotNull(selector.Filter.Thumbprint);

            Assert.That(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                ComplexSubjectDn,
                "abcd"), Is.False);
        }
    }
}

﻿using Google.Solutions.IapDesktop.Application.Services.SecureConnect;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Application.Test.Services.SecureConnect
{
    [TestFixture]
    public class TestChromeCertificateSelector : ApplicationFixtureBase
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
        // URL match.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUrlMismatches_ThenIsMatchReturnsFalse()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://*.google.com/', 
                    'filter':{}
                }");

            Assert.IsNotNull(selector.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);

            Assert.IsFalse(selector.IsMatch(
                new Uri("https://www.google.de"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null));
        }

        [Test]
        public void WhenUrlAndFilterEmpty_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                }");

            Assert.IsNotNull(selector.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null));
        }

        [Test]
        public void WhenUrlMatchesAndFilterEmpty_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://*.google.com/', 
                    'filter':{}
                }");

            Assert.IsNotNull(selector.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null));
        }

        //---------------------------------------------------------------------
        // Issuer match.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUrlAndIssuerMatchesBySingleField_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://*.google.com/', 
                    'filter':{
                        'ISSUER': {
                            'CN': 'Issuer'
                        }
                    }
                }");

            Assert.IsNotNull(selector.Pattern);
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
        public void WhenUrlAndIssuerMatchByAllFields_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://*.google.com/', 
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

            Assert.IsNotNull(selector.Pattern);
            Assert.IsNotNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                ComplexIssuerDn,
                SimpleSubjectDn,
                null));

            Assert.IsFalse(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null));
        }

        [Test]
        public void WhenUrlAndIssuerMatchButSubjectMismatches_ThenIsMatchReturnsFalse()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://*.google.com/', 
                    'filter':{
                        'ISSUER': {
                            'CN': 'Issuer'
                        },
                        'SUBJECT': {
                            'CN': 'Xxx'
                        }
                    }
                }");

            Assert.IsNotNull(selector.Pattern);
            Assert.IsNotNull(selector.Filter.Issuer);
            Assert.IsNotNull(selector.Filter.Subject);

            Assert.IsFalse(selector.IsMatch(
                new Uri("https://www.google.com"),
                ComplexIssuerDn,
                ComplexSubjectDn,
                null));
        }

        //---------------------------------------------------------------------
        // Subject match.
        //---------------------------------------------------------------------

        [Test]
        public void WhenUrlAndSubjectMatchesBySingleField_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://*.google.com/', 
                    'filter':{
                        'SUBJECT': {
                            'CN': 'Subject'
                        }
                    }
                }");

            Assert.IsNotNull(selector.Pattern);
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
        public void WhenUrlAndSubjectMatchByAllFields_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://*.google.com/', 
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

            Assert.IsNotNull(selector.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNotNull(selector.Filter.Subject);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                ComplexSubjectDn,
                null));

            Assert.IsFalse(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                null));
        }

        //---------------------------------------------------------------------
        // Thumbprint match.
        //---------------------------------------------------------------------

        [Test]
        public void WhenThumbprintMatches_ThenIsMatchReturnsTrue()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://*.google.com/', 
                    'filter':{
                        'THUMBPRINT': 'abcd'
                    }
                }");

            Assert.IsNotNull(selector.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNull(selector.Filter.Subject);
            Assert.IsNotNull(selector.Filter.Thumbprint);

            Assert.IsTrue(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                "ABCD"));

            Assert.IsFalse(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                SimpleSubjectDn,
                "0123"));
        }

        [Test]
        public void WhenThumbprintMatchesButSubjectDoesnt_ThenIsMatchReturnsFalse()
        {
            var selector = ChromeCertificateSelector.Parse(
                @"{
                    'pattern': 'https://*.google.com/', 
                    'filter':{
                        'THUMBPRINT': 'abcd',
                        'SUBJECT': {
                            'CN': 'Somethingelse'
                        }
                    }
                }");

            Assert.IsNotNull(selector.Pattern);
            Assert.IsNull(selector.Filter.Issuer);
            Assert.IsNotNull(selector.Filter.Subject);
            Assert.IsNotNull(selector.Filter.Thumbprint);

            Assert.IsFalse(selector.IsMatch(
                new Uri("https://www.google.com"),
                SimpleIssuerDn,
                ComplexSubjectDn,
                "abcd"));
        }
    }
}

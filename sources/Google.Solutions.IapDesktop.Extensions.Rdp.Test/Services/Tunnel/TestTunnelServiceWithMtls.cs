
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

using Google.Apis.Auth.OAuth2;
using Google.Solutions.Common.Auth;
using Google.Solutions.Common.Locator;
using Google.Solutions.Common.Test;
using Google.Solutions.Common.Test.Integration;
using Google.Solutions.IapDesktop.Application.Services.Adapters;
using Google.Solutions.IapDesktop.Extensions.Rdp.Services.Tunnel;
using Google.Solutions.IapTunneling.Iap;
using Moq;
using NUnit.Framework;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Google.Solutions.IapDesktop.Extensions.Rdp.Test.Services.Tunnel
{
    [TestFixture]
    [Category("IntegrationTest")]
    [Category("SecureConnect")]
    public class TestTunnelServiceWithMtls : FixtureBase
    {
        private IAuthorizationAdapter CreateAuthorizationAdapter(
            ICredential credential,
            IDeviceEnrollment enrollment)
        {
            var authz = new Mock<IAuthorization>();
            authz.SetupGet(a => a.Credential).Returns(credential);

            var adapter = new Mock<IAuthorizationAdapter>();
            adapter.SetupGet(a => a.Authorization).Returns(authz.Object);
            adapter.SetupGet(a => a.DeviceEnrollment).Returns(enrollment);

            return adapter.Object;
        }

        [Test]
        public async Task WhenUsingExpiredClientCertificate_ThenProbeThrowsUnauthorizedException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Enrolled);
            enrollment.SetupGet(e => e.Certificate).Returns(ExpiredCertitficate);

            var service = new TunnelService(CreateAuthorizationAdapter(
                await credential,
                enrollment.Object));

            var destination = new TunnelDestination(
                await testInstance,
                3389);

            var tunnel = await service.CreateTunnelAsync(
                destination,
                new SameProcessRelayPolicy());

            Assert.IsTrue(tunnel.IsMutualTlsEnabled);
            AssertEx.ThrowsAggregateException<UnauthorizedException>(
                () => tunnel.Probe(TimeSpan.FromSeconds(20)).Wait());

            tunnel.Close();
        }

        [Test]
        public async Task WhenUsingClientCertificateWithWrongSubject_ThenProbeThrowsUnauthorizedException(
            [WindowsInstance] ResourceTask<InstanceLocator> testInstance,
            [Credential(Role = PredefinedRole.IapTunnelUser)] ResourceTask<ICredential> credential)
        {
            var enrollment = new Mock<IDeviceEnrollment>();
            enrollment.SetupGet(e => e.State).Returns(DeviceEnrollmentState.Enrolled);
            enrollment.SetupGet(e => e.Certificate).Returns(CertitficateWithWrongSubject);

            var service = new TunnelService(CreateAuthorizationAdapter(
                await credential,
                enrollment.Object));

            var destination = new TunnelDestination(
                await testInstance,
                3389);

            var tunnel = await service.CreateTunnelAsync(
                destination,
                new SameProcessRelayPolicy());

            Assert.IsTrue(tunnel.IsMutualTlsEnabled);
            AssertEx.ThrowsAggregateException<UnauthorizedException>(
                () => tunnel.Probe(TimeSpan.FromSeconds(20)).Wait());

            tunnel.Close();
        }

        //
        // Self-signed certificate, created using:
        //   New-SelfSignedCertificate -Subject"Google Endpoint Verification" `
        //     -CertStoreLocation Cert:\CurrentUser\My\ -NotAfter <today>
        // then exported as PFX, and base64-encoded:
        //   & certutil.exe -encode .\cert.pfx cert.pfx.txt
        //
        private static readonly X509Certificate2 ExpiredCertitficate =
            new X509Certificate2(Convert.FromBase64String(
                    @"MIIJ6gIBAzCCCaYGCSqGSIb3DQEHAaCCCZcEggmTMIIJjzCCBgAGCSqGSIb3DQEH
                    AaCCBfEEggXtMIIF6TCCBeUGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcN
                    AQwBAzAOBAg0npQ+qMsc8AICB9AEggTYZmpOMfM7qfgGl5jxrkaUQGeHJybJ6vkt
                    olLJSFo0lvmP+2AzdcBk1zJAdxgmZveLh7N1ULCoM98Opf5Dl/zqEwtviYDtG0Kq
                    ArC5rAGcNfu1XjZhb+aDR2YJPFM0hkCpKekE15IUpogJNs7ZjmtWCIt19U66FjyV
                    QdZSSXIVYoxQ8HPBXK6vZjGLxetvbIuFD3CkZfr/oEuisThdwkIAbcSAr5PVbAXJ
                    sUXT5Iurow0Bdl8x7vhJcyp9caVhRY/NH5ttyN/C/2AbyFwip09kj2+QZJ4NSxNl
                    7gTdy4yFnrK6qz2ChrOIfOgwM+3T4iNhs6PDvSmfA6LqX0Vut8LTJ8noMyK4f2lZ
                    ih7B2nPQbXXyFERh4rQFIC1oYnQLKGOIJCVU2w550YKpwj+BrxoYwRg0CtcwfcMV
                    Rmq8goOG4qzUVo1fWrI7dfMAbkxqi7YgzSITJoDLM0hQkDfRIHFDEqvVkm2BsOgw
                    +/QvcibDkgaa8gy9EHjMCNIdyUrE9yD73rG268AL6Zz38fZNjjBp9qdHeMYPjBn7
                    OvNlJ39f5+Ss2BQWW0xHwQFfH0joachrxJsxZ7D1GdoJAAp2tgqT1NnQ1xfn9GpN
                    ybXtSyXreulKNt2bpVNu/XmheQrESWSiiUeP4gewufgzZBVZIP2dElkSxZgQwN8F
                    W5VlonPSLtk1DVuLW9YAD+UqMHl5e+tnpwXkNtZ61RhsNEjl7Ij1kDfdqzeIjnES
                    AcaM9sTqUysJ/J1E1WX7bLef+lY/K8CTcbfl2hLy2mILiR+NcIPChP+7gMzMIo7E
                    NEqKrtjpwj7CMPgePHjwdLTXK+i6fGBPX7Id0sg2LzABT9cmK7q3iC9JuvYrDU3e
                    gugWqPGfRcYDvnEYib2SLkcpHN2VobDgn2VNziMUsK02XaSaZQmb3IcgdFNX4YOc
                    czaOnQjuvd6EmUL2vv0kB2tGlKNzynoSSqrHS172YDz7f4ixoYXxaQyDkVDXSN0+
                    ZrW/4ePlD9gMiAXWJ4gl4w9Tpi5GUax4BIW+OT52/84hm/7CXfr0jX04eXWapYhO
                    TSxePap5WsoBLosXV6l0/MeA+gzsG6z0V3EVu6hkkSEc7HAzTRat3HfwXDmGM2Pr
                    aYtnziXT02n+WLC85mea/g143ZxFcY7Lh6c9jmyVp61ueq6E8pidQOlNf0ksRYdJ
                    9jua+lK8QtO99YprTITdTNmHFVua7tklMHatHamhRjJ6Xq9geqPFp9wCNGpKUvBj
                    8MP+W7iTce8H1DOszXlT5fQBUBUm+CnmPANZynGnxC7yxa6Ax571djGI35SCxfpv
                    ol059FQE7AVB5J0O2/zB1K54z2wTVPb/uilgEsOWR6kQxTnwU3vJYenM2VwZJ3KQ
                    o1oNUavc2riye8Tze4PsfgkWmDMtXmIuiuxP0j2PjP2mvDKUjgxigWeavA27eg8I
                    eH1KKFqXISV+GUbdbxu1jyiiUlP+cv3cBE02rkL5KWW0SAh1rb9yAnn1bbMogsbN
                    u0ws9QbVPS7Epvy8rJ777tPgh8A6hyrThSXwptY1lDbtUlPaOaOYNhwK8bAHSSK1
                    zGQ+Hm7ZAd5ef5y7AwC5+lUCZRypSOV7AVL1YALo1HVOversM0fxOFGB/kPn+yCj
                    X/F2KE5TZdt01OwqvL4I3DGB0zATBgkqhkiG9w0BCRUxBgQEAQAAADBdBgkqhkiG
                    9w0BCRQxUB5OAHQAZQAtAGEAOQBiAGYANAA2ADMAYwAtADcAMgBjADAALQA0ADgA
                    ZgBkAC0AOAA2AGUAOAAtADgAMAA4ADEAYQA4ADgAYgBmAGEAMwA3MF0GCSsGAQQB
                    gjcRATFQHk4ATQBpAGMAcgBvAHMAbwBmAHQAIABTAG8AZgB0AHcAYQByAGUAIABL
                    AGUAeQAgAFMAdABvAHIAYQBnAGUAIABQAHIAbwB2AGkAZABlAHIwggOHBgkqhkiG
                    9w0BBwGgggN4BIIDdDCCA3AwggNsBgsqhkiG9w0BDAoBA6CCA0QwggNABgoqhkiG
                    9w0BCRYBoIIDMASCAywwggMoMIICEKADAgECAhAW+hVAdm2fq0ESQ7Mcq1SOMA0G
                    CSqGSIb3DQEBCwUAMCcxJTAjBgNVBAMMHEdvb2dsZSBFbmRwb2ludCBWZXJpZmlj
                    YXRpb24wHhcNMjAxMDE2MTIwNjIwWhcNMjAxMDE2MjIwMDAwWjAnMSUwIwYDVQQD
                    DBxHb29nbGUgRW5kcG9pbnQgVmVyaWZpY2F0aW9uMIIBIjANBgkqhkiG9w0BAQEF
                    AAOCAQ8AMIIBCgKCAQEAw8tXZWFuhizgOfLdOkIDVVTABSs68cXKX9wB+AoRb/Jr
                    UwHG5+XV+Y/f6a/MNXCXjwUNfC5uBaQRqvBQKR4LEouHoCX9Cb8pTUf+PR/R9OyB
                    3XHblxPSKMk6aJVW011vMoV7K9WaG59nehaj2xmwKD3PNh57iWD7W4tfzsgawxjq
                    wyrhevGDpmZwXGcHs+HaVDWhuje1QWXI/gMmU8K6a6yK3OlEpHnIO6L86csw+tSk
                    gkK0tiXqjsf0AQgxlhi9YfkNMUXYAlwojxeLBn8/4+tq7hWlFILorme5BIgUlWzn
                    EJ5IlRLmPGXjwGq31IDlZEYocbjF0ov6BFvK+yEgfQIDAQABo1AwTjAOBgNVHQ8B
                    Af8EBAMCBaAwHQYDVR0lBBYwFAYIKwYBBQUHAwIGCCsGAQUFBwMBMB0GA1UdDgQW
                    BBS0v5KSQNBw+NmRhOgwV6pD1NcmCTANBgkqhkiG9w0BAQsFAAOCAQEAcYg3GhKz
                    N4vY4y+gE5YIf9XW6fHS+J2zMpE84Eapnh1Yfg3wnJCIcljUIUwMYOzlKnDe5CIp
                    9eanp2aTZ83IXJqud6WAO0cDfOXI2zVokOA5Q7Sf8MnxLUJMwtvucCF3HZaDNs8N
                    k9DHz/rtzLqyl9TnJc6/KiDn8DJF68ns5dJpcgI6c+T5zV4Uhd/WHFOHCx2Tj0hg
                    S5A7xIeuodZ0BBQlhQ4KcMpGz4w36isAGgs7YrwDYcmHf2kGuE7Anmmr2xwlV07l
                    /1sfr9ML3sNPWodiY94RdZzmZZf9sH4Vk8siaNIfQjhdsbMszAkjLH/zTmLWuHIw
                    Dyb1ZfC7ei6RCzEVMBMGCSqGSIb3DQEJFTEGBAQBAAAAMDswHzAHBgUrDgMCGgQU
                    VpjWeWprnLZgXPKtJ2V4ZfT+Sp8EFKpKtfvVobb2OJbsTX48yviR/bcxAgIH0A=="),
                "password");


        //
        // Self-signed certificate, created using:
        //   New-SelfSignedCertificate -Subject"Not Google Endpoint Verification" `
        //     -CertStoreLocation Cert:\CurrentUser\My\ -NotAfter 1/1/2030
        // then exported as PFX, and base64-encoded:
        //   & certutil.exe -encode .\cert.pfx cert.pfx.txt
        //
        private static readonly X509Certificate2 CertitficateWithWrongSubject =
            new X509Certificate2(Convert.FromBase64String(
                    @"MIIJ8gIBAzCCCa4GCSqGSIb3DQEHAaCCCZ8EggmbMIIJlzCCBgAGCSqGSIb3DQEH
                    AaCCBfEEggXtMIIF6TCCBeUGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYKKoZIhvcN
                    AQwBAzAOBAhERPKx21T+ewICB9AEggTYfnrMVM2KIZC49CVszrKgwvQ31svCSpQb
                    4oyrREefrB2EcslNi+ARVZ7mVa6Uy2m1PAsTD4j9Ym7MwWPMWPhkq/KKKmzt/xms
                    McDksBEw3EmWpjViKknx2OJONn2bYK0oH9B19w/Y3Ta6JyoJ5xIU7jsT46m1eHvo
                    RQVfV7z7EYMiC6XsmZZtutrsBJ+gAW6aV2fJUk01c2l3qxuZzQ2vHFlOhOLFfMJO
                    b6rjP5XYdg4cSPE20MZ2BhyvvY5vYAaVCkoDoatk9dU7vV2mn583nI46Vch/t6j4
                    sFYoPUthA+IixnrotjKyepGbhQQOGCLh33gLZBo4ijE/IdxDcGK/QQbfumporE9e
                    iIm6EXo8ENz1BjM2B6GX1Zk6tf8vaZUO1pTthBlu4pHZmz+KEpS4p5hvhUMxKV+l
                    /K+6xCxanX0Eo9abG8WU6PjwjxqlXDdolFEX1lshBrTfgJ9FAkTiLy3oQpZ5DN0K
                    8W74TxNXcCKJIixucEomuETs/KMDiCa43RE61mHnTtybmKH54zNlM6MTrqWNji9p
                    yv/kScdxBBFib9qR2pElYdZEVI9TJeRlaTLRpVjN9cY0zY+KmzIJN6R76Ze7ITBu
                    9VYtfdM8edgvDZR+zUCJs+ZZbvVs1REdrmAKoArZyWagILiGSx19of/qqklWUVJ0
                    lyaOdCYA2LS934Yzb/7uOv360Hjq+54wt1+GCdvpRw5KddWQnPiwGdhGMaC+/syc
                    HPlPhisr7O/6R42e17Oaqh6hOi41CjVxknNUzaMFUTZlTCpvXVUUqBf91Cw+m84h
                    rqb6EEj931wJxK9uZORMmrjyreb2i5RRizNZz3K/oWjIjRc9VU502GkLpRINMbdR
                    V+m6QJzbQOWHnpfR+aZtqmJhkx+H/lz0uBb7GMjijjs84w+l9C7hLTkN8mQODgZ8
                    kqnPVtZJ9TOmbwWf2V1o09WsNvOYZuxq3QbitVdQP+7jTEZGHKNDIsS5Xwxf6xT1
                    Gdbj9K+EDdXg1aWvltmlCYXjd3pT5IMeTbOXbTDp3tlRAFUFJRfN895uzOGCjThZ
                    gUjwEnpY3yOWRHaj5gbtxNVOftKkc4eDD6ulBy46mYrLse15NA/fVOvln+s9LGtQ
                    kxzWDSBuE9IKnb/SOiDhVVwZCUQqd4bXPuMNvEPIbIjmZWjQKgFlqxVWQk52mFv3
                    pqrykRnWD5cDOsVIgjjVE1i4ncjtgvGFpj8WYeAuSYJvyOIOcqd8UQ6bPZI2hfKx
                    Y9Szc74zWXTdXs9ANI5Q/sTtuJH/fLkqCGYyG0N5PeOcH9p1S/poRP7L39SxaQ+M
                    Bflf6du9mnXB2afCz5LcgSIWpc8nJiDDfmcUasM/dlEIprKoObzD0Oy+n9T4Zn0d
                    uyq8xyL1m1OkbeuCizGQ+lg/+sU8n5XVydnQp9LlkTS+LHW8DWn2oGI3IWv/v/ve
                    /RNcSb6bd4p3ngAr8wNZJJsyj2MknzcWGqDAm9LJbLkS52jlVp/ofufk70+0i+M8
                    /WF0SLAFIgHxu3NPFZUYnATFcur9Rn2JElmNpPxFtdSzq5T1O5qRY9T9KTW4jp7I
                    9PQw3Ndaq9OK4eH2mPoGb8+ZLUXGipeTpPJCgpLv+hCeoewAfkufN1Rg7J4f/0Vx
                    FFBGTfx0tN1lgyhaftSTTjGB0zATBgkqhkiG9w0BCRUxBgQEAQAAADBdBgkqhkiG
                    9w0BCRQxUB5OAHQAZQAtAGMAMAA2AGMAYwAxAGEAYgAtAGUANAA1ADAALQA0ADIA
                    OQBhAC0AOQBmADQAZAAtAGEANgAxADgAMQBjADkAYQBkADYAZAAyMF0GCSsGAQQB
                    gjcRATFQHk4ATQBpAGMAcgBvAHMAbwBmAHQAIABTAG8AZgB0AHcAYQByAGUAIABL
                    AGUAeQAgAFMAdABvAHIAYQBnAGUAIABQAHIAbwB2AGkAZABlAHIwggOPBgkqhkiG
                    9w0BBwGgggOABIIDfDCCA3gwggN0BgsqhkiG9w0BDAoBA6CCA0wwggNIBgoqhkiG
                    9w0BCRYBoIIDOASCAzQwggMwMIICGKADAgECAhAT6Whe3dvotUtE+C39hatbMA0G
                    CSqGSIb3DQEBCwUAMCsxKTAnBgNVBAMMIE5vdCBHb29nbGUgRW5kcG9pbnQgVmVy
                    aWZpY2F0aW9uMB4XDTIwMTAxNjEyMDYzOFoXDTI5MTIzMTIyMDAwMFowKzEpMCcG
                    A1UEAwwgTm90IEdvb2dsZSBFbmRwb2ludCBWZXJpZmljYXRpb24wggEiMA0GCSqG
                    SIb3DQEBAQUAA4IBDwAwggEKAoIBAQCqvGKp3kSsiYQ7iX8ExnTG0yCrX7BFXM57
                    TNzs4NbkeT6TPsyEzQGYjUhnB4vAkF3CvxZ3n2ZeuQkMoICskLQHFWrDrKJv2VCY
                    jKm+Iz8oUD805YdSh0/IPLDcsSJ6AQVrOCofu8sx+DDwYcfR3juodNeLiSP4DVbu
                    +/Myh5y4OIbpGnvDQtvFKcj5BnbvgWSGHt+Vg8fhwykvuXu9OSA2lw19GSdte/Rq
                    2DCJhIZXxIq5roquTgM1RJihs7B+ICFlx/WAwj+i0RFN8O8Ju7vDYZlroMFu56vt
                    WHt7bkO6rVNcwZh3XEDGcj4I0AkjRRAtqG5UOlumQmOh++s8U/i9AgMBAAGjUDBO
                    MA4GA1UdDwEB/wQEAwIFoDAdBgNVHSUEFjAUBggrBgEFBQcDAgYIKwYBBQUHAwEw
                    HQYDVR0OBBYEFNNecBq+DfPuv2czpwYn3hJ0L4SIMA0GCSqGSIb3DQEBCwUAA4IB
                    AQBTocGFXReRXJkxWOHyhIrBh6jK2YruA9vGPWJ4FPDmgsFqoRi96lMlrs+70gdt
                    z7RX1QT5rS28BFYJiyEu+tbNKEFswGiDr1Kr+5fhRyG9Yr3n0rSqmrQh1/EQfrzi
                    uysYdDAmsF4XEAW2ab0GjlxmxXoqzLfhMntU4J3yekuTg9tjYU42hvXuk5lOjVgw
                    FSmfasecAm9/UyGrd+r0SCnbWOZUTcipee2V3ED6tpX/btLHUxuOF7ombS6SqE8G
                    sEVkJ2STY2QgraWrsIJ8YVVjfIN7XutRODzsEBEODZYikH5DRISBYV4fyuTdsuB6
                    oUvWZs38N+HMMZwQWdZQNCtYMRUwEwYJKoZIhvcNAQkVMQYEBAEAAAAwOzAfMAcG
                    BSsOAwIaBBSbjGgjw1FtOYUl3lFCIr7t1dCmJQQU5/a48mkx5xnfqt2VOR0XaZHl
                    XJICAgfQ"),
                "password");
    }
}

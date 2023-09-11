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

using Google.Solutions.Ssh.Cryptography;
using NUnit.Framework;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Google.Solutions.Ssh.Test.Cryptography
{
    [TestFixture]
    public class TestEcdsaSshKeyPair
    {
        [Test]
        public void WhenKeyUsesNistp256_ThenPublicKeyStringMatchesResultOfSshKeyGen()
        {
            //
            // Generated with OpenSSL:
            // openssl ecparam -name prime256v1 -genkey -noout -out private-key.pem
            // openssl ec -in private-key.pem -pubout -out public-key.pem
            // openssl req -new -x509 -key private-key.pem -out cert.pem -days 360
            // openssl pkcs12 -export -inkey private-key.pem -in cert.pem -out cert.pfx
            // base64 cert.pfx
            //
            // NB. secp256r1 is called prime256v1 in openssl.
            //
            var certificateWithNistp256Key = new X509Certificate2(
                Convert.FromBase64String(@"
                    MIID0gIBAzCCA5gGCSqGSIb3DQEHAaCCA4kEggOFMIIDgTCCAncGCSqGSIb3DQEHBqCCAmgwggJk
                    AgEAMIICXQYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQYwDgQIq5h5zq6KKesCAggAgIICMFweqN3r
                    9PTqxsB7HCWwUlWVMeHyUSPXa9SEqYTBTOcOXNi/oUhSVvaqSgqHjrexld79c/bHhL91VHBpYLJV
                    +Oos/USLLq8I1/a34QJRvk7pGN2jc3TbvBEQtMQJIJq9caOeJ3B+tKPOrtAhAO3UHD/j4UIES0Hi
                    sEIzMiJG+CVtuDMc8PpsauTxMlYPXCYNZSZLkdyRaRRldtFoujPS+hPRCTaxgt7XamSsBbB6QokH
                    qaznEWu/kNsg/wWhSdXB5RE/gsFEW9sTG6MlT9AxEWrOlDHd9w/RbBY1oou5IOn/urRjHFwSPvqm
                    HuThYkVD1GUgpXZ6TWxvnnmkiUYkFEjvOPeSZu+6YBtL+aQjtAMwQB4hEk3P54r6Ivqytk5gDxu3
                    3Edckfce6ucTC86VoGcIjX133Hmnqirc83JF4JV8bXXPIkb7WCKCA4AEc8dpiNzFGVU8+6jQSdlx
                    7V+QOjMvwx5AKbQ0MvOXU9BCkE3QgvpM+NPHvpeAYLernRpXNawzUmhuo9876PNVAcQx90eIn9pt
                    tUyHQIfQF1+siTHG2C1V0ib2YqiujmrPNaGhECMdrvm0cD4M76uWNB+oLnWpEj2xk5XY4DmFj4JM
                    x7GA8Cjza37QcTs7t1jOdBrZXqSGBUWJ3y9aR1B8lP/2WjzKt1myHra3H7GG3LTpD6VlHK8M3gV1
                    fvwMFFzItpN4AfPRWWVPFWvMH2iCFh4PuoqGn5BjGrL90rtG9BAiyWffMIIBAgYJKoZIhvcNAQcB
                    oIH0BIHxMIHuMIHrBgsqhkiG9w0BDAoBAqCBtDCBsTAcBgoqhkiG9w0BDAEDMA4ECFXNlQuLfaJw
                    AgIIAASBkDYCBO4S2wW69p1Fu9sep6flR8SSRi7KQln3AK6V7NtnjJwBhxSSQb4bhrdERB6MBXQB
                    v/mpaexZzcMMjy5hHcXUiidaT0YgA9Ph9hUMnfl/ajcbW3eOBiii4Jn0Nl+igzeca/QAjyaZ+6mX
                    7rnQt4Fwa0QYh1q/DHCQkkM7kJFG93Hrxn62uGqAWrpLuZ58gjElMCMGCSqGSIb3DQEJFTEWBBRv
                    7feV2Btm4tHzFE4Am14YeaXPzjAxMCEwCQYFKw4DAhoFAAQUCaN1jyLGS72I/aw43GjceYGgghcE
                    CBrZPfwJBee4AgIIAA=="),
                string.Empty);

            //
            // ssh-keygen -f public-key.pem  -i -m PKCS8
            //
            var openSshKeyForNistp256Key = "AAAAE2VjZHNhLXNoYTItbmlzdHAyNTYAAAAIbmlzdHAyNTYAAABB" +
                "BC1j0bi/xsx91tP4NBBGmk+sD2iMBuyIp1KeQURwF6EJn6BOrS+tibpSyH947LpeJnw89pUHMNmFeL7" +
                "gmVLqf0g=";

            using (var key = ECDsaSshKeyPair.FromKey((ECDsaCng)certificateWithNistp256Key.GetECDsaPrivateKey()))
            {
                Assert.AreEqual(
                    openSshKeyForNistp256Key,
                    key.PublicKeyString);
            }
        }

        [Test]
        public void WhenKeyUsesNistp384_ThenPublicKeyStringMatchesResultOfSshKeyGen()
        {
            //
            // Generated with OpenSSL:
            // openssl ecparam -name secp384r1 -genkey -noout -out private-key.pem
            // openssl ec -in private-key.pem -pubout -out public-key.pem
            // openssl req -new -x509 -key private-key.pem -out cert.pem -days 360
            // openssl pkcs12 -export -inkey private-key.pem -in cert.pem -out cert.pfx
            // base64 cert.pfx
            //
            var certificateWithNistp384Key = new X509Certificate2(
                Convert.FromBase64String(@"
                    MIIEPgIBAzCCBAQGCSqGSIb3DQEHAaCCA/UEggPxMIID7TCCAq8GCSqGSIb3DQEHBqCCAqAwggKc
                    AgEAMIIClQYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQYwDgQIjoljvpl52U0CAggAgIICaMkUA+n0
                    vNUhZcpf8NLsJ5TYsD0dPphFt7LohaQE3w5ST1Tjm1pAWQAHhvtZ1W9sJdB/gbJrdmKQ+fLDasOt
                    5ufj40I+jQiNvveQDR7WIBVoI1LHA7pG/YYZ8d4zvS+8ZtkKjLKFUqflXWV/Sa3vQvKal1linpM9
                    2HKcyTTTNB88yto8mp96VkWWZZHMIE+kDAjdgO6oXcR+yC+s8O9+OFxOeIVq1xHcSACZv2Mor3Mb
                    V7zu5FLrFqi5acyVt8sniLztzwdluKytO/CjYWRoWC9DaIhsxiXiZPQGXsuBBHMcV3z4+M42ILkB
                    603gNHGvAlSomFmGIqCQ11ojco3Rsrb8odkqumVDFW3ax55g+wsb9TTba6cw60VsY5kPhGKK3hHr
                    Rg64wt7o2HTo6PDurIWMvnDqL1nkn+xhKPvBgwzpf8BHYaoHKVaLeNh6tEi1l+jCAGH0ZC7JVvji
                    DCOlG9sO8tgeeltPldll3GWQe1e/oE9ojduAw2NxiP22NLfcG+zWRBNV2k49mrtsdeAJO3O7+LbJ
                    tqB96vlYrHxX4wZWLULlOZC8Z67MqXO8McNMS4ydMfL9UpqpAzJvRYqRwK/UGG/fduz+WGUt74ky
                    l8XV/UZx+pwmxKws3+zocz9Ci3Ah4NIWFM1XeiBsAzoo5spD7QIUsUMAFGf4cOCsze2ZkXi/MGqW
                    0FBkUhc/0TTPn+gD0mKYEYSncIn7LeFD+oqe2EryqRFTsnWGzyIZpDtaW6Ana71ukGXelFygfqSa
                    w1RJpK81k6u1SZmrg57KOvfvhjl4X0qZEVtQ8ZbkuYDYRG6gyo7U+2QwggE2BgkqhkiG9w0BBwGg
                    ggEnBIIBIzCCAR8wggEbBgsqhkiG9w0BDAoBAqCB5DCB4TAcBgoqhkiG9w0BDAEDMA4ECKFWknbs
                    r1UaAgIIAASBwN23vPjKJ4N/s0nSIBh1qKnLp0ylvemgyerEDNcI6eeY6fm988e5yCOga6uqs0ln
                    1dVj1PwNSAL10dRqV9VRxODRhc6FFP5kfJPW5ykUZtTg/+wdDD2p+b43EzXzwyLt9nommWghdJrW
                    VEYa1v8OflEIOlSR8PfMr8O/U4WhNGX4oJJwPjrmVh44Vm5ld2/lsn1js++nBtipuT2yx6gfgpge
                    4H9TtVWViQoFn29OfQhFirvsA7NNBHVoB01pWj9jUTElMCMGCSqGSIb3DQEJFTEWBBRdhDmPbmij
                    x9+Q/p/AchFF2d4eBTAxMCEwCQYFKw4DAhoFAAQUR8EB3sdauDMaNoCeW04f4u64GV4ECBnaokON
                    TTz6AgIIAA=="),
                string.Empty);

            //
            // ssh-keygen -f public-key.pem  -i -m PKCS8
            //
            var openSshKeyForNistp384Key = "AAAAE2VjZHNhLXNoYTItbmlzdHAzODQAAAA" +
                "IbmlzdHAzODQAAABhBKy3mjS25s76iVqXDlAIvNfD+5Zhb9V+U3ok2V8FQvBymQYv+L2rFbCWBnG2ih" +
                "0oa+ez5502ltV8/+U5DVTp8zr8OQ+siSOwv8P1RTXmzHLHdD0G3quthtjy1QxhXz1bBA==";

            using (var key = ECDsaSshKeyPair.FromKey((ECDsaCng)certificateWithNistp384Key.GetECDsaPrivateKey()))
            {
                Assert.AreEqual(
                    openSshKeyForNistp384Key,
                    key.PublicKeyString);
            }
        }

        [Test]
        public void WhenKeyUsesNistp521_ThenPublicKeyStringMatchesResultOfSshKeyGen()
        {
            //
            // Generated with OpenSSL:
            // openssl ecparam -name secp521r1 -genkey -noout -out private-key.pem
            // openssl ec -in private-key.pem -pubout -out public-key.pem
            // openssl req -new -x509 -key private-key.pem -out cert.pem -days 360
            // openssl pkcs12 -export -inkey private-key.pem -in cert.pem -out cert.pfx
            // base64 cert.pfx
            //
            var certificateWithNistp521Key = new X509Certificate2(
                Convert.FromBase64String(@"
                    MIIEwAIBAzCCBIYGCSqGSIb3DQEHAaCCBHcEggRzMIIEbzCCAvcGCSqGSIb3DQEHBqCCAugwggLk
                    AgEAMIIC3QYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQYwDgQI4Nuo8ojB0hoCAggAgIICsI+tobvZ
                    SW1cYdomQKMWAqTzzKDBe35YpeMr8O80e+0IHR1MCHnkzQ/cu4fFAQxGf9gwXKYNAVbZRR6LXLLW
                    Zj2QiU1cVvES9RscyOfp4LMGXYTVpgSxNQ3NUHFyA6IVomugl/QfSoamYx2MwcbSp7b0G56sa0yz
                    OI1rdK3iQyeDquGgz+vtA6ETa1BrJRg5b5WErfAkQMwoWQSgvdBT9Xv4vxWqpsJcAE1mJgQJoWR1
                    YGNqRj/A7bHQ/t2ZcaVdKeWDfC9UsG15RmJkih26pMY8ElSs5WtTYyLsO16oda3cjWrv723+sinX
                    pzMY0HkMZpQJg+Htg0fiJMnZseezGDW4Q1k0X4awWSehi4i3qC1Ic/cWkpH0lX3YS9aUsj8X97lT
                    Ihyc+FOiP7YCNBj+h+X2ILMtNemAy5dGFtv4qhGpyUuAzxvxjJB9os1mmOOjl4/2FrOxgUPAlHph
                    vv+4+ndo3gZXf+hUml4bY4syzHujrTT44cc9OgTgUtvfkvNkCHa//FUlzFVUDW86dHJafeL5hRCl
                    KWUqNeVsAOST2gnwfhmkFPuLs8Ei0rWt0IBh3oNLemX1RyubUj4HMHKwdnvKbAxRDzFwJFCyDrOv
                    GUBITGFSSKhb+zNZqFVov1sjAY75uasZLowYHyoBXHsCNoEbbhvD/MHiXeYtNRbgnGTa0efEr//D
                    u4WKbZzmVJ4Na/RcB/qqKvZfQnCRrXKRMldid5kJ1c7tvDXFu9TythY0KRR6GMxJ1pEOjacnke+Q
                    6McfehYNlUNjZEKCgtmJkgcytSPY78I92jnMSCU6I7KfuMKAyR/OpCQ4ty0ChUMpfRw/Kj6ei6OM
                    9PjkIbURIz5fczxkWu+s1H0PCw61yUN/cknbJ0waUeW4L6lJ4ZAdF0/ZnSAn/Oy+dI11zMBEKNYw
                    ggFwBgkqhkiG9w0BBwGgggFhBIIBXTCCAVkwggFVBgsqhkiG9w0BDAoBAqCCAR0wggEZMBwGCiqG
                    SIb3DQEMAQMwDgQIWFerBdpVl2gCAggABIH4mBtfkdSuyD2fuATFaCWjaEKGHkaTJ+m5NUgQU9Dn
                    WDrUywUo/0AxDflvLdaO33Nafhr30G4yRKwqftUlgrBucN1fSwkXMGtjz17uVwWIcQcxWZgDoah8
                    Cs3RMelUgr4jhWttCnHD/GKFxzcNfsBU/lyQHfPY2nO6a/9P6BEbSdNzf/qHnKugRBdLHloa8ICP
                    Suaujji2V8aq+xSonxgTZA37g/CEynJ3060YKZ7QCBA0okXQ0SkYxNQTZ4iEfwP67qmDhDw/zEhw
                    ynbkdnV2642GVFdEPAF+t2nUI3++9ft9SM9YxlxF3V9ORk+9YJxSRro7fqAtPB4xJTAjBgkqhkiG
                    9w0BCRUxFgQU5716hms1gYxaZHP8d8hw54vXALAwMTAhMAkGBSsOAwIaBQAEFJrHWqbNQVqGEftJ
                    cbMI+fMt3i6rBAiSH0LFXgP6LgICCAA="),
                string.Empty);

            //
            // ssh-keygen -f public-key.pem  -i -m PKCS8
            //
            var openSshKeyForNistp521Key = "AAAAE2VjZHNhLXNoYTItbmlzdHA1MjEAAAA" +
                "IbmlzdHA1MjEAAACFBAFUv+fP8ziOox7ND+z0EumV0A4L+f5mjy5dBJ29WuHEv+a8LbQoSDDS88aIeg" +
                "HjFcKK+tKQUPxZlRHqkKnZDr4atgCQHZPoQCR9fyy5ted5sjjhSNME1AJPIl92SX/PysEhgeD1GyJDg" +
                "SKZRvKwiCGc1axl7m8m4x+a0cqVgvquqMYoYQ==";
            using (var key = ECDsaSshKeyPair.FromKey((ECDsaCng)certificateWithNistp521Key.GetECDsaPrivateKey()))
            {
                Assert.AreEqual(
                    openSshKeyForNistp521Key,
                    key.PublicKeyString);
            }
        }
    }
}

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

using Google.Solutions.Ssh.Auth;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Google.Solutions.Ssh.Test.Auth
{
    [TestFixture]
    public class TestRsaSshKeyPair
    {
        [Test]
        public void WhenKeyUsesRsa3072_ThenPublicKeyStringMatchesResultOfSshKeyGen()
        {
            //
            // Generated with OpenSSL:
            // openssl rsa -in private-key.pem -pubout -out public-key.pem
            // openssl req -new -x509 -key private-key.pem -out cert.pem -days 360
            // openssl pkcs12 -export -inkey private-key.pem -in cert.pem -out cert.pfx
            // base64 cert.pfx
            // 
            var certificateWithRsa3072Key = new X509Certificate2(
                Convert.FromBase64String(@"
                    MIIM2QIBAzCCDJ8GCSqGSIb3DQEHAaCCDJAEggyMMIIMiDCCBP8GCSqGSIb3DQEHBqCCBPAwggTs
                    AgEAMIIE5QYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQYwDgQIAoGiw+DfIIwCAggAgIIEuO9sGFsW
                    iJfWGYsuPIPybI/IpLohlzC5tvNDXK4miCDov5ChKann+qxUZqJSQ1D2jdraipXAUAqtwf3hzuEN
                    /kJyOKo9ZuqyGG3ZykI+ROVrzb7iJODeXv/zAUdvQVOO14ppGnzLwfVct8jUVtXkKKmZLdB9pmuT
                    wKNQduNBLcd6TH43BlRVn1d1LRPuRo7208fC47z03Lc+L2pyJxhQNWZTGxT6yt5084Hp+dhLnNml
                    jiE1iuj8VrjqykknLNI+3CYR9Uf4j5TwzUyCL9YjDEWBf5dCVn02f8H/KBDY7IGmL5YrRBQUZOts
                    +ZZJpGeZdo5ZE0hdwchDEqZCqla0CCyeuPDKUoT9+S5XqFgpSCiF1KiKllI+Q5rWd4YDv1+1TQFy
                    lcfhKg+nlk7C/41wOrdRfBcpl61wwzNvjbkr6FR2b9+BHwdBzQ2JsI6TR+dH+8eGe9aNTi9dMHb1
                    nrxmICIsca/BvEae1UdFh6ik8RsfOFuR7wf+p1CtEYHIUgj2Ip8RsLJSQRu+C/B0u5B/F6E4N/43
                    rOFBERs9cQDENn8MHprkHsH32CECs/T8GNVAR85uFHsLFyiYs9/CEK2vaNS1KrK89uG7XClD/xgX
                    gt9uZZyuKzKbJXnMZXze99JEAFfpssWS4NraHY1MgX0TxNPPAKeVgMq0Pbj5g0u6EOwa3u+ZsZFx
                    WE7CrHYZaj6AdfLXzUwZVxHjwnOLrX18c+RqHWrAOrIcnxTZVgVlHHHg2Cnooib07A3kgJXfxFoD
                    2OCbuL+gf+/dz+mKwCUCmmS0yWHYpbfbWAbHkgUkdpZMS39xiRK1LFRiNp3Ib0frTke32xI8a97i
                    a3OlsLDk9MWe+f9y58/TrhYDztlfGwXbvzwPwuTo0LnzsNPo6y3xV89r5LI3SJq/odYUrFRczRHl
                    s5jeHQ5wneNyIHZyneP0j2z0Iv62LFwmroDeIkAP5QO8Adj7c+xcGe3hjXM/eaQPsoJ2qpEG4XIp
                    s8fZ8Ie7+5ffOqPs6dtn5dVMA2qz1+O/BdoEyzB2HrA3vvfdWh72YYHlYYvwUs3Shd88IqIj0lWX
                    mJo8Jo9+6Hok6GYSHyj0dkT8c9QVQWMS9VlLASkRmnUJmYIfcD/s7nDku0f+oPU6An+6Gi6hylZa
                    evzzwMsYA4m0oevNFJXZpo6FCn3ibzhb9ucnVnkfi4zZlfQhaMnQg0FHQDD9YWSKGYSlGC3ib9eZ
                    51r+uLrqNjqbWqA9v447fnIQEXR9XkDJCqzmn+QlYUbOALu3imd4DZ5BpZTlgSCCkowM9vWjLhj5
                    mA2kRsArg8IpScRpx6BJn93skd36JaBDOauCH7zkGZVWIgJSl19+lgdnh8qoiAhaghLw1/k4/sHz
                    pmX9plCEZbGrC8vvTTjD/mJPUFO+RZRJyWdiNBqU5rvVu8TlkD/S3/D8jzDh1sF3P+nLpuSSRP2g
                    lOtKGE7znxrwEo/gMWbELacUiL9ap/kB0SCCDblorPTTuzOseGT5uhbZ13B8qXjAd9FjyGviOWUz
                    X/vPZ3sVdlR4s1jcdSziXdQrCkAEa/iDTvo5f1Fj5McJ7OL2FhgJS61IltcZrYKXSG2pd+DqM9Nk
                    ZRqhE7ryMIIHgQYJKoZIhvcNAQcBoIIHcgSCB24wggdqMIIHZgYLKoZIhvcNAQwKAQKgggcuMIIH
                    KjAcBgoqhkiG9w0BDAEDMA4ECMdBoz8+6M+XAgIIAASCBwi/SaB3ngDOse9B2ljmZLQ1KyPdLXpa
                    0PEu0A1z6yJTHPb4na/0hG/xSlabVTyeVRz7v5YqU9NC94Sp9kbKBVYUID3k72WOIMeX7mSki7RC
                    mwz90SglgPrFAmy0edGm74yhGfuMfu9mSWl2eBX1Im3KkTt/YGZmfgJzCtXAOZSmU+ZDcAeWrOnB
                    rbDRRv+N7B9wz2ozXjmZYSR0QC5NkWl2abbplgz51JkRXxcX75CW6Kesx6SNMMa17tn6jsjXLE04
                    IU4sfeUWZ5SiN7c1on79bvAg40IitMTioOM83R4mVupkHAUCUCPGVSOoCxmMZyDwj7kaoNTC74rB
                    JjKNficKQLWyqzGMbAYR5ngHjFQMl5jFQRgw8xenlxVL7FN47iX69/J16VHh8rrrzeS6w0+wixxh
                    uNAxA/P4kDiNzQ2HP+9Y2LYvqjdEm6wADH0iSd2BTblBUT9EV80q1RqWaPvattvWTEjDq3Mqpt1G
                    gWydNJlKA5hkIkkPaXGaR+U4fSQHjYQJBUx29rFPCfi/SBWsfCcyl1Pqn9PJNysa3T45gOJV0D7r
                    cYAlodNgOk7c29a64RuCajSb2XGvtWWewr2fDgHmkeDgvh/DCfnF+y2Z43OZY90MCWTEIu+Hj5H0
                    5xQOL3rOfUym3f/eIMfMvHYQVspJZ54+tNwvl1spAR/RVXTATinVE3OgUMw84fnWMcf2B1neodjQ
                    DJfL/t2HochRC47JOYAjXt4HjAz1v3++yjS6v+/H2gy+KV5T4tpswXWgvLhBK4vL0HxnNUoFWd24
                    gJAE/DlTKgHzVa8QYjlEXMKifZ2X5JwX/s9P+BO+IVAr1v8aeLN9TCEXh+6dB4iVCCoXkqx8QUo0
                    0BFrfkQLEmO2M82gY3LmNyhh0GXvtLWAayCaEET2wH9a0WG6PzkCSh2BRJ12h8M8nmHqUwT4NZKn
                    WD5FoPr1ngPExkDdXOE2LKNaOoX3/jRMmMUjD02go+Wk5ZlIJw9ImcJNuqq/VHWKKlPH44O19BMX
                    KfLhxRdlQjq1IF89QE8bSG3fI9vDUiMPOXF2ooZC7GPgbDSaVAMDLLpOSgYC4LwduVcgD5OKkk8C
                    sekS1/7FKze07aEqs2vNMy0+/4m9XSMq7TLknO4P71JvA6psAHIK3wV6Glt283QCGTvgvZvu6guU
                    /Pu++1lKsEatTMwdxBHUgAefQByCinf3oxLgQkCCIhBiGfbTli7dT5Q5VcSlveX11kml+/bDV6bH
                    EnDRwYkbD+Lxzq/pTpdwByZLYLlnLK0R7tdjz7GkAgLxYkYux6EwDRvKM9wQTo1i2KU6NyiCIkil
                    QoC3dR3A6TygqK0obDb/M7wC2ZlLmFJS9clu15EGE3yfckxYx17x08Z0uLwbQVDQqpv07rxt9Jfi
                    7D4pVXAyCkogYvIaR7w151udFcjzC4UdX1aOqYLQNWwCBu79vcql5itfqjtTThIxGAcSvLBegxwO
                    qZcIN4KEQffw7DMjrnlbjMBUwxKF4kGRKirmWoh7jxiojlbZT0hNEb2AncCEfjB9/W2ZrvvMqqr1
                    RE/GaFP/wGH4CsvyPeNlr0Acg06uGbLgYCtT0g0vn0krdjcGWESuHTuMOsQ8gpmUvOTCJIhWbBS3
                    B7oHBXmxEbUTPH+6K5mG6KpK0hQY3uCrTbigtsMEvyrIXYS8HTEUjp58+hwBYs9S877ePX8+2i+a
                    hAmlgc4/ndZeXWUPaYgAuu+m/HcUC+mpXHWdgEE6SMF2nrWmLcyaITJjlxQMkFpn9REMZKfLQw+3
                    RKAl5ngZxDnAecKGwFNAN+Y5enyYPDPWInainH6tz7VRT6Qd9zdk002qH4G6V4ylZOT2QaI0FyAh
                    /SCMdOc/6cJ2PFU3jHkBn5cLZpB1gK/KjWid+x07g6OXwsaZWTGtMXx+TXAL2EC5jZc0XHiKdrGu
                    2v9FNGPtzMlhEriEDLKfamrXfOuV7KiHT/jVp4s4TpUQaua9RcNsGpjyd2EESsnmEpW2uZQuv1cI
                    7rotz4QbI6IUzj7RFhBGAzYJhEYR+UEJ3DTIssBara18XlJsiskvfxprPDjJtS1xCf6DRuda3whu
                    J+AmM7y5Fyi0FuUOUIvybOqz66rYICZrFCL+xBXA5WfJxa5gHErbr3BJE74Cxwg7yylXeGzK4VED
                    t2cyuyNAxvYIvaA6RzHoMZn2verrw8a7cXkttzpjE3qfvyFSdL/C2EbDZ/N1hzy4K9jDX39oS46l
                    vL/yerXLd8hKQjeuEBson/lro1Yl7ETnqED4pEQW9ZvrnZ8VHcYG3TEqcId0L4dn0hkb8a8p1bAH
                    cWOJpWWgN5q8G1STFmkrBLLGfp6eNRZiVBZ9U6/IvaNuycpctQ0hhx7Hz4GQ0oyQIMEKJ4L5Sgo4
                    mLM6DLz5SaI53RUxJTAjBgkqhkiG9w0BCRUxFgQUvSlWH+3DRfuHWaumTPxHZ6//4yQwMTAhMAkG
                    BSsOAwIaBQAEFDZQJHO+tGu0ZKVBMxpC7mS6OBD1BAj1RjpZ0SaPKAICCAA="),
                string.Empty);

            //
            // ssh-keygen -f public-key.pem  -i -m PKCS8
            //
            var openSshKey = "AAAAB3NzaC1yc2EAAAADAQABAAABgQC" +
                "zTXL91kYHgQmFiwX6N2utlU3+eDywXgUG2cei+erOaZenMDFC9ntl6ILhMB0vEhNgAQ51tI+j7616FT" +
                "nVt1hBuGydnBcyEYfF7zdfzhlC7HJRfZvHAoPIaNauw6Qv80BrgL+gBJ7lSTTJv1lyuO8hVmg48tNMm" +
                "nbECXiON/DVxcXdgv8uN8gfM86EhEFgXoH4HK394VTXwhbvp82K3PVhDoNqVrg08QPibPW2nWwXYDge" +
                "NuGqPCjoHqBk1viuAaiBe3uUl5FUUvR/Pcor2wcDWckotJ7ZiixyC3EheP3wGRe9R+9/lryg2gv9vjv" +
                "sdKxvXSMMex6PlHuOcuYg9pz+kxWkcrW01fuGSaV/IWB8HOsr+jaHs5CjDyDR60HRekpOStUxqv0ipc" +
                "NG0Yk0/da/AEUNQJ4r7T5pjWTtshSRuDx257MklJbqt98ShOyZcabfsWxQUTsM+mBijXIV+XvU9KW3N" +
                "R3S+um1Uz+YmUcWNYSAOLA5xIz/H5UFEk5ywv0=";
            using (var key = RsaSshKeyPair.FromKey((RSA)certificateWithRsa3072Key.GetRSAPublicKey()))
            {
                Assert.AreEqual(
                    openSshKey,
                    key.PublicKeyString);
            }
        }
    }
}

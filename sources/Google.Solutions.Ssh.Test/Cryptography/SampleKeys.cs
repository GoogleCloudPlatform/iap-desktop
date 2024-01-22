//
// Copyright 2023 Google LLC
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

namespace Google.Solutions.Ssh.Test.Cryptography
{
    internal class SampleKeys
    {
        //
        // ECC.
        //
        public const string Nistp256 =
            "AAAAE2VjZHNhLXNoYTItbmlzdHAyNTYAAAAIbmlzdHAyNTYAAABBBOAATK5b5Y" +
            "ERo8r80PGSNgH+fabpTTr1tSt3CcAXd1gk3E+f1vvPL/1MxYeGolwehAyTL8mP" +
            "kxxmyn0tRb5TGvM=";
        public const string Nistp384 =
            "AAAAE2VjZHNhLXNoYTItbmlzdHAzODQAAAAIbmlzdHAzODQAAABhBL6qZo4KDt" +
            "qqGrpk0+SuiYtDxABMWf330nc6xfXeIJ7Fbbpnvpjg0o8Y3Z12L/KtTVkyEYW+" +
            "gujUacFa6urOW8PdFwn5sJi0UN4pUIJypI1ish3roab0uG7dqbHNuNANYQ==";
        public const string Nistp512 =
            "AAAAE2VjZHNhLXNoYTItbmlzdHA1MjEAAAAIbmlzdHA1MjEAAACFBAESzAPsMa" +
            "tZVMT31EHOvTcWN694KYmIrx3OA2vKiSWHkX2+Wun3KNmSAFxU1AVmL5kEQJpL" +
            "stT2bhnTzUMUUr25bQD11KUPkP1roaCYxpUlcyem8rTYi6IC9nxo5nNmzOD4Rj" +
            "tNvF05NitDw9LZaBEWWBD8Bs34lmptlo3cbCYyAFKW0w==";
        public const string Ed25519 =
            "AAAAC3NzaC1lZDI1NTE5AAAAIOeTnI4/fMiw5Aahj3o+GzOFjgXivxvExrEN795o+q+T";

        //
        // RSA.
        //
        public const string Rsa1024 =
            "AAAAB3NzaC1yc2EAAAADAQABAAAAgQC15/jAy4ZpHr+Li+d0gFpV2vk5iQA8yf" +
            "oUA9h6bmON9ITe5gWUfe/Jkkzi7TNcczae7gycp1E3kIRWSdpbhxdbSO0Kkae+" +
            "vNqaXqUSMnOOg5kWtPlRunDB+5DS+ghzxgllSsE4C8j9XC1loBh9wAzMUstePh" +
            "mJ/10DXsL1AdLQNQ==";
        public const string Rsa2048 =
            "AAAAB3NzaC1yc2EAAAADAQABAAABAQCsovX/mkfa8Z9XAH/uO1ASJ1shBoHLYg" +
            "JIF26sUmcg454znUNm1dApPxawSC5I6Th3aNvXOW/ojffT87C3fkmWZ5LrghIn" +
            "u9/muC0zZwvNPTkYdG6f8cuiJBUqntExkD4gNix5zNzxIEtl5GSe6UWU59+Aw5" +
            "Vi0+7wrahNi8E9cZKtuWWEh3CR7nGZE81yunT0vDv6gAtJADL5IUEsmOtWkzUm" +
            "Oj3rHRPiV2rOMcVVbFO3NxoDWCvLtLen2ld+CJXgWHjzCa0p1i0X6YlnvG5ZHx" +
            "zdmZd1DpyEb2HYVNjQKKvWP03kjPFYdM7gKfKBZ9qhDbj9pDotq67fvIVprcLz";
        public const string Rsa3076 =
            "AAAAB3NzaC1yc2EAAAADAQABAAABgQwY5hBWLT4u/hRxScW8nHdpBnX9nXmDSu" +
            "LIuz7FN1HVZySZWLPlnDUuNY27H/zdHRBBfHQA9456YP/VV9StjLlvY3nLcpmM" +
            "rWCnC9nesHAdnsZQQBUqaoE6YoXuZA562XD2ovpIhEmmtBB9qmCdJwDaI/fg/m" +
            "2f0yRrWxz5mSmkN2jxB8tUdeY0iDfo8636vSmNnwzBDom123jgesjAURVTZL7o" +
            "XGnAqs7oNvzYsaoKbI7Ak3o6YVlNuWnMjlJuPzndXxSDN+wWCUDXzSiSFBlexg" +
            "0YflzXyycZfpq3Dv08TXzKUvXjnBb+S6e5jUwd5LkPI3WSorE/JZHERManxyAa" +
            "F/z2Sf8hOczDLU+VpWnQ2qwnUNfSjMK1BVAOhdwymkUmGTG4liSP8gI6mlNNAa" +
            "66F3h/+twCCF6UQsSINKNkXuRQchX0E0Uqhwa+4i1WcMxvpM6zeqcn5fFlmWuk" +
            "HQcWRyxhHJ56cVyjUBrvGL6VOsUIJpAnOFhyAfOKR/rIwXU=";
        public const string Rsa4096 =
            "AAAAB3NzaC1yc2EAAAADAQABAAACAQDVbqfei3w1kw37maxtm4ECyeNaEc1Wvc" +
            "wSmMKQ8ClwSpEotNHlvYbVPv+iYzQGvqwAtiJbBHNSOiZou/f6Y9IXC8hrFkek" +
            "+yry9fhPYC72huCtjzEipf7rzxtHmBSYu1GxI0etRM7ijpiE7nayvaahD4MQ3C" +
            "khc57sQagpMx7VUk8C7EbO9b40Q0YgUjKpG4DPUVOHhvKz7b6gg2XzS56oT4jh" +
            "vHRcHsYFmbXazrzKgTj+wLZrDrf3Aa7pUCJzm/AR6hI/5wZs9xvuMuywfl3swA" +
            "EJZw+YWXaOyOyT5t1zyzzLfPzjTBbPh9yNJ85lyRu7upP3GaB2Q9Cx2GptCMUn" +
            "guk4IfnfHidWuNQot265hMfet8zPbH9VAMNGapDz154DTG4K3DCkeMWTuZrdlK" +
            "aTEh1oFIwP3uhQLlPX4ynj2iuzo41JqIpxO7WZz+Ir2CCKe00vP29R71uSmc5I" +
            "QQamlkVdQCoL24ltqKWwN5wX/AZU5fXxItboALz1moB3jPH111rH4WHo/UOBx9" +
            "6pzMsRgmmLjmgIfGrCRDyBWOoHW92w+mtX8q6g7nJQieaEhRl4Uo8nC7rZITca" +
            "TWlejIewTs0u4odez7GtOUXZSNMiXke8pShoQLLCb9+Uyd4pVwk//lwxUf5WOL" +
            "j/12K6VTRSaeCZs5hFJ4JfKeI1C5hflw==";
    }
}

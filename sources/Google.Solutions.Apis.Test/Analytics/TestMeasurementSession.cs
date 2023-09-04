﻿//
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

using Google.Solutions.Apis.Analytics;
using NUnit.Framework;
using System.Linq;

namespace Google.Solutions.Apis.Test.Analytics
{
    [TestFixture]
    public class TestMeasurementSession
    {
        //---------------------------------------------------------------------
        // GenerateParameters.
        //---------------------------------------------------------------------

        [Test]
        public void GenerateParametersSetsDebugMode()
        {
            var session = new MeasurementSession("client-id")
            {
                DebugMode = true
            };

            var parameters = session
                .GenerateParameters()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Assert.AreEqual("true", parameters["debug_mode"]);
        }

        [Test]
        public void GenerateParametersSetsSessionId()
        {
            var session = new MeasurementSession("client-id");

            var parameters = session
                .GenerateParameters()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            Assert.AreEqual(session.Id.ToString(), parameters["session_id"]);
        }

        [Test]
        public void GenerateParametersSetsEngagementTime()
        {
            var session = new MeasurementSession("client-id");

            var engagementTime1 = long.Parse(session
                .GenerateParameters()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)["engagement_time_msec"]);

            Assert.Less(engagementTime1, 1000);
        }
    }
}

//
// Copyright 2019 Google LLC
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

using Google.Solutions.LogAnalysis.Events;
using Newtonsoft.Json;
using System;

namespace Google.Solutions.LogAnalysis.Extensions
{
    internal static class ListLogEntriesPage
    {
        internal static string Read(
            JsonReader reader,
            Action<EventBase> callback)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    // Read entire array, parsing and consuming 
                    // records one by one.
                    foreach (var record in EventFactory.Read(reader))
                    {
                        callback(record);
                    }
                }
                else if (reader.TokenType == JsonToken.String && reader.Path == "nextPageToken")
                {
                    return (string)reader.Value;
                }
            }

            return null;
        }
    }
}

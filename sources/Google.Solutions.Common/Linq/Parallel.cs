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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Google.Solutions.Common.Linq
{
    public static class ParallelLinqExtensions
    {
        public static async Task<IEnumerable<TOutput>> SelectParallelAsync<TInput, TOutput>(
            this IEnumerable<TInput> inputItems,
            Func<TInput, Task<TOutput>> mapFunc,
            ushort batchSize = 8)
        {
            var result = new List<TOutput>();

            foreach (var batch in inputItems.Chunk(batchSize))
            {
                var tasks = batch.Select(mapFunc);

                await Task
                    .WhenAll(tasks.ToArray())
                    .ConfigureAwait(false);

                result.AddRange(tasks
                    .Select(t => t.Result)
                    .Where(res => res != null));
            }

            return result;
        }
    }
}

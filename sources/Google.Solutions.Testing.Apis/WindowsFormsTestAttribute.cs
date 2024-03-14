//
// Copyright 2024 Google LLC
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

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Testing.Apis
{
    /// <summary>
    /// Test that requires a WindowsFormsSynchronizationContext because
    /// it might be using await-calls that require messages to be pumped.
    /// </summary>
    public sealed class WindowsFormsTestAttribute : TestAttribute, IApplyToTest, IWrapSetUpTearDown
    {
        public new void ApplyToTest(NUnit.Framework.Internal.Test test)
        {
            base.ApplyToTest(test);
            test.Properties.Set(PropertyNames.ApartmentState, ApartmentState.STA);
        }

        public TestCommand Wrap(TestCommand command)
        {
            return new WindowsFormsTestCommand(command);
        }

        private sealed class WindowsFormsTestCommand : DelegatingTestCommand
        {
            public WindowsFormsTestCommand(TestCommand innerCommand)
                : base(innerCommand)
            {
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                var originalContext = SynchronizationContext.Current;
                if (originalContext is WindowsFormsSynchronizationContext)
                {
                    //
                    // Context is ok already.
                    //

                    SynchronizationContext.SetSynchronizationContext(
                        new WindowsFormsSynchronizationContext());

                    return this.innerCommand.Execute(context);
                }
                else
                {
                    //
                    // Temporarily swap contexts.
                    //

                    SynchronizationContext.SetSynchronizationContext(
                        new WindowsFormsSynchronizationContext());

                    try
                    {
                        return this.innerCommand.Execute(context);
                    }
                    finally
                    {

                        SynchronizationContext.SetSynchronizationContext(
                            originalContext);
                    }
                }
            }
        }
    }
}

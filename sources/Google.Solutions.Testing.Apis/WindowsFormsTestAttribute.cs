using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

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

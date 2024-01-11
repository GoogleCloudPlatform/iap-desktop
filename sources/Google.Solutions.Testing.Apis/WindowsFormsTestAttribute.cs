using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Google.Solutions.Testing.Apis
{
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
            public WindowsFormsTestCommand(TestCommand innerCommand) : base(innerCommand)
            {
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                if (!(SynchronizationContext.Current is WindowsFormsSynchronizationContext))
                {
                    SynchronizationContext.SetSynchronizationContext(
                        new WindowsFormsSynchronizationContext());
                }

                return innerCommand.Execute(context);
            }
        }
    }
}

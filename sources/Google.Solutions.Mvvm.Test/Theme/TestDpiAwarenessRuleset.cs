using Google.Solutions.Mvvm.Theme;
using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Theme
{
    [TestFixture]
    [InteractiveTest]
    [Apartment(ApartmentState.STA)]
    public class TestDpiAwarenessRuleset
    {

        [Test]
        public void TestUi()
        {
            using (var form = new SampleDialog())
            {
                ApplicationExtensions.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();

                new ControlTheme()
                    .AddRuleSet(new WindowsRuleSet(false))
                    .AddRuleSet(new CommonControlRuleSet())
                    .AddRuleSet(new DpiAwarenessRuleset())
                    .ApplyTo(form);

                Application.Run(form);
            }
        }
    }
}

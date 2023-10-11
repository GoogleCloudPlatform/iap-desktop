using Google.Solutions.Testing.Apis.Integration;
using NUnit.Framework;
using System.Threading;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Theme
{
    [TestFixture]
    [InteractiveTest]
    [Apartment(ApartmentState.STA)]
    public partial class TestDpiAwarenessRuleset : Form
    {
        public TestDpiAwarenessRuleset()
        {
            InitializeComponent();
        }

        [Test]
        public void TestUi()
        {
            using (var form = new TestDpiAwarenessRuleset())
            {
                Application.EnableVisualStyles();
                Application.Run(form);
            }
        }
    }
}

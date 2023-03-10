using Moq;
using NUnit.Framework;
using System;
using Google.Solutions.Mvvm.Binding;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Solutions.Mvvm.Windows;
using System.Windows.Forms;

namespace Google.Solutions.Mvvm.Test.Windows
{
    [Apartment(System.Threading.ApartmentState.STA)]
    [TestFixture]
    public class TestPropertiesView
    {
        private class SampleSheetView : UserControl, IPropertiesSheetView
        {
            public Type ViewModel => typeof(SampleSheetViewModel);

            public void Bind(PropertiesSheetViewModelBase viewModel, IBindingContext context)
            {
            }
        }

        private class SampleSheetViewModel : PropertiesSheetViewModelBase
        {
            public SampleSheetViewModel() : base("Sample")
            {
            }

            public override ObservableProperty<bool> IsDirty { get; }
                = ObservableProperty.Build(false);
        }

        private IServiceProvider CreateServiceProvider()
        {
            var serviceProvider = new Mock<IServiceProvider>();

            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(PropertiesView))))
                .Returns(new PropertiesView(serviceProvider.Object, null));
            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(PropertiesViewModel))))
                .Returns(new PropertiesViewModel());

            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(SampleSheetView))))
                .Returns(new SampleSheetView());
            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(SampleSheetViewModel))))
                .Returns(new SampleSheetViewModel());

            serviceProvider
                .Setup(s => s.GetService(It.Is<Type>(t => t == typeof(IBindingContext))))
                .Returns(new Mock<IBindingContext>().Object);

            return serviceProvider.Object;
        }

        //---------------------------------------------------------------------
        // Binding.
        //---------------------------------------------------------------------

        [Test]
        public void SteetsAreBoundToTabPages()
        {
            var serviceProvider = CreateServiceProvider();

            var window = serviceProvider
                .GetWindow<PropertiesView, PropertiesViewModel>()
                .Form;
            window.AddSheet<SampleSheetView>();
            window.Show();

            var sheet = window.Sheets.FirstOrDefault();
            Assert.IsNotNull(sheet);

            Assert.AreEqual(
                "Sample",
                window.TabPages.FirstOrDefault().Text);
            
            window.Close();
        }
    }
}

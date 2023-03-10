using Google.Solutions.Mvvm.Binding;
using System;

namespace Google.Solutions.Mvvm.Windows
{
    internal struct ViewType<TView, TViewModel> // TODO: Not needed?
        where TView : IView<TViewModel>
        where TViewModel : ViewModelBase
    {
        public Type View => typeof(TView);
        public Type ViewModel => typeof(TViewModel);
    }
}

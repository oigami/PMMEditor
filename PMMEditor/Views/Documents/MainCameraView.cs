using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PMMEditor.ViewModels.MMW;

namespace PMMEditor.Views.Documents
{
    public  class MainCameraView : SharpDxControl.SharpDxControl
    {
        public MainCameraView()
        {
            (DataContext as IInitializable)?.Initialize(Device);
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            (dependencyPropertyChangedEventArgs.NewValue as IInitializable)?.Initialize(Device);
        }

        protected override void Render()
        {
            (DataContext as IRender)?.Render();
        }
    }
}

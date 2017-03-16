using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMMEditor.ViewModels
{
    internal class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            WindowViewModel = new MMD.MainViewViewModel();
        }

        public object WindowViewModel { get; }
    }
}

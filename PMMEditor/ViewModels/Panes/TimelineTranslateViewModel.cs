using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Livet;
using PMMEditor.ViewModels.Panes;

namespace PMMEditor.ViewModels.Panes
{
    internal class TimelineTranslateViewModel : PaneViewModelBase
    {
        public override string Title => "TimelineTranslate";

        public override string ContentId { get; } = typeof(TimelineTranslateViewModel).FullName;
    }
}

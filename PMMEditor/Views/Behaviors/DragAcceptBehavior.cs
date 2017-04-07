using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace PMMEditor.Views.Behaviors
{
    public class DragAcceptBehavior : BehaviorBase<FrameworkElement>
    {
        public ICommand DragCommand
        {
            get => (ICommand) GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(DragCommand), typeof(ICommand),
                                        typeof(DragAcceptBehavior), new PropertyMetadata(null));

        public ICommand DropCommand
        {
            get => (ICommand) GetValue(DropCommandProperty);
            set => SetValue(DropCommandProperty, value);
        }

        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.Register(nameof(DropCommand), typeof(ICommand),
                                        typeof(DragAcceptBehavior), new PropertyMetadata(null));

        void AssociatedObject_DragOver(object sender, DragEventArgs e)
        {
            ICommand command = DragCommand;
            if (command == null)
            {
                e.Effects = DragDropEffects.Link;
                e.Handled = true;
                return;
            }

            if (command.CanExecute(e))
            {
                command.Execute(e);
            }
            e.Handled = true;
        }

        void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            ICommand command = DropCommand;
            if (command == null)
            {
                e.Effects = DragDropEffects.Link;
                e.Handled = true;
                return;
            }

            if (command.CanExecute(e))
            {
                command.Execute(e);
            }
            e.Handled = true;
        }

        protected override void OnSetup()
        {
            AssociatedObject.PreviewDragOver += AssociatedObject_DragOver;
            AssociatedObject.PreviewDrop += AssociatedObject_Drop;
        }

        protected override void OnCleanup()
        {
            AssociatedObject.PreviewDragOver -= AssociatedObject_DragOver;
            AssociatedObject.PreviewDrop -= AssociatedObject_Drop;
        }
    }
}
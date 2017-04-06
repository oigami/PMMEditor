using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace PMMEditor.Views.Behaviors
{
    public abstract class BehaviorBase<T> : Behavior<T> where T : FrameworkElement
    {
        private bool _isSetup;
        private bool _isHookedUp;
        private WeakReference _weakTarget;

        protected abstract void OnSetup();
        protected abstract void OnCleanup();

        protected override void OnChanged()
        {
            T target = AssociatedObject;
            if (target != null)
            {
                HookupBehavior(target);
            }
            else
            {
                UnHookupBehavior();
            }
        }

        private void OnTarget_Loaded(object sender, RoutedEventArgs e)
        {
            SetupBehavior();
        }

        private void OnTarget_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupBehavior();
        }

        private void HookupBehavior(T target)
        {
            if (_isHookedUp)
            {
                return;
            }

            _weakTarget = new WeakReference(target);
            _isHookedUp = true;
            target.Unloaded += OnTarget_Unloaded;
            target.Loaded += OnTarget_Loaded;
            SetupBehavior();
        }

        private void UnHookupBehavior()
        {
            if (!_isHookedUp)
            {
                return;
            }

            _isHookedUp = false;
            T target = AssociatedObject ?? (T) _weakTarget.Target;
            if (target != null)
            {
                target.Unloaded -= OnTarget_Unloaded;
                target.Loaded -= OnTarget_Loaded;
            }
            CleanupBehavior();
        }

        private void SetupBehavior()
        {
            if (_isSetup)
            {
                return;
            }

            _isSetup = true;
            OnSetup();
        }

        private void CleanupBehavior()
        {
            if (!_isSetup)
            {
                return;
            }

            _isSetup = false;
            OnCleanup();
        }
    }
}

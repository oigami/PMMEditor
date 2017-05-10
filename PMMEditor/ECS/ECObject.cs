using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMMEditor.MVVM;

namespace PMMEditor.ECS
{
    public abstract class ECObject : BindableDisposableBase
    {
        protected abstract void OnDestroyInternal();

        public static void Destroy(ECObject obj)
        {
            obj?.OnDestroyInternal();
        }
    }
}

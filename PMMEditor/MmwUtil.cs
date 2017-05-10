using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMMEditor
{
    class MmwUtil
    {
    }

    public static class MmwExtension
    {
        public static IEnumerable<(T, int)> Indexed<T>(this IEnumerable<T> self)
        {
            return self.Select((v, i) => (v, i));
        }

        public static IEnumerable<(T, TU)> ZipTuple<T, TU>(
            this IEnumerable<T> self,
            IEnumerable<TU> other)
        {
            return self.Zip(other, (a, b) => (a, b));
        }
    }
}

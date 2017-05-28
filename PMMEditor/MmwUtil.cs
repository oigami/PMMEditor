using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMMEditor
{
    internal class MmwUtil { }

    public static class ListExtension
    {
        public static void Resize<T>(this List<T> list, int sz, T c = default(T))
        {
            int cur = list.Count;
            if (sz < cur)
            {
                list.RemoveRange(sz, cur - sz);
            }
            else if (sz > cur)
            {
                list.AddRange(Enumerable.Repeat(c, sz - cur));
            }
        }
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

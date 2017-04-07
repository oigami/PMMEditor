using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMMEditor.MMDFileParser
{
    public enum MmdFileKind
    {
        Unknown,
        Pmm,
        Pmd,
        Pmx
    }

    public static class Mmd
    {
        public static MmdFileKind FileKind(byte[] checkData)
        {
            if (Pmd.MagicNumberEqual(checkData))
            {
                return MmdFileKind.Pmd;
            }
            if (Pmm.MagicNumberEqual(checkData))
            {
                return MmdFileKind.Pmm;
            }
            if (Pmx.MagicNumberEqual(checkData))
            {
                return MmdFileKind.Pmx;
            }

            return MmdFileKind.Unknown;
        }
    }
}

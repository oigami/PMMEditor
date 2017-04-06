using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMMEditor.MMDFileParser
{
    public static class Pmx
    {

        public static PmxStruct Read(byte[] data)
        {
            return new PmxReader(data).Read();
        }

        public static PmxStruct ReadFile(string filepath)
        {
            return Read(File.ReadAllBytes(filepath));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMMEditor.MMDFileParser
{
    public static class Pmd
    {
        public static PmdStruct Read(byte[] data)
        {
            return new PmdReader(data).Read();
        }

        public static PmdStruct ReadFile(string path)
        {
            return Read(File.ReadAllBytes(path));
        }

        public static async Task<PmdStruct> ReadFileAsync(string path)
        {
            return await Task.Run(() => ReadFile(path));
        }

        public static async Task<PmdStruct> ReadAsync(byte[] data)
        {
            return await Task.Run(() => Read(data));
        }
    }
}

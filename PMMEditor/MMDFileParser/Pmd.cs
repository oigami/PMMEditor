using System.IO;
using System.Threading.Tasks;

namespace PMMEditor.MMDFileParser
{
    public static class Pmd
    {
        public static bool MagicNumberEqual(byte[] checkData)
        {
            return PmdReader.MagicNumberEqual(checkData);
        }

        public static PmdStruct Read(byte[] data)
        {
            return new PmdReader(data).Read();
        }

        public static PmdStruct ReadFile(string path)
        {
            return Read(File.ReadAllBytes(path));
        }

        public static Task<PmdStruct> ReadFileAsync(string path)
        {
            return Task.Run(() => ReadFile(path));
        }

        public static Task<PmdStruct> ReadAsync(byte[] data)
        {
            return Task.Run(() => Read(data));
        }
    }
}

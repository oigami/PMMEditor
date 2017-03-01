using System.IO;
using System.Threading.Tasks;

namespace PMMEditor.MMDFileParser
{
    public class Pmm
    {
        public static PmmStruct Read(byte[] data)
        {
            return new PmmReader(data).Read();
        }

        public static PmmStruct ReadFile(string path)
        {
            return Read(File.ReadAllBytes(path));
        }


        public static async Task<PmmStruct> ReadFileAsync(string path)
        {
            return await Task.Run(() => ReadFile(path));
        }

        public static async Task<PmmStruct> ReadAsync(byte[] data)
        {
            return await Task.Run(() => Read(data));
        }


        public static void WriteFile(string path, PmmStruct pmm)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                new PmmWriter(stream).Write(pmm);
            }
        }

        public static async Task WriteFileAsync(string path, PmmStruct pmm)
        {
            await Task.Run(() => WriteFile(path, pmm));
        }
    }
}

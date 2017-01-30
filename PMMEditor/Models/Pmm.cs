using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMMEditor.Models
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


        public static async Task<PmmStruct> ReadAsync(string path)
        {
            return await Task.Run(() => ReadFile(path));
        }

        public static async Task<PmmStruct> ReadFileAsync(byte[] data)
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

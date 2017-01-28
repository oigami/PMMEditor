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
        public static PmmStuct Read(byte[] data)
        {
            return new PmmReader(data).Read();
        }

        public static PmmStuct ReadFile(string path)
        {
            return Read(File.ReadAllBytes(path));
        }


        public static async Task<PmmStuct> ReadAsync(string path)
        {
            return await Task.Run(() => ReadFile(path));
        }

        public static async Task<PmmStuct> ReadFileAsync(byte[] data)
        {
            return await Task.Run(() => Read(data));
        }


        public static void WriteFile(string path, PmmStuct pmm)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                new PmmWriter(stream).Write(pmm);
            }
        }

        public static async Task WriteFileAsync(string path, PmmStuct pmm)
        {
            await Task.Run(() => WriteFile(path, pmm));
        }
    }
}

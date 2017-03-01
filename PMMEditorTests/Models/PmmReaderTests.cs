using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PMMEditor.MMDFileParser;
using PMMEditor.Models;

namespace PMMEditorTests.Models
{
    [TestClass]
    public class PmmReaderTests
    {
        [TestMethod]
        public void ReadTest()
        {
            var reader = new PmmReader(File.ReadAllBytes("C:/tool/MikuMikuDance_v926x64/UserFile/サンプル（きしめんAllStar).pmm"));
            try
            {
                var data = reader.Read();
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message + "\n\n" + e.StackTrace);
            }
        }

        [TestMethod]
        public void WriteTest()
        {
            var reader = new PmmReader(File.ReadAllBytes("C:/tool/MikuMikuDance_v926x64/UserFile/サンプル（きしめんAllStar).pmm"));
            var data = reader.Read();

            try
            {
                using (var stream = new FileStream("test_pmm.pmm", FileMode.Create, FileAccess.Write))
                {
                    new PmmWriter(stream).Write(data);
                }
                var writtenData = new PmmReader(File.ReadAllBytes("test_pmm.pmm")).Read();
                var jsonData = JsonConvert.SerializeObject(data);
                var jsonWrittenData = JsonConvert.SerializeObject(writtenData);
                Assert.AreEqual(jsonData, jsonWrittenData);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message + "\n\n" + e.StackTrace);
            }
        }
    }
}

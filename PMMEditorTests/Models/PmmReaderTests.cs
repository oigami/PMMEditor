using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PMMEditor.MMDFileParser;

namespace PMMEditorTests.Models
{
    [TestClass]
    public class PmmReaderTests
    {
        [TestMethod]
        public void PmmReadTest()
        {
            Pmm.ReadFile("C:/tool/MikuMikuDance_v926x64/UserFile/サンプル（きしめんAllStar).pmm");
        }

        [TestMethod]
        public void PmmWriteTest()
        {
            PmmStruct data = Pmm.ReadFile("C:/tool/MikuMikuDance_v926x64/UserFile/サンプル（きしめんAllStar).pmm");

            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                new PmmWriter(stream).Write(data);
                bytes = stream.ToArray();
            }
            PmmStruct writtenData = Pmm.Read(bytes);
            string jsonData = JsonConvert.SerializeObject(data);
            string jsonWrittenData = JsonConvert.SerializeObject(writtenData);
            Assert.AreEqual(jsonData, jsonWrittenData);
        }
    }
}

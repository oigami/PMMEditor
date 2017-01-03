using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PMMEditor.Models.Tests
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
    }
}

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using PMMEditor.MMDFileParser;

namespace PMMEditorTests.Models
{
    [TestClass]
    public class PmdParserTests
    {
        [TestMethod]
        public void PmdReadTest()
        {
            try
            {
                Pmd.ReadFile("C:/tool/MikuMikuDance_v926x64/UserFile/Model/初音ミク.pmd");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message + "\n\n" + e.StackTrace);
                Assert.Fail(e.Message + "\n\n" + e.StackTrace);
            }
        }

    }
}

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PMMEditor.MMDFileParser;

namespace PMMEditorTests.Models
{
    [TestClass]
    public class PmdParserTests
    {
        [TestMethod]
        public void PmdReadTest()
        {
            Assert.IsTrue(
                Pmd.MagicNumberEqual(File.ReadAllBytes("C:/tool/MikuMikuDance_v926x64/UserFile/Model/初音ミク.pmd")));
            Assert.IsFalse(
                Pmd.MagicNumberEqual(File.ReadAllBytes("../../../UserFile/Model/PronamaChan/01_Normal_通常/b.bmp")));

            Pmd.ReadFile("C:/tool/MikuMikuDance_v926x64/UserFile/Model/初音ミク.pmd");
        }
    }
}

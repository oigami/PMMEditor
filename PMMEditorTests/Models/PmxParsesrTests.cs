using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PMMEditor.MMDFileParser;

namespace PMMEditorTests.Models
{
    [TestClass]
    public class PmxParserTests
    {
        [TestMethod]
        public void PmxReadTest()
        {
            Assert.IsTrue(
                Pmx.MagicNumberEqual(File.ReadAllBytes("../../../UserFile/Model/PronamaChan/01_Normal_通常/プロ生ちゃん.pmx")));
            Assert.IsFalse(
                Pmx.MagicNumberEqual(File.ReadAllBytes("../../../UserFile/Model/PronamaChan/01_Normal_通常/b.bmp")));

            Pmx.ReadFile("../../../UserFile/Model/PronamaChan/01_Normal_通常/プロ生ちゃん.pmx");
            Pmx.ReadFile("../../../UserFile/Model/PronamaChan/02_TShirt_Tシャツ/プロ生ちゃんTシャツ.pmx");
            Pmx.ReadFile("../../../UserFile/Model/PronamaChan/03_SD/プロ生ちゃんSD.pmx");
        }
    }
}

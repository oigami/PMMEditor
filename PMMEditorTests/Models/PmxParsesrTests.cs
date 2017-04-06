using System;
using System.Diagnostics;
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
            Pmx.ReadFile("../../../UserFile/Model/PronamaChan/01_Normal_通常/プロ生ちゃん.pmx");
            Pmx.ReadFile("../../../UserFile/Model/PronamaChan/02_TShirt_Tシャツ/プロ生ちゃんTシャツ.pmx");
            Pmx.ReadFile("../../../UserFile/Model/PronamaChan/03_SD/プロ生ちゃんSD.pmx");
        }
    }
}

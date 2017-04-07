using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PMMEditor.MMDFileParser;

namespace PMMEditorTests.Models
{
    [TestClass]
    public class MmdTests
    {
        [TestMethod]
        public void MmdFileKindTest()
        {
            Assert.AreEqual(
                Mmd.FileKind(File.ReadAllBytes("C:/tool/MikuMikuDance_v926x64/UserFile/Model/初音ミク.pmd")), MmdFileKind.Pmd);

            Assert.AreEqual(
                Mmd.FileKind(File.ReadAllBytes("../../../UserFile/Model/PronamaChan/01_Normal_通常/プロ生ちゃん.pmx")), MmdFileKind.Pmx);

            Assert.AreEqual(
                Mmd.FileKind(File.ReadAllBytes("C:/tool/MikuMikuDance_v926x64/UserFile/サンプル（きしめんAllStar).pmm")), MmdFileKind.Pmm);

            Assert.AreEqual(
                Mmd.FileKind(File.ReadAllBytes("../../../UserFile/Model/PronamaChan/01_Normal_通常/hadashi.png")), MmdFileKind.Unknown);
        }
    }
}

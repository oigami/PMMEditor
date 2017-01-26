using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Livet;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace PMMEditor.Models
{
    public class Model : NotificationObject
    {
        /*
         * NotificationObjectはプロパティ変更通知の仕組みを実装したオブジェクトです。
         */

        public void OpenPmm(byte[] pmmData)
        {
            PmmStruct = new PmmReader(pmmData).Read();
        }

        public async Task SavePmmJson(string filename, bool isCompress = false)
        {
            var json = JsonConvert.SerializeObject(PmmStruct, new JsonSerializerSettings
            {
                Culture = new CultureInfo("ja-JP"),
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Local,
                Formatting = Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            });
            if (isCompress)
            {
                using (var fso = new FileStream(filename, FileMode.Create))
                {
                    using (var ds = new ZipArchive(fso, ZipArchiveMode.Create))
                    {
                        var entry = ds.CreateEntry("pmm.json", CompressionLevel.Optimal);
                        using (var stream = entry.Open())
                        {
                            var data = Encoding.GetEncoding("Shift_JIS").GetBytes(json);
                            await stream.WriteAsync(data, 0, data.Length);
                        }
                    }
                }
            }
            else
            {
                File.WriteAllText(filename, json);
            }
        }

        #region PmmStruct変更通知プロパティ

        private PmmStuct _PmmStruct;

        public PmmStuct PmmStruct
        {
            get { return _PmmStruct; }
            set
            {
                if (_PmmStruct == value)
                {
                    return;
                }
                _PmmStruct = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}

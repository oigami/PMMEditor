using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Livet;

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



        #region PmmStruct変更通知プロパティ
        private PmmStuct _PmmStruct;

        public PmmStuct PmmStruct
        {
            get
            { return _PmmStruct; }
            set
            { 
                if (_PmmStruct == value)
                    return;
                _PmmStruct = value;
                RaisePropertyChanged();
            }
        }
        #endregion

    }
}

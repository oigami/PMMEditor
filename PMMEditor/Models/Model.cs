using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        #region ReadWriteFile

        public async Task OpenPmm(byte[] pmmData)
        {
            PmmStruct = await Pmm.ReadAsync(pmmData);
            await MmdAccessoryList.Set(PmmStruct.AccessoryDatas);
            await Camera.Set(PmmStruct.CameraKeyFrames, PmmStruct.CameraInitFrame);
        }

        public async Task SavePmm(string filename)
        {
            await Pmm.WriteFileAsync(filename, PmmStruct);
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
            await Task.Run(() =>
            {
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
                                stream.Write(data, 0, data.Length);
                            }
                        }
                    }
                }
                else
                {
                    File.WriteAllText(filename, json);
                }
            });
        }

        #endregion

        public struct ChangeTimeline
        {
            public bool Model { get; set; }

            public bool Accessory { get; set; }

            public bool Camera { get; set; }

            public bool Gravity { get; set; }

            public bool SelfShadow { get; set; }

            public bool Light { get; set; }
        }

        private static void ChangeFunc<T>(List<T> list, Func<int, int> func) where T : class, PmmStruct.IKeyFrame
        {
            foreach (var frame in list)
            {
                frame.FrameNumber = func(frame.FrameNumber);
                if (frame.FrameNumber == -1)
                {
                    list[frame.PreIndex].NextIndex = frame.NextIndex;
                    list[frame.NextIndex].PreIndex = frame.PreIndex;
                }
            }
        }

        private void OnChangeTimeline(ChangeTimeline isChange, Func<int, int?> changeFrameFunc)
        {
            Func<int, int> changeFunc = frame =>
            {
                var i = changeFrameFunc(frame);
                if ((i ?? 1) <= 0)
                {
                    throw new Exception("frame change error.");
                }
                return i ?? -1;
            };
            if (isChange.Model)
            {
                foreach (var item in PmmStruct.ModelDatas)
                {
                    ChangeFunc(item.BoneKeyFrames, changeFunc);
                    item.BoneKeyFrames = item.BoneKeyFrames.Where(f => f.FrameNumber != -1).ToList();

                    ChangeFunc(item.MorphKeyFrames, changeFunc);
                    item.MorphKeyFrames = item.MorphKeyFrames.Where(f => f.FrameNumber != -1).ToList();
                }
            }
            if (isChange.Accessory)
            {
                foreach (var item in PmmStruct.AccessoryDatas)
                {
                    ChangeFunc(item.KeyFrames, changeFunc);
                    item.KeyFrames = item.KeyFrames.Where(f => f.FrameNumber != -1).ToList();
                }
            }

            if (isChange.Camera)
            {
                ChangeFunc(PmmStruct.CameraKeyFrames, changeFunc);
                PmmStruct.CameraKeyFrames = PmmStruct.CameraKeyFrames.Where(f => f.FrameNumber != -1).ToList();
            }

            if (isChange.Gravity)
            {
                ChangeFunc(PmmStruct.GravityKeyFrames, changeFunc);
                PmmStruct.GravityKeyFrames = PmmStruct.GravityKeyFrames.Where(f => f.FrameNumber != -1).ToList();
            }

            if (isChange.SelfShadow)
            {
                ChangeFunc(PmmStruct.SelfShadowKeyFrames, changeFunc);
                PmmStruct.SelfShadowKeyFrames = PmmStruct.SelfShadowKeyFrames.Where(f => f.FrameNumber != -1).ToList();
            }

            if (isChange.Light)
            {
                ChangeFunc(PmmStruct.LightKeyFrames, changeFunc);
                PmmStruct.LightKeyFrames = PmmStruct.LightKeyFrames.Where(f => f.FrameNumber != -1).ToList();
            }

            RaisePropertyChanged(nameof(PmmStruct));
        }

        public void KeyFrameAddAll(int beginTime, int frameCount)
        {
            OnChangeTimeline(new ChangeTimeline(), i => beginTime <= i ? i + frameCount : i);
        }

        public void KeyFrameDeleteAll(int beginTime, int frameCount)
        {
            OnChangeTimeline(new ChangeTimeline(), i =>
            {
                if (i < beginTime)
                {
                    return i;
                }
                if (i < beginTime + frameCount)
                {
                    return null;
                }
                return i - frameCount;
            });
        }

        #region PmmStruct変更通知プロパティ

        private PmmStruct _PmmStruct;

        public PmmStruct PmmStruct
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

        #region MmdAccessoryList変更通知プロパティ

        private MmdAccessoryList _mmdAccessoryList = new MmdAccessoryList();

        public MmdAccessoryList MmdAccessoryList
        {
            get { return _mmdAccessoryList; }
            set
            {
                if (_mmdAccessoryList == value)
                {
                    return;
                }
                _mmdAccessoryList = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Camera変更通知プロパティ

        private MmdCameraModel _Camera = new MmdCameraModel();

        public MmdCameraModel Camera
        {
            get { return _Camera; }
            set
            {
                if (_Camera == value)
                {
                    return;
                }
                _Camera = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}

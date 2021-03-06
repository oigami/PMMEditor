﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using PMMEditor.ECS;
using PMMEditor.Log;
using PMMEditor.MMDFileParser;
using PMMEditor.Models.Graphics;
using PMMEditor.MVVM;
using Reactive.Bindings.Extensions;

namespace PMMEditor.Models
{
    public class Model : BindableDisposableBase
    {
        public static readonly ECSystem System;
        public ILogger Logger { get; }

        private Entity MainCamera { get; }

        static Model()
        {
            ECSystem.Device = GraphicsModel.Device;
            System = new ECSystem();
        }

        public Model(ILogger logger)
        {
            CompositeDisposables.Add(System);
            Logger = logger;
            FrameControlModel = new FrameControlModel();
            MmdModelList = new MmdModelList(Logger);
            GraphicsModel = new GraphicsModel(Logger, MmdModelList).AddTo(CompositeDisposables);
            MainCamera = System.CreateEntity();
            Camera = MainCamera.AddComponent<CameraControlModel>().Initialize(this);
            CompositeDisposables.Add(Disposable.Create(() => ECObject.Destroy(MainCamera)));
        }

        #region ReadWriteFile

        public async Task OpenPmmAsync(byte[] pmmData)
        {
            try
            {
                PmmStruct = await Pmm.ReadAsync(pmmData);
                MmdModelList.Set(PmmStruct.ModelDatas);
                await Camera.Set(PmmStruct.CameraKeyFrames, PmmStruct.CameraInitFrame);
                await Light.Set(PmmStruct.LightKeyFrames, PmmStruct.LightInitFrame);
                await MmdAccessoryList.Set(PmmStruct.AccessoryDatas);
            }
            catch (Exception e)
            {
                Logger.Error("OpenPmm Error", e);
                PmmStruct = null;
                MmdModelList.Clear();
                Camera.Clear();
                Light.Clear();
                MmdAccessoryList.Clear();
            }
        }

        public async Task OpenPmmAsync(string filepath)
        {
            await OpenPmmAsync(await Task.Run(() => File.ReadAllBytes(filepath)));
        }

        public void OpenPmm(string filepath)
        {
            OpenPmmAsync(filepath).ContinueOnlyOnFaultedErrorLog(Logger);
        }

        public void OpenPmm(byte[] data)
        {
            OpenPmmAsync(data).ContinueOnlyOnFaultedErrorLog(Logger);
        }

        public async Task SavePmm(string filename)
        {
            await Pmm.WriteFileAsync(filename, PmmStruct);
        }

        public async Task SavePmmJson(string filename, bool isCompress = false)
        {
            string json = JsonConvert.SerializeObject(PmmStruct, new JsonSerializerSettings
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
                            ZipArchiveEntry entry = ds.CreateEntry("pmm.json", CompressionLevel.Optimal);
                            using (Stream stream = entry.Open())
                            {
                                byte[] data = Encoding.GetEncoding("Shift_JIS").GetBytes(json);
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
                int? i = changeFrameFunc(frame);
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

        private PmmStruct _pmmStruct;

        public PmmStruct PmmStruct
        {
            get { return _pmmStruct; }
            private set { SetProperty(ref _pmmStruct, value); }
        }

        #endregion

        #region MmdAccessoryList変更通知プロパティ

        public MmdAccessoryList MmdAccessoryList { get; } = new MmdAccessoryList();

        #endregion

        #region MmdModelList変更通知プロパティ

        public MmdModelList MmdModelList { get; }

        #endregion

        #region Camera変更通知プロパティ

        public CameraControlModel Camera { get; }

        #endregion

        #region Light変更通知プロパティ

        public MmdLightModel Light { get; } = new MmdLightModel();

        #endregion

        public FrameControlModel FrameControlModel { get; }

        public GraphicsModel GraphicsModel { get; }

        public void Open(string[] files)
        {
            foreach (var file in files)
            {
                Open(file);
            }
        }

        public void Open(string file)
        {
            try
            {
                var blob = new FileBlob(file);
                byte[] data = blob.Data;
                switch (Mmd.FileKind(data))
                {
                    case MmdFileKind.Pmm:
                        OpenPmm(file);
                        break;
                    case MmdFileKind.Pmd:
                    case MmdFileKind.Pmx:
                        MmdModelList.Add(blob);
                        break;
                    default:
                        try
                        {
                            throw new ArgumentException(file);
                        }
                        catch (Exception e)
                        {
                            Logger.Info("file not match object", e);
                        }

                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error("", e);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using PMMEditor.Log;
using PMMEditor.MMDFileParser;
using PMMEditor.Models.MMDModel;
using PMMEditor.MVVM;
using Reactive.Bindings.Extensions;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;
using Direct3D11 = SharpDX.Direct3D11;

namespace PMMEditor.Models.Graphics
{
    public static class TextureLoader
    {
        private static readonly ImagingFactory2 _factory = new ImagingFactory2();

        /// <summary>
        /// Loads a bitmap using WIC.
        /// </summary>
        /// <param name = "filename"> </param>
        /// <returns> </returns>
        public static BitmapSource LoadBitmap(string filename)
        {
            var bitmapDecoder = new BitmapDecoder(
                _factory,
                filename,
                DecodeOptions.CacheOnDemand
                );

            var formatConverter = new FormatConverter(_factory);

            formatConverter.Initialize(
                bitmapDecoder.GetFrame(0),
                PixelFormat.Format32bppPRGBA,
                BitmapDitherType.None,
                null,
                0.0,
                BitmapPaletteType.Custom);

            return formatConverter;
        }

        /// <summary>
        /// Creates a <see cref = "SharpDX.Direct3D11.Texture2D" /> from a WIC <see cref = "SharpDX.WIC.BitmapSource" />
        /// </summary>
        /// <param name = "device"> The Direct3D11 device </param>
        /// <param name = "bitmapSource"> The WIC bitmap source </param>
        /// <returns> A Texture2D </returns>
        public static Direct3D11.Texture2D CreateTexture2DFromBitmap(
            Direct3D11.Device device, BitmapSource bitmapSource)
        {
            // Allocate DataStream to receive the WIC image pixels
            int stride = bitmapSource.Size.Width * 4;
            using (var buffer = new DataStream(bitmapSource.Size.Height * stride, true, true))
            {
                // Copy the content of the WIC to the buffer
                bitmapSource.CopyPixels(stride, buffer);
                return new Direct3D11.Texture2D(device, new Direct3D11.Texture2DDescription
                {
                    Width = bitmapSource.Size.Width,
                    Height = bitmapSource.Size.Height,
                    ArraySize = 1,
                    BindFlags = Direct3D11.BindFlags.ShaderResource,
                    Usage = Direct3D11.ResourceUsage.Immutable,
                    CpuAccessFlags = Direct3D11.CpuAccessFlags.None,
                    Format = Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = Direct3D11.ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0)
                }, new DataRectangle(buffer.DataPointer, stride));
            }
        }
    }

    public sealed class MmdModelRendererSource : BindableBase, IDisposable
    {
        public MmdModelRendererSource(ILogger logger, MmdModelModel model, Direct3D11.Device device)
        {
            Model = model;
            _device = device ?? throw new ArgumentNullException(nameof(device));

            BoneCount = Model.BoneKeyList.Count;
            Task.Run(() =>
            {
                CreateData();
                IsInitialized = true;
            }).ContinueOnlyOnFaultedErrorLog(logger);
        }

        public int BoneCount { get; }

        private CompositeDisposable _d3DObjectCompositeDisposable2 = new CompositeDisposable();
        private readonly Direct3D11.Device _device;

        #region IsInitialized変更通知プロパティ

        private bool _isInitialized;

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { SetProperty(ref _isInitialized, value); }
        }

        #endregion

        public MmdModelModel Model { get; }

        #region VertexBufferBinding変更通知プロパティ

        private Direct3D11.Buffer _verteBuffer;
        private Direct3D11.VertexBufferBinding _vertexBufferBinding;

        public Direct3D11.VertexBufferBinding VertexBufferBinding
        {
            get { return _vertexBufferBinding; }
            private set { SetProperty(ref _vertexBufferBinding, value); }
        }

        #endregion

        #region IndexBuffer変更通知プロパティ

        private Direct3D11.Buffer _indexBuffer;

        private Format _indexFormat;

        public void SetIndexBuffer(Direct3D11.DeviceContext context)
        {
            context.InputAssembler.SetIndexBuffer(_indexBuffer, _indexFormat, 0);
        }

        #endregion

        #region Materials変更通知プロパティ

        private List<Material> _materials;

        public List<Material> Materials
        {
            get { return _materials; }
            private set { SetProperty(ref _materials, value); }
        }

        #endregion

        #region Materials変更通知プロパティ

        public List<Direct3D11.ShaderResourceView> Textures { get; } = new List<Direct3D11.ShaderResourceView>();

        #endregion

        public class Material
        {
            public int IndexStart { get; set; }

            public int IndexNum { get; set; }

            public int? TexuteIndex { get; set; }

            public RawColor4 Diffuse { get; set; }
        }

        private struct Vertex
        {
            public Vector4 Position { get; set; }

            public Int4 Idx { get; set; }

            public Vector4 Weight { get; set; }

            public Vector2 UV { get; set; }
        }


        private void OnUnload()
        {
            _d3DObjectCompositeDisposable2.Dispose();
            _d3DObjectCompositeDisposable2 = new CompositeDisposable();
            IsInitialized = false;
            VertexBufferBinding = new Direct3D11.VertexBufferBinding();
            _indexBuffer = null;
            Materials = null;
        }

        private void OnLoad()
        {
            IsInitialized = true;
        }

        private void CreateData()
        {
            OnUnload();

            Materials = new List<Material>(Model.Materials.Count);
            int preIndex = 0;
            foreach (var material in Model.Materials)
            {
                var diffuse = new RawColor4(material.Diffuse.R, material.Diffuse.G,
                                            material.Diffuse.B, material.Diffuse.A);
                Materials.Add(new Material
                {
                    IndexNum = (int) material.FaceVertexCount,
                    IndexStart = preIndex,
                    TexuteIndex = material.TextureIndex,
                    Diffuse = diffuse
                });
                preIndex += (int) material.FaceVertexCount;
            }

            // 頂点データ生成
            if (Model.Vertices.Count > 0)
            {
                int typeSize = Utilities.SizeOf<Vertex>();

                // TODO: SDEF の対応
                // 現在はBDEF2へ変換している

                Int4 CreateBoneIndex(PmxStruct.Sdef sdef, IList<PmxStruct.Bdef> bdef)
                {
                    if (sdef != null)
                    {
                        return new Int4(sdef.BoneIndex[0], sdef.BoneIndex[1], 0, 0);
                    }

                    switch (bdef.Count)
                    {
                        case 1:
                            return new Int4(bdef[0].BoneIndex, 0, 0, 0);
                        case 2:
                            return new Int4(bdef[0].BoneIndex, bdef[1].BoneIndex, 0, 0);
                        case 4:
                            return new Int4(bdef[0].BoneIndex, bdef[1].BoneIndex, bdef[2].BoneIndex, bdef[3].BoneIndex);
                    }

                    throw new ArgumentException(nameof(bdef));
                }

                Vector4 CreateBoneWeight(PmxStruct.Sdef sdef, IList<PmxStruct.Bdef> bdef)
                {
                    if (sdef != null)
                    {
                        return new Vector4(sdef.Weight, 1.0f - sdef.Weight, 0, 0);
                    }

                    switch (bdef.Count)
                    {
                        case 1:
                            return new Vector4(bdef[0].Weight, 0, 0, 0);
                        case 2:
                            return new Vector4(bdef[0].Weight, bdef[1].Weight, 0, 0);
                        case 4:
                            return new Vector4(bdef[0].Weight, bdef[1].Weight, bdef[2].Weight, bdef[3].Weight);
                    }

                    throw new ArgumentException(nameof(bdef));
                }

                _verteBuffer = Direct3D11.Buffer.Create(
                    _device,
                    Model.Vertices.Select(_ => new Vertex
                    {
                        Position = new Vector4(_.Position.X, _.Position.Y, _.Position.Z, 1.0f),
                        Weight = CreateBoneWeight(_.Sdef, _.BdefN),
                        Idx = CreateBoneIndex(_.Sdef, _.BdefN),
                        UV = new Vector2(_.UV.X, _.UV.Y)
                    }).ToArray(),
                    new Direct3D11.BufferDescription
                    {
                        SizeInBytes = typeSize * Model.Vertices.Count,
                        Usage = Direct3D11.ResourceUsage.Immutable,
                        BindFlags = Direct3D11.BindFlags.VertexBuffer,
                        StructureByteStride = typeSize
                    }).AddTo(_d3DObjectCompositeDisposable2);

                VertexBufferBinding = new Direct3D11.VertexBufferBinding(_verteBuffer, typeSize, 0);

                // 頂点インデックス生成
                int maxIndex = Model.Indices.Max();

                int indexSize = sizeof(ushort);
                _indexFormat = Format.R16_UInt;
                if (ushort.MaxValue < maxIndex)
                {
                    indexSize = sizeof(int);
                    _indexFormat = Format.R32_UInt;
                }
                else if (maxIndex < byte.MaxValue)
                {
                    indexSize = sizeof(byte);
                    _indexFormat = Format.R8_UInt;
                }
                int indexNum = Model.Indices.Count;
                var bufferDescription = new Direct3D11.BufferDescription
                {
                    SizeInBytes = indexSize * indexNum,
                    Usage = Direct3D11.ResourceUsage.Immutable,
                    BindFlags = Direct3D11.BindFlags.IndexBuffer,
                    StructureByteStride = indexSize
                };
                if (indexSize == sizeof(int))
                {
                    _indexBuffer = Direct3D11.Buffer.Create(_device, Model.Indices.ToArray(), bufferDescription);
                }
                else if (indexSize == sizeof(short))
                {
                    _indexBuffer = Direct3D11.Buffer.Create(_device, Model.Indices.Select(x => (ushort) x).ToArray(), bufferDescription);
                }
                else
                {
                    _indexBuffer = Direct3D11.Buffer.Create(_device, Model.Indices.Select(x => (byte) x).ToArray(), bufferDescription);
                }
                _indexBuffer.AddTo(_d3DObjectCompositeDisposable2);

                // テクスチャ読み込み
                foreach (var texturePath in Model.TextureFilePath)
                {
                    BitmapSource bitmap =
                        TextureLoader.LoadBitmap(Path.Combine(Path.GetDirectoryName(Model.FilePath) ?? "", texturePath));

                    Direct3D11.Texture2D texture = TextureLoader.CreateTexture2DFromBitmap(_device, bitmap);
                    var textureView = new Direct3D11.ShaderResourceView(_device, texture);
                    Textures.Add(textureView);
                }
            }

            OnLoad();
        }

        #region IDisposable Support

        private bool _disposedValue;

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _d3DObjectCompositeDisposable2.Dispose();
                    _d3DObjectCompositeDisposable2 = null;
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}

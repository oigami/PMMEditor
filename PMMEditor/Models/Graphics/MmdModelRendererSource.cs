using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using PMMEditor.ECS;
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

    public interface IMmdModelRendererSource
    {
        MmdModelModel Model { get; }

        List<Direct3D11.ShaderResourceView> Textures { get; }
    }

    public sealed class MmdModelRendererSource : Component, IMmdModelRendererSource
    {
        public void Initialize(ILogger logger, Direct3D11.Device device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            Model = GameObject.GetComponent<MmdModelModel>();
            if (GraphicsModel.FeatureThreading.supportsConcurrentResources)
            {
                CreateData();
            }
            else
            {
                lock (GraphicsModel.SyncObject)
                {
                    CreateData();
                }
            }
            IsInitialized = true;
        }

        private Direct3D11.Device _device;

        #region IsInitialized変更通知プロパティ

        private bool _isInitialized;

        public bool IsInitialized
        {
            get { return _isInitialized; }
            private set { SetProperty(ref _isInitialized, value); }
        }

        #endregion

        public MmdModelModel Model { get; private set; }

        public List<Direct3D11.ShaderResourceView> Textures { get; } = new List<Direct3D11.ShaderResourceView>();

        private struct Vertex
        {
            public Vector4 Position { get; set; }

            public Int4 Idx { get; set; }

            public Vector4 Weight { get; set; }

            public Vector2 UV { get; set; }
        }


        private void OnUnload()
        {
            IsInitialized = false;
        }

        private void OnLoad()
        {
            IsInitialized = true;
        }

        private void CreateData()
        {
            OnUnload();

            // テクスチャ読み込み
            foreach (var texturePath in Model.TextureFilePath)
            {
                BitmapSource bitmap =
                    TextureLoader.LoadBitmap(Path.Combine(Path.GetDirectoryName(Model.FilePath) ?? "", texturePath));

                Direct3D11.Texture2D texture = TextureLoader.CreateTexture2DFromBitmap(_device, bitmap);
                var textureView = new Direct3D11.ShaderResourceView(_device, texture);
                Textures.Add(textureView);
            }

            GameObject.AddComponent<MmdModelRenderer>();
            // メッシュ生成
            if (Model.Vertices.Count > 0)
            {
                GameObject.AddComponent<MeshFilter>().Mesh = CreateMesh();
            }

            OnLoad();
        }

        private Mesh CreateMesh()
        {
            // TODO: SDEF の対応
            // 現在はBDEF2へ変換している
            BoneWeight CreateBoneWeight(PmxStruct.Sdef sdef, IList<PmxStruct.Bdef> bdef)
            {
                if (sdef != null)
                {
                    return new BoneWeight(sdef.BoneIndex[0], sdef.Weight,
                        sdef.BoneIndex[1], 1.0f - sdef.Weight);
                }

                switch (bdef.Count)
                {
                    case 1:
                        return new BoneWeight(bdef[0].BoneIndex, bdef[0].Weight);
                    case 2:
                        return new BoneWeight(bdef[0].BoneIndex, bdef[0].Weight,
                                              bdef[1].BoneIndex, bdef[1].Weight);
                    case 4:
                        return new BoneWeight(bdef[0].BoneIndex, bdef[0].Weight,
                                              bdef[1].BoneIndex, bdef[1].Weight,
                                              bdef[2].BoneIndex, bdef[2].Weight,
                                              bdef[3].BoneIndex, bdef[3].Weight);
                    default:
                        throw new ArgumentException(nameof(bdef));
                }
            }

            var mesh = new Mesh
            {
                Vertices = Model.Vertices.Select(x => new Vector3(x.Position.X, x.Position.Y, x.Position.Z)).ToArray(),
                BoneWeights = Model.Vertices.Select(x => CreateBoneWeight(x.Sdef, x.BdefN)).ToArray(),
                UV = Model.Vertices.Select(x => new Vector2(x.UV.X, x.UV.Y)).ToArray(),

                SubMeshCount = Model.Materials.Count
            };

            // 材質生成
            MmdModelRenderer renderer = GameObject.GetComponent<MmdModelRenderer>();
            var materials = new Material[Model.Materials.Count];
            int preIndex = 0;
            foreach (var (material, i) in Model.Materials.Indexed())
            {
                var diffuse = new RawColor4(material.Diffuse.R, material.Diffuse.G,
                                            material.Diffuse.B, material.Diffuse.A);
                materials[i] = new Material
                {
                    MainTexture = material.TextureIndex != null ? Textures[(int) material.TextureIndex] : null,
                    Diffuse = diffuse
                };
                var faceCount = (int) material.FaceVertexCount;
                mesh.SetTriangles(Model.Indices.GetRange(preIndex, faceCount).ToArray(), i);
                preIndex += faceCount;
            }

            renderer.SharedMaterials = materials;

            return mesh;

        }
    }
}

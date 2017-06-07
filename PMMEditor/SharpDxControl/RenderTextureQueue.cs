using System.Collections.Generic;
using PMMEditor.ECS;
using PMMEditor.Models.Graphics;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace PMMEditor.SharpDxControl
{
    public class RenderTextureQueue
    {
        private readonly Queue<RenderTexture> _renderedQueue =
            new Queue<RenderTexture>(QueueCount);

        private readonly Queue<RenderTexture> _freeTexture2Ds =
            new Queue<RenderTexture>(QueueCount);

        private const int QueueCount = 5;
        private int _updatedCount = 5;
        private Texture2DDescription _texture2DDescription = new Texture2DDescription
        {
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            Format = Format.B8G8R8A8_UNorm,
            Width = 640,
            Height = 480,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            OptionFlags = ResourceOptionFlags.Shared,
            CpuAccessFlags = CpuAccessFlags.None,
            ArraySize = 1
        };

        public Texture2DDescription Texture2DDescription
        {
            get => _texture2DDescription;
            set
            {
                _texture2DDescription = value;
                lock (_freeTexture2Ds)
                {
                    _updatedCount = 0;
                    while (_freeTexture2Ds.Count != 0)
                    {
                        _freeTexture2Ds.Dequeue().Release();
                        _updatedCount++;
                    }

                    lock (_renderedQueue)
                    {
                        while (_renderedQueue.Count != 0)
                        {
                            _renderedQueue.Dequeue().Release();
                            _updatedCount++;
                        }

                        CreateQueue();
                    }
                }
            }
        }

        public RenderTextureQueue()
        {
            CreateQueue();
        }

        public RenderTexture Dequeue()
        {
            lock (_freeTexture2Ds)
            {
                if (_freeTexture2Ds.Count == 0)
                {
                    return null;
                }

                return _freeTexture2Ds.Dequeue();
            }
        }

        public void Enqueue(RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }

            lock (_renderedQueue)
            {
                if (_updatedCount == QueueCount)
                {
                    _renderedQueue.Enqueue(texture);
                }
                else
                {
                    texture.Release();
                    _updatedCount++;
                }
            }
        }

        public RenderTexture DequeueTexture2D()
        {
            lock (_renderedQueue)
            {
                if (_renderedQueue.Count <= 2)
                {
                    return null;
                }

                return _renderedQueue.Dequeue();
            }
        }

        public void EnqueueTexture2D(RenderTexture tex)
        {
            lock (_freeTexture2Ds)
            {
                if (QueueCount <= _updatedCount)
                {
                    _freeTexture2Ds.Enqueue(tex);
                }
                else
                {
                    tex.Release();
                    _updatedCount++;
                }
            }
        }

        private void CreateQueue()
        {
            for (int i = 0; i < QueueCount; i++)
            {
                _freeTexture2Ds.Enqueue(new RenderTexture
                {
                    Format = _texture2DDescription.Format,
                    Width = _texture2DDescription.Width,
                    Height = _texture2DDescription.Height,
                    Depth = 24,
                });
            }
        }
    }
}

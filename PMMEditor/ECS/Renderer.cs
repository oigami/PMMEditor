using System;
using System.Collections.Generic;
using System.Numerics;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace PMMEditor.ECS
{
    public class Material
    {
        public ShaderResourceView MainTexture { get; set; }

        public RawColor4 Diffuse { get; set; }

        public Shader Shader { get; set; }

        private T VariableCast<T>(int nameID, Func<EffectVariable, T> f)
            where T : EffectVariable
        {
            T f1 = f(Shader.GetVariable(nameID));
            return f1.IsValid ? f1 : null;
        }

        public void SetColor(int nameID, RawColor4 color)
        {
            VariableCast(nameID, x => x.AsVector())?.Set(color);
        }

        public void SetFloat(int nameID, float value)
        {
            VariableCast(nameID, x => x.AsScalar()).Set(value);
        }

        public void SetInt(int nameID, int value)
        {
            VariableCast(nameID, x => x.AsScalar()).Set(value);
        }

        public void SetMatrix(int nameID, Matrix4x4 matrix)
        {
            VariableCast(nameID, x => x.AsMatrix())?.SetMatrix(matrix);
        }

        public void SetTexture(int nameID, ShaderResourceView texture)
        {
            VariableCast(nameID, x => x.AsShaderResource()).SetResource(texture);
        }
    }

    public abstract class Renderer : Component
    {
        private int _sortingOrder;

        internal Mesh Mesh { get; set; }

        public Material[] SharedMaterials { get; set; }

        public int SortingOrder
        {
            get => _sortingOrder;
            set
            {
                _sortingOrder = value;
                GameObject.System.UpdateRenderOrder();
            }
        }

        internal abstract object DequeueRenderData();
        internal abstract void EnqueueRenderData(object obj);

        internal abstract void Render(ECSystem.RendererArgs p);
    }

}

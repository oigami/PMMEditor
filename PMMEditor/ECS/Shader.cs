using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace PMMEditor.ECS
{
    public class Shader
    {
        private Dictionary<int, EffectVariable> _effectDictionary = new Dictionary<int, EffectVariable>();
        protected readonly Effect Effect;

        internal Shader(string filename)
        {
            byte[] data = File.ReadAllBytes(filename);
            var byteCode = new ShaderBytecode(data);
            Effect = new Effect(ECSystem.Device, byteCode);
            for (int i = 0, len = Effect.Description.GlobalVariableCount; i < len; i++)
            {
                EffectVariable variableData = Effect.GetVariableByIndex(i);
                _effectDictionary[PropertyToID(variableData.Description.Semantic)] = variableData;
            }
        }

        public EffectVariable GetVariable(int nameID) => _effectDictionary[nameID];

        #region PropertyToID

        private static readonly Dictionary<string, int> _propDictionary = new Dictionary<string, int>();

        public static int PropertyToID(string name)
        {
            if (_propDictionary.TryGetValue(name, out int v))
            {
                return v;
            }

            int newValue = _propDictionary.Count;
            return _propDictionary[name] = newValue;
        }
        #endregion
    }
}

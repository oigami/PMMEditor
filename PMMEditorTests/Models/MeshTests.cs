using System;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PMMEditor.ECS;
using PMMEditor.Log;
using PMMEditor.MMDFileParser;
using PMMEditor.Models.Graphics;
using PMMEditor.Models.MMDModel;
using SharpDX.Direct3D11;

namespace PMMEditorTests.Models
{
    public static class DynamicCastExtension
    {
        public static dynamic AsDynamic<T>(this T obj)
        {
            return new MyDynamicObject<T>(obj);
        }

        private class MyDynamicObject<T> : DynamicObject
        {
            private readonly T _obj;

            public MyDynamicObject(T obj)
            {
                _obj = obj;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                MethodInfo method = typeof(T).GetMethod(binder.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                result = method.Invoke(_obj, args);
                return true;
            }
        }
    }

    [TestClass]
    public class MeshTests
    {
        public readonly Device device = GraphicsModel.Device;
        readonly ECSystem _system = new ECSystem();

        [TestMethod]
        public void MeshSubIndexTest()
        {
            Entity entity = _system.CreateEntity();

            MmdModelModel model = entity.AddComponent<MmdModelModel>();
            model.Set(new FileBlob("../../../UserFile/Model/PronamaChan/01_Normal_通常/プロ生ちゃん.pmx"));

            MmdModelRendererSource source = entity.AddComponent<MmdModelRendererSource>();
            source.Initialize(new LogMessageNotifier(), device);

            Mesh mesh = source.AsDynamic().CreateMesh();

            mesh.UploadMeshData(false);

            int[] indices = mesh.AsDynamic().CreateTrianglesInt();
            ushort[] indicesUShort = mesh.AsDynamic().CreateTrianglesUShort();
            byte[] indicesByte = mesh.AsDynamic().CreateTrianglesByte();
            Assert.AreEqual(sizeof(ushort), mesh.AsDynamic().GetIndexElementSize());
            CollectionAssert.AreEqual(indices, model.Indices, "int");
            CollectionAssert.AreEqual(indicesUShort.Select(x => (int) x).ToArray(), model.Indices, "ushort");
            CollectionAssert.AreNotEquivalent(indicesByte.Select(x => (int) x).ToArray(), model.Indices, "byte");
        }

        [TestMethod]
        public void MeshIndexEelementSizeTest()
        {
            var mesh = new Mesh();
            mesh.SetTriangles(new[] { 0, 10, byte.MaxValue }, 0);
            Assert.AreEqual(sizeof(byte), mesh.AsDynamic().GetIndexElementSize());

            mesh.SetTriangles(new[] { 0, 10, byte.MaxValue + 1 }, 0);
            Assert.AreEqual(sizeof(ushort), mesh.AsDynamic().GetIndexElementSize());

            mesh.SetTriangles(new[] { 0, 10, ushort.MaxValue }, 0);
            Assert.AreEqual(sizeof(ushort), mesh.AsDynamic().GetIndexElementSize());

            mesh.SetTriangles(new[] { 0, 10, ushort.MaxValue + 1 }, 0);
            Assert.AreEqual(sizeof(int), mesh.AsDynamic().GetIndexElementSize());
        }
    }
}

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
using PMMEditor.Models;
using SharpDX.Direct3D11;

namespace PMMEditorTests.Models
{
    public static class DynamicCastExtension
    {
        public static dynamic AsDynamic<T>(this T obj)
        {
            return new MyDynamicObject<T>(obj);
        }

        public static dynamic AsDynamic<T>()
        {
            return new MyDynamicObject<T>(default(T));
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
                MethodInfo method = typeof(T).GetMethod(binder.Name,
                                                        BindingFlags.Instance | BindingFlags.Public
                                                        | BindingFlags.NonPublic);

                result = method.Invoke(_obj, args);
                return true;
            }

            /// <summary>
            /// メンバーの値を設定する操作の実装を提供します。 派生したクラス、 <see cref = "T:System.Dynamic.DynamicObject" />
            /// クラスは、プロパティの値の設定などの操作の動的な動作を指定するには、このメソッドをオーバーライドできます。
            /// </summary>
            /// <param name = "binder">
            /// 動的操作を呼び出したオブジェクトに関する情報を提供します。binder.Name プロパティ値が割り当てられているメンバーの名前を提供します。 たとえば、ステートメント
            /// sampleObject.SampleProperty = "Test", ここで、 sampleObject から派生したクラスのインスタンス、
            /// <see cref = "T:System.Dynamic.DynamicObject" /> クラス、 binder.Name "SampleProperty"を返します。binder.IgnoreCase
            /// プロパティは、メンバー名が大文字小文字を区別するかどうかを指定します。
            /// </param>
            /// <param name = "value">
            /// メンバーに設定する値。 たとえば、 sampleObject.SampleProperty = "Test", ここで、 sampleObject から派生したクラスのインスタンス、
            /// <see cref = "T:System.Dynamic.DynamicObject" /> クラス、 <paramref name = "value" /> は"Test"です。
            /// </param>
            /// <returns> 操作が正常に終了した場合は true。それ以外の場合は false。 このメソッドが戻る場合 false, 、言語の実行時バインダーは、動作を決定します。 (ほとんどの場合、言語固有の実行時例外がスローされます)。 </returns>
            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                Type type = typeof(T);
                PropertyInfo info = type.GetProperty(binder.Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (info == null)
                {
                    return false;
                }

                info.SetValue(null, value);
                return true;
            }
        }
    }

    [TestClass]
    public class MeshTests
    {
        public readonly Device device = GraphicsModel.Device;
        readonly ECSystem _system;

        public MeshTests()
        {
            DynamicCastExtension.AsDynamic<ECSystem>().Device = device;
            _system = new ECSystem(ECSystem.ThreadType.Single);
        }

        [TestMethod]
        public void MeshSubIndexTest()
        {
            Entity entity = _system.CreateEntity();

            MmdModelModel model = entity.AddComponent<MmdModelModel>();
            model.Set(new FileBlob("../../../UserFile/Model/PronamaChan/01_Normal_通常/プロ生ちゃん.pmx"));

            entity.AddComponent<LoggerFilter>().Logger = new LogMessageNotifier();
            MmdModelRendererSource source = entity.AddComponent<MmdModelRendererSource>();
            entity.AddComponent<MmdModelRenderer>();
            _system.Update();

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

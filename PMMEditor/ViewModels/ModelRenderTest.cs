using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using Factory = SharpDX.DirectWrite.Factory;

namespace PMMEditor.ViewModels
{
    public class ModelRenderTest : SharpDxControl.SharpDxControl
    {
        private readonly Factory _fontFactory = new Factory();
        private readonly TextFormat _textFormat;

        public ModelRenderTest()
        {
            _textFormat = new TextFormat(_fontFactory, "Segoe UI", 24.0f);
            BrushManager.Add("Red", t => new SolidColorBrush(t, new RawColor4(1.0f, 0.0f, 0.0f, 1.0f)));
        }

        protected override void Render()
        {
            var target = D2DRenderTarget;
            target.Clear(new RawColor4(0, 0, 0, 0));
            target.DrawEllipse(new Ellipse(new RawVector2(10, 10), 10, 10), BrushManager["Red"]);
        }

        protected override void ResetRenderTarget() {}
    }
}

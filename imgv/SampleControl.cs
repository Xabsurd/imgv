using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using D2dControl;
using SharpDX.Direct2D1;

namespace imgv
{
    internal class SampleControl : D2dControl.D2dControl
    {
        string path = @"D:\Users\absurd\Pictures\art\95494859_p0.jpg";
        Bitmap bitmap;
        public SampleControl()
        {
            bitmap= new Bitmap(new RenderTarget());
        }
        
        public override void Render(RenderTarget target)
        {
            target.DrawBitmap();
        }
    }
}

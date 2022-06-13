using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace imgv
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : System.Windows.Window
    {
        string path = @"D:\Users\absurd\Pictures\art\95494859_p0.jpg";
        WriteableBitmap wBitmap;
        private System.Drawing.Image img;
        private bool canMove = false;
        private Size drawSize = new Size(0, 0);
        private System.Drawing.Point drawPoint = new Point(0, 0);
        private Point downPoint = new Point(0, 0);
        public Window1()
        {
            InitializeComponent();

            this.Loaded += Window1_Loaded;

            this.MouseDown += ImageViewer_MouseDown;
            this.MouseMove += ImageViewer_MouseMove;
            this.MouseUp += ImageViewer_MouseUp;

        }

        private void Window1_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            this.DrawContain();
        }

        private void Window1_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            wBitmap = new WriteableBitmap((int)this.ActualWidth, (int)this.ActualHeight, 72, 72, System.Windows.Media.PixelFormats.Bgra32, null);
            this.baseImage.Source = wBitmap;
            img = System.Drawing.Image.FromFile(path);
            this.SizeChanged += Window1_SizeChanged;
            this.DrawContain();
        }

        private void DrawContain()
        {
            var wb = this.ActualWidth / this.ActualHeight;
            var ib = (double)img.Width / img.Height;
            if (wb > ib)
            {
                drawSize.Width = (int)(this.ActualHeight * ib);
                drawSize.Height = (int)(this.ActualHeight);
                drawPoint.X = (int)((this.ActualWidth - drawSize.Width) / 2);
            }
            else
            {
                drawSize.Width = (int)(this.ActualWidth);
                drawSize.Height = (int)(this.ActualWidth / ib);
                drawPoint.Y = (int)((this.ActualHeight - drawSize.Height) / 2);
            }
            DrawImage(drawPoint, drawSize);
        }
        private void DrawImage(Point point, Size size)
        {
            wBitmap.Lock();
            Bitmap backBitmap = new Bitmap((int)this.ActualWidth, (int)this.ActualHeight, wBitmap.BackBufferStride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, wBitmap.BackBuffer);
            Graphics graphics = Graphics.FromImage(backBitmap);
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.DrawImage(img, new System.Drawing.Rectangle(point, size));
            graphics.Flush();
            graphics.Dispose();
            backBitmap.Dispose();
            wBitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, (int)this.ActualWidth, (int)this.ActualHeight));
            wBitmap.Unlock();

        }
        private void ImageViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((System.Windows.UIElement)e.Source).CaptureMouse();
            this.canMove = true;
            this.downPoint = new Point((int)e.GetPosition(this).X, (int)e.GetPosition(this).Y);
        }

        private void ImageViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((System.Windows.UIElement)e.Source).ReleaseMouseCapture();
            this.canMove = false;
        }

        private void ImageViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (canMove)
            {
                System.Windows.Point movePoint = e.GetPosition(this);
                DrawImage(new System.Drawing.Point((int)(drawPoint.X + (movePoint.X - downPoint.X)), (int)(drawPoint.Y + (movePoint.Y - downPoint.Y))), drawSize);
            }
        }
    }
}

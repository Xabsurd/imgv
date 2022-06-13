using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace imgv
{
    /// <summary>
    /// ImageViewer.xaml 的交互逻辑
    /// </summary>
    public partial class ImageViewer : UserControl
    {
        string path = @"D:\Users\absurd\Pictures\art\95494859_p0.jpg";
        private BitmapSource img;
        private bool contain = true;
        private Size imgSize = new Size(0, 0);
        public int zoom = 1;
        public double zoomStep = 0.1;
        private bool canMove = false;
        private Size drawSize = new Size(0, 0);
        private Point drawPoint = new Point(0, 0);
        private Point downPoint = new Point(0, 0);
        public ImageViewer()
        {
            InitializeComponent();
            BitmapsourceHelp bh = new BitmapsourceHelp();
            BitmapsourceHelp.PictureTypeAndName pictureTypeAndName = bh.GetPictureType(path);
            System.IO.Stream imageStreamSource = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            BitmapDecoder decoder = bh.GetBitmapDecoder(pictureTypeAndName.pictureType, imageStreamSource);
            img = decoder.Frames[0];
            imgSize = new Size(img.PixelWidth, img.PixelHeight);
            //imageStreamSource.Close();
            zoom = 1;
            GC.Collect();
            DrawContain();
            this.SizeChanged += ImageViewer_SizeChanged;
            this.MouseWheel += ImageViewer_MouseWheel;
            this.MouseDown += ImageViewer_MouseDown;
            this.MouseMove += ImageViewer_MouseMove;
            this.MouseUp += ImageViewer_MouseUp;
        }



        public void ZoomUp(Point point)
        {
            if (zoom < 10 - zoomStep)
            {

            }
        }
        public void ZoomDown(Point point)
        {
            if (zoom > 0 + zoomStep)
            {

            }
        }
        private void DrawContain()
        {
            var wb = this.ActualWidth / this.ActualHeight;
            var ib = img.Width / img.Height;
            if (wb > ib)
            {
                drawSize.Width = this.ActualHeight * ib;
                drawSize.Height = this.ActualHeight;
                drawPoint.X = (this.ActualWidth - drawSize.Width) / 2;
            }
            else
            {
                drawSize.Width = this.ActualWidth;
                drawSize.Height = this.ActualWidth / ib;
                drawPoint.Y = (this.ActualHeight - drawSize.Height) / 2;
            }
            DrawImage(drawPoint, drawSize);
        }
        private void DrawImage(Point location, Size size)
        {
            var drawing = this.dvc.drawingVisual.RenderOpen();
            drawing.DrawImage(img, new Rect(location, size));
            drawing.Close();
        }
        private void ImageViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (contain)
            {
                this.DrawContain();
            }
        }
        private void ImageViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)e.Source).CaptureMouse();
            this.canMove = true;
            this.downPoint = e.GetPosition(this);
        }

        private void ImageViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)e.Source).ReleaseMouseCapture();
            this.canMove = false;
        }

        private void ImageViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (canMove)
            {
                Point movePoint = e.GetPosition(this);
                DrawImage(new Point(drawPoint.X + (movePoint.X - downPoint.X), drawPoint.Y + (movePoint.Y - downPoint.Y)), drawSize);
            }
        }
        private void ImageViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point wheelPoint = e.GetPosition(this);
            if (e.Delta > 0)
            {
                ZoomUp(wheelPoint);
            }
            else
            {
                ZoomDown(wheelPoint);
            }
        }
    }
}

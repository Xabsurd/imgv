using OpenCvSharp.WpfExtensions;
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
        public ImageViewer()
        {
            InitializeComponent();
            delayAction = new DelayAction();
            baseImg = new OpenCvSharp.Mat(path);
            baseSource = BitmapSourceConverter.ToBitmapSource(baseImg);
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            bitmapImage = new BitmapImage();
            encoder.Frames.Add(BitmapFrame.Create(baseSource));
            encoder.Save(memoryStream);

            memoryStream.Position = 0;
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();

            //memoryStream.Close();

            OpenCvSharp.Mat mat = new OpenCvSharp.Mat();
            zoom = 1;
            GC.Collect();
            DrawContain();
            this.SizeChanged += ImageViewer_SizeChanged;
            this.MouseWheel += ImageViewer_MouseWheel;
            this.MouseDown += ImageViewer_MouseDown;
            this.MouseMove += ImageViewer_MouseMove;
            this.MouseUp += ImageViewer_MouseUp;
        }
        string path = @"D:\Users\absurd\Pictures\art\95494859.jpg";
        OpenCvSharp.Mat baseImg;
        BitmapImage bitmapImage;
        BitmapSource drawImg;
        BitmapSource baseSource;
        DelayAction delayAction;
        private bool contain = true;
        public int zoom = 1;
        public double zoomStep = 0.1;
        private bool canMove = false;
        private Size drawSize = new Size(0, 0);
        private Point drawPoint = new Point(0, 0);
        private Point downPoint = new Point(0, 0);
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private void ResizeImg(Size size)
        {

            drawImg = baseSource;
            DrawImage(drawPoint, drawSize);
            delayAction.Debounce(100, this.Dispatcher, new Action(async () =>
            {
                watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                OpenCvSharp.Mat mat = new OpenCvSharp.Mat();
                float[] data = new float[9] { 0, -1, 0, -1, 5, -1, 0, -1, 0 };
                OpenCvSharp.Mat kernel = new OpenCvSharp.Mat(3, 3, OpenCvSharp.MatType.CV_32F, data);
                ////baseImg.Resize(1);

                OpenCvSharp.Cv2.Resize(baseImg, mat, new OpenCvSharp.Size(size.Width, size.Height), 2, 2, OpenCvSharp.InterpolationFlags.Area);
                
                

                OpenCvSharp.Cv2.Filter2D(mat, kernel, mat.Type(), kernel, new OpenCvSharp.Point(0, 0));
                //OpenCvSharp.Cv2.ConvertScaleAbs();
                //OpenCvSharp.Cv2.BilateralFilter(mat, mat1, 5, 10, 2);
                drawImg = BitmapSourceConverter.ToBitmapSource(kernel);
                //drawImg = new TransformedBitmap(baseSource, new ScaleTransform(size.Width / baseSource.Width, size.Height / baseSource.Height));
                //drawImg = new NearestScale().Scale(bitmapImage, size.Width / baseSource.Width);
                DrawImage(drawPoint, drawSize);
                watch.Stop();
                Debug.WriteLine(watch.Elapsed.TotalMilliseconds);
            }));

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
            var ib = (double)baseImg.Width / (double)baseImg.Height;
            if (wb > ib)
            {
                drawSize.Width = this.ActualHeight * ib;
                drawSize.Height = this.ActualHeight;
                drawPoint.X = (this.ActualWidth - drawSize.Width) / 2;
                drawPoint.Y = 0;
            }
            else
            {
                drawSize.Width = this.ActualWidth;
                drawSize.Height = this.ActualWidth / ib;
                drawPoint.X = 0;
                drawPoint.Y = (this.ActualHeight - drawSize.Height) / 2;
            }
            ResizeImg(drawSize);
            //DrawImage(drawPoint, drawSize);
        }
        private void DrawImage(Point location, Size size)
        {
            var drawing = this.dvc.drawingVisual.RenderOpen();
            drawing.DrawImage(drawImg, new Rect(location, size));
            drawing.Close();
        }
        private void ImageViewer_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (contain)
            {
                this.DrawContain();
            }
        }
        private void ImageViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((System.Windows.UIElement)e.Source).CaptureMouse();
            this.canMove = true;
            this.downPoint = e.GetPosition(this);
        }

        private void ImageViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((System.Windows.UIElement)e.Source).ReleaseMouseCapture();
            if (canMove)
            {
                Point movePoint = e.GetPosition(this);
                drawPoint = new Point(drawPoint.X + (movePoint.X - downPoint.X), drawPoint.Y + (movePoint.Y - downPoint.Y));
                DrawImage(drawPoint, drawSize);

            }
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

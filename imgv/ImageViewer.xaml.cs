
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
using OpenCvSharp.WpfExtensions;
using Cv2 = OpenCvSharp.Cv2;
using Mat = OpenCvSharp.Mat;
using MatType = OpenCvSharp.MatType;
using InterpolationFlags = OpenCvSharp.InterpolationFlags;
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
            baseImg = new Mat(path);
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

            Mat mat = new Mat();
            zoom = 1;
            GC.Collect();
            ChangeRotate(15);
            DrawContain();
            this.SizeChanged += ImageViewer_SizeChanged;
            this.MouseWheel += ImageViewer_MouseWheel;
            this.MouseDown += ImageViewer_MouseDown;
            this.MouseMove += ImageViewer_MouseMove;
            this.MouseUp += ImageViewer_MouseUp;
        }
        string path = @"D:\Users\absurd\Pictures\art\95494859.jpg";
        Mat baseImg;
        BitmapImage bitmapImage;
        BitmapSource drawImg;
        BitmapSource baseSource;
        DelayAction delayAction;
        IRect baseRect= new IRect();
        float sharpness = 1;
        private bool contain = true;
        private bool drawBase = true;
        public double zoom = 1;
        public double zoomStep = 0.1;
        private bool canMove = false;
        private Size drawSize = new Size(0, 0);
        private Point drawPoint = new Point(0, 0);
        private Point downPoint = new Point(0, 0);
        private Point downPoint_r = new Point(0, 0);
        RotateTransform rotate = new RotateTransform(0);
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private void ResizeImg(Size size)
        {
            drawBase = true;
            DrawImage(drawPoint, drawSize);
            //drawImg = baseSource;
            if (zoom < 1)
            {

                delayAction.Debounce(100, this.Dispatcher, new Action(() =>
            {
                new Task<bool>(() =>
                 {
                     drawBase = false;
                     watch = new System.Diagnostics.Stopwatch();
                     watch.Start();
                     Mat mat = new Mat();
                     Cv2.Resize(baseImg, mat, new OpenCvSharp.Size(size.Width, size.Height), 2, 2, InterpolationFlags.Area);
                     Mat mat1 = new Mat();
                     if (sharpness > 1)
                     {
                         float h = (sharpness - 1) / -8;
                         float[] data = new float[9] { h, h, h, h, sharpness, h, h, h, h };
                         Mat kernel = new Mat(3, 3, MatType.CV_32F, data);
                         Cv2.Filter2D(mat, mat1, mat.Type(), kernel);
                     }
                     this.Dispatcher.Invoke(() =>
                     {
                         if (sharpness > 1)
                         {


                             drawImg = BitmapSourceConverter.ToBitmapSource(mat1);
                         }
                         else
                         {
                             //Mat rotMat = Cv2.GetRotationMatrix2D(new Point2f(0, 0), 45, 1);
                             //Mat mat1 = new Mat();
                             //Cv2.WarpAffine(mat,mat1,rotMat,mat.Size());
                             drawImg = BitmapSourceConverter.ToBitmapSource(mat);
                         }
                         DrawImage(drawPoint, drawSize);
                     });

                     watch.Stop();
                     Debug.WriteLine("渲染用时" + watch.Elapsed.TotalMilliseconds + "ms");
                     return true;
                 }).Start();

            }));
            }


        }
        public void ZoomUp(Point point)
        {
            if (zoom < 10 - zoomStep)
            {
                ZoomTo(point, zoom + zoomStep);
            }
        }

        public void ZoomDown(Point point)
        {
            if (zoom > 0 + zoomStep)
            {
                ZoomTo(point, zoom - zoomStep);
            }
        }
        public void ZoomTo(Point point, double z)
        {
            Point p = RotatePoint(new Point(0, 0), point, rotate.Angle * -1);
            double x = p.X - drawPoint.X;
            double y = p.Y - drawPoint.Y;
            drawPoint.X += x - (x / zoom * z);
            drawPoint.Y += y - (y / zoom * z);
            zoom = z;
            this.drawSize = new Size(baseImg.Width * zoom, baseImg.Height * zoom);
            ResizeImg(drawSize);
        }
        public void ChangeRotate(double angle)
        {
            rotate.Angle = angle;
            baseRect = RotateRect(new Point(0, 0), new Rect(new Point(0, 0), new Size(baseImg.Width, baseImg.Height)), rotate.Angle);
        }
        private void DrawContain()
        {
            var wb = this.ActualWidth / this.ActualHeight;
            var ib = baseRect.bound.Width / baseRect.bound.Height;
            if (wb > ib)
            {
                zoom = this.ActualHeight / baseRect.bound.Height;
                drawSize.Width = baseImg.Width * zoom;
                drawSize.Height = baseImg.Height * zoom;
                drawPoint.X = baseRect.tl.X - baseRect.bound.Left * zoom + (this.ActualWidth - baseRect.bound.Width * zoom) / 2;
                drawPoint.Y = baseRect.tl.Y - baseRect.bound.Top * zoom;
            }
            else
            {
                zoom = this.ActualWidth / baseRect.bound.Width;
                drawSize.Width = baseImg.Width * zoom;
                drawSize.Height = baseImg.Height * zoom;
                drawPoint.X = baseRect.tl.X - baseRect.bound.Left * zoom;
                drawPoint.Y = baseRect.tl.Y - baseRect.bound.Top * zoom + (this.ActualHeight - baseRect.bound.Height * zoom) / 2;
            }
            drawPoint = RotatePoint(new Point(0, 0), drawPoint, rotate.Angle * -1);

            Debug.WriteLine("DrawContain" + zoom);
            ResizeImg(drawSize);
            //DrawImage(drawPoint, drawSize);
        }
        private void DrawImage(Point location, Size size)
        {
            var drawing = this.dvc.drawingVisual.RenderOpen();
            drawing.PushTransform(rotate);
            //drawing.PushTransform(new ScaleTransform(-1,1));
            if (drawBase)
            {
                drawing.DrawImage(baseSource, new Rect(location, size));
            }
            else
            {
                drawing.DrawImage(drawImg, new Rect(location, size));
            }


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
            this.downPoint_r = drawPoint;
        }

        private void ImageViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((System.Windows.UIElement)e.Source).ReleaseMouseCapture();
            if (canMove)
            {
                Point movePoint = e.GetPosition(this);
                Point p = RotatePoint(new Point(0, 0), new Point((movePoint.X - downPoint.X), (movePoint.Y - downPoint.Y)), rotate.Angle * -1);
                drawPoint = new Point(downPoint_r.X + p.X, downPoint_r.Y + p.Y);
                DrawImage(drawPoint, drawSize);
            }
            this.canMove = false;
        }

        private void ImageViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (canMove)
            {
                Point movePoint = e.GetPosition(this);
                Point p = RotatePoint(new Point(0, 0), new Point((movePoint.X - downPoint.X), (movePoint.Y - downPoint.Y)), rotate.Angle * -1);
                drawPoint = new Point(downPoint_r.X + p.X, downPoint_r.Y + p.Y);
                Debug.WriteLine(drawPoint);
                Debug.WriteLine(downPoint_r);
                Debug.WriteLine(downPoint);

                DrawImage(drawPoint, drawSize);
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
        public IRect RotateRect(Point p, Rect rect, double angle)
        {
            Point p1 = RotatePoint(p, rect.TopLeft, angle);
            Point p2 = RotatePoint(p, rect.TopRight, angle);
            Point p3 = RotatePoint(p, rect.BottomLeft, angle);
            Point p4 = RotatePoint(p, rect.BottomRight, angle);
            Point p5 = new Point(Math.Min(Math.Min(Math.Min(p1.X, p2.X), p3.X), p4.X), Math.Min(Math.Min(Math.Min(p1.Y, p2.Y), p3.Y), p4.Y));
            Point p6 = new Point(Math.Max(Math.Max(Math.Max(p1.X, p2.X), p3.X), p4.X), Math.Max(Math.Max(Math.Max(p1.Y, p2.Y), p3.Y), p4.Y));
            return new IRect()
            {
                tl = p1,
                tr = p2,
                bl = p3,
                br = p4,
                bound = new Rect(p5, p6)
            };
        }
        //p2点绕p1点旋转angle度后的坐标
        public Point RotatePoint(Point p1, Point p2, double angle)
        {
            double radian = angle / 180 * Math.PI;
            double x = p2.X - p1.X;
            double y = p2.Y - p1.Y;
            double x1 = x * Math.Cos(radian) - y * Math.Sin(radian) + p1.X;
            double y1 = x * Math.Sin(radian) + y * Math.Cos(radian) + p1.Y;
            return new Point(x1, y1);
        }
    }
    public class IRect
    {
        public Point tl { get; set; }
        public Point tr { get; set; }
        public Point bl { get; set; }
        public Point br { get; set; }
        public Rect bound { get; set; }
    }
}

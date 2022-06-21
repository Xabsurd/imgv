
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
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using ImageAnimator = System.Drawing.ImageAnimator;
using System.Windows.Interop;

namespace imgv
{
    /// <summary>
    /// ImageViewer.xaml 的交互逻辑
    /// </summary>
    public partial class ImageViewer : UserControl
    {

        BitmapSource baseSource;
        private System.Drawing.Bitmap gifBitmap;// gif动画的System.Drawing.Bitmap
        IRect baseRect = new IRect();
        public bool contain = true;
        public double zoom = 1;
        public double minZoom = 1;
        public double maxZoom = 100;
        public double zoomStep = 0.1;
        private bool canMove = false;
        private bool canScale = false;
        private bool isAnima = false;
        private Size drawSize = new Size(0, 0);
        private Point drawPoint = new Point(0, 0);
        private Point downPoint = new Point(0, 0);
        private Point downPoint_D = new Point(0, 0);
        private double downZoom = 1;
        IRect downRect;
        RotateTransform rotate = new RotateTransform(0);

        public ImageViewer()
        {
            InitializeComponent();

            var property = typeof(Visual).GetProperty("VisualBitmapScalingMode",
               BindingFlags.NonPublic | BindingFlags.Instance);

            property.SetValue(this.dvc.drawingVisual, BitmapScalingMode.Fant);
            var property1 = typeof(Visual).GetProperty("VisualEdgeMode",
               BindingFlags.NonPublic | BindingFlags.Instance);

            property1.SetValue(this.dvc.drawingVisual, EdgeMode.Unspecified);
            this.Loaded += ImageViewer_Loaded;


        }

        private void ImageViewer_Loaded(object sender, RoutedEventArgs e)
        {
            this.SizeChanged += ImageViewer_SizeChanged;
            this.MouseWheel += ImageViewer_MouseWheel;
            this.MouseDown += ImageViewer_MouseDown;
            this.MouseMove += ImageViewer_MouseMove;
            this.MouseUp += ImageViewer_MouseUp;
            //InitImage(@"D:\Users\absurd\Pictures\art\95494859.jpg");
        }

        public void InitImage(string path)
        {
            BitmapsourceHelp bh = new BitmapsourceHelp();
            BitmapsourceHelp.PictureTypeAndName type = bh.GetPictureType(path);
            ClearGif();
            if (type != null)
            {
                if (type.name != "gif")
                {
                    BitmapDecoder bitmapDecoder = BitmapDecoder.Create(
                      new Uri(path, UriKind.Relative),
                      BitmapCreateOptions.None,
                      BitmapCacheOption.Default);
                    baseSource = bitmapDecoder.Frames[0];

                    isAnima = false;
                    ChangeRotate(0);
                    ResizeToContain();
                }
                else
                {
                    this.gifBitmap = new System.Drawing.Bitmap(path);
                    this.GetBitmapSource();
                    ChangeRotate(0);
                    ResizeToContain();
                    StartAnimate();
                }

            }
        }


        private void DrawImage(Point location, Size size)
        {
            var drawing = this.dvc.drawingVisual.RenderOpen();
            drawing.PushTransform(rotate);
            drawing.DrawImage(baseSource, new Rect(location, size));
            drawing.Close();
        }
        private void DrawImage()
        {
            this.DrawImage(drawPoint, drawSize);
        }

        public void ChangeRotate(double angle)
        {

            rotate.Angle = angle;
            baseRect = RotateRect(new Point(0, 0), new Rect(new Point(0, 0), new Size(baseSource.PixelWidth, baseSource.PixelHeight)), rotate.Angle);
        }
        public void ResizeToContain()
        {
            contain = true;
            var wb = this.ActualWidth / this.ActualHeight;
            var ib = baseRect.bound.Width / baseRect.bound.Height;
            if (wb > ib)
            {
                zoom = this.ActualHeight / baseRect.bound.Height;
                drawSize.Width = baseSource.PixelWidth * zoom;
                drawSize.Height = baseSource.PixelHeight * zoom;
                drawPoint.X = baseRect.tl.X - baseRect.bound.Left * zoom + (this.ActualWidth - baseRect.bound.Width * zoom) / 2;
                drawPoint.Y = baseRect.tl.Y - baseRect.bound.Top * zoom;
            }
            else
            {
                zoom = this.ActualWidth / baseRect.bound.Width;
                drawSize.Width = baseSource.PixelWidth * zoom;
                drawSize.Height = baseSource.PixelHeight * zoom;
                drawPoint.X = baseRect.tl.X - baseRect.bound.Left * zoom;
                drawPoint.Y = baseRect.tl.Y - baseRect.bound.Top * zoom + (this.ActualHeight - baseRect.bound.Height * zoom) / 2;
            }
            drawPoint = RotatePoint(new Point(0, 0), drawPoint, rotate.Angle * -1);
            minZoom = zoom > 1 ? 1 : zoom;
            DrawImage(drawPoint, drawSize);
        }
        public void ResizeToTile()
        {
            contain = false;
            drawSize = new Size(baseSource.PixelWidth, baseSource.PixelHeight);
            IRect ir = RotateRect(new Point(0, 0), new Rect(new Point(0, 0), drawSize), rotate.Angle);
            double diff_width = this.ActualWidth - ir.bound.Width;
            double diff_height = this.ActualHeight - ir.bound.Height;
            double diff_x = ir.tl.X - ir.bound.Left;
            double diff_y = ir.tl.Y - ir.bound.Top;
            drawPoint = RotatePoint(new Point(0, 0), new Point(diff_width / 2 + diff_x, diff_height / 2 + diff_y), rotate.Angle * -1);
            zoom = 1;
            DrawImage(drawPoint, drawSize);
        }
        public void ClearGif()
        {
            StopAnimate();
            if (gifBitmap != null)
            {
                this.gifBitmap.Dispose();
                this.gifBitmap = null;
            }
            if (this.baseSource != null)
            {
                baseSource.Freeze();
                baseSource = null;
            }
            GC.Collect();

        }
        /// <summary>
        /// Start
        /// </summary>
        public void StartAnimate()
        {
            ImageAnimator.Animate(this.gifBitmap, this.OnFrameChanged);
        }

        /// <summary>
        /// Stop
        /// </summary>
        public void StopAnimate()
        {
            ImageAnimator.StopAnimate(this.gifBitmap, this.OnFrameChanged);
        }
        /// <summary>
        /// 从System.Drawing.Bitmap中获得用于显示的那一帧图像的BitmapSource
        /// </summary>
        /// <returns></returns>
        private void GetBitmapSource()
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = this.gifBitmap.GetHbitmap();
                this.baseSource = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    DeleteObject(handle);
                }
            }
            //return this.baseSource;
        }
        /// <summary>
        /// 帧处理
        /// </summary>
        private void OnFrameChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (this.gifBitmap != null)
                {
                    ImageAnimator.UpdateFrames(); // 更新到下一帧
                    if (this.baseSource != null)
                    {
                        this.baseSource.Freeze();
                    }
                    this.GetBitmapSource();
                    DrawImage();
                    this.InvalidateVisual();
                }
            }));
        }
        private void ImageViewer_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (contain)
            {
                this.ResizeToContain();
            }
            else
            {
                var wb = this.ActualWidth / this.ActualHeight;
                var ib = baseRect.bound.Width / baseRect.bound.Height;
                if (wb > ib)
                {
                    minZoom = this.ActualHeight / baseRect.bound.Height;
                }
                else
                {
                    minZoom = this.ActualWidth / baseRect.bound.Width;
                }
                minZoom = minZoom > 1 ? 1 : minZoom;
                double diff_width = this.ActualWidth - baseRect.bound.Width * zoom;
                double diff_height = this.ActualHeight - baseRect.bound.Height * zoom;
                double diff_x = (baseRect.tl.X - baseRect.bound.Left) * zoom;
                double diff_y = (baseRect.tl.Y - baseRect.bound.Top) * zoom;
                bool reDraw = false;
                Point move = new Point(0, 0);
                Point v_p = RotatePoint(move, drawPoint, rotate.Angle);
                if (diff_width > 0)
                {
                    reDraw = true;

                    move.X = diff_width / 2 + diff_x;

                }
                else
                {
                    if (baseRect.bound.Width * zoom + v_p.X - diff_x < this.ActualWidth)
                    {
                        reDraw = true;
                        move.X = this.ActualWidth - baseRect.bound.Width * zoom + diff_x;
                    }
                    else
                    {
                        move.X = v_p.X;
                    }

                }
                if (diff_height > 0)
                {
                    reDraw = true;

                    move.Y = diff_height / 2 + diff_y;
                }
                else
                {
                    if (baseRect.bound.Height * zoom + v_p.Y - diff_y < this.ActualHeight)
                    {
                        reDraw = true;
                        move.Y = this.ActualHeight - baseRect.bound.Height * zoom + diff_y;
                    }
                    else
                    {
                        move.Y = v_p.Y;
                    }
                }
                if (reDraw)
                {
                    drawPoint = RotatePoint(new Point(0, 0), move, rotate.Angle * -1);
                    DrawImage(drawPoint, drawSize);
                }
            }
        }
        private void ImageViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((System.Windows.UIElement)e.Source).CaptureMouse();
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.canMove = true;
                this.downPoint = e.GetPosition(this);
                this.downPoint_D = RotatePoint(new Point(0, 0), drawPoint, rotate.Angle);
                downRect = RotateRect(new Point(0, 0), new Rect(drawPoint, drawSize), rotate.Angle);
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                canScale = true;
                downZoom = zoom;
                this.downPoint = e.GetPosition(this);
            }

        }

        private void ImageViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((System.Windows.UIElement)e.Source).ReleaseMouseCapture();
            this.canMove = false;
            this.canScale = false;
        }

        private void ImageViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (canMove)
            {
                Point mousePoint = e.GetPosition(this);
                double left = mousePoint.X - downPoint.X;
                double top = mousePoint.Y - downPoint.Y;
                Point move = new Point();


                double diff_width = this.ActualWidth - downRect.bound.Width;
                double diff_height = this.ActualHeight - downRect.bound.Height;
                double diff_x = downRect.tl.X - downRect.bound.Left;
                double diff_y = downRect.tl.Y - downRect.bound.Top;
                if (downRect.bound.Width > this.ActualWidth)
                {
                    if (downRect.bound.Left + left < 0)
                    {
                        if (downRect.bound.Left + left > diff_width)
                        {
                            move.X = downPoint_D.X + left;
                        }
                        else
                        {
                            move.X = diff_width + diff_x;
                        }
                    }
                    else
                    {
                        move.X = diff_x;
                    }
                }
                else
                {
                    move.X = diff_width / 2 + diff_x;
                }
                if (downRect.bound.Height > this.ActualHeight)
                {
                    if (downRect.bound.Top + top < 0)
                    {
                        if (downRect.bound.Top + top > diff_height)
                        {
                            move.Y = downPoint_D.Y + top;
                        }
                        else
                        {
                            move.Y = diff_height + diff_y;
                        }
                    }
                    else
                    {
                        move.Y = diff_y;
                    }
                }
                else
                {
                    move.Y = diff_height / 2 + diff_y;
                }
                drawPoint = RotatePoint(new Point(0, 0), move, rotate.Angle * -1);

                DrawImage(drawPoint, drawSize);
            }
            else if (canScale)
            {
                Point mousePoint = e.GetPosition(this);
                double diff_y = mousePoint.Y - downPoint.Y;
                double s = downZoom + diff_y / -200;
                s = limitedZoom(s);
                if (s != zoom && s > 0)
                {
                    ZoomTo(downPoint, s);
                }

            }
        }
        private void ImageViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point wheelPoint = e.GetPosition(this);
            double s = 0;
            if (e.Delta > 0)
            {
                s = zoom + zoom * zoomStep;
            }
            else
            {
                s = zoom - zoom * zoomStep;
            }
            s = limitedZoom(s);
            if (s != zoom && s > 0)
            {
                ZoomTo(wheelPoint, s);
                if (this.canMove)
                {
                    this.downPoint = e.GetPosition(this);
                    this.downPoint_D = RotatePoint(new Point(0, 0), drawPoint, rotate.Angle);
                    downRect = RotateRect(new Point(0, 0), new Rect(drawPoint, drawSize), rotate.Angle);
                }
            }

        }
        public double limitedZoom(double s)
        {
            if (s > maxZoom)
            {
                s = maxZoom;
            }
            else if (s < minZoom)
            {
                s = minZoom;
            }
            if (s == minZoom)
            {
                if (baseSource.PixelHeight < this.ActualHeight && baseSource.PixelWidth < this.ActualWidth)
                {
                    ResizeToTile();
                }
                else
                {
                    ResizeToContain();
                }


                return -1;
            }
            else
            {
                contain = false;
            }
            return s;
        }
        public void ZoomTo(Point point, double z)
        {
            this.drawSize = new Size(baseSource.PixelWidth * z, baseSource.PixelHeight * z);
            //视觉矩形
            IRect v_Rect = RotateRect(new Point(0, 0), new Rect(drawPoint, drawSize), rotate.Angle);
            Point move = new Point();

            double x = point.X - v_Rect.tl.X;
            double y = point.Y - v_Rect.tl.Y;
            double m_x = x - (x / zoom * z);
            double m_y = y - (y / zoom * z);
            double diff_width = this.ActualWidth - v_Rect.bound.Width;
            double diff_height = this.ActualHeight - v_Rect.bound.Height;
            double diff_x = v_Rect.tl.X - v_Rect.bound.Left;
            double diff_y = v_Rect.tl.Y - v_Rect.bound.Top;
            if (diff_width < 0)
            {
                if (v_Rect.bound.Left + m_x < 0)
                {
                    if (v_Rect.bound.Left + m_x > diff_width)
                    {
                        move.X = v_Rect.tl.X + m_x;
                    }
                    else
                    {
                        move.X = diff_width + diff_x;
                    }
                }
                else
                {
                    move.X = diff_x;
                }
            }
            else
            {
                move.X = diff_width / 2 + diff_x;
            }
            if (diff_height < 0)
            {
                if (v_Rect.bound.Top + m_y < 0)
                {
                    if (v_Rect.bound.Top + m_y > diff_height)
                    {
                        move.Y = v_Rect.tl.Y + m_y;
                    }
                    else
                    {
                        move.Y = diff_height + diff_y;
                    }
                }
                else
                {
                    move.Y = diff_y;
                }
            }
            else
            {
                move.Y = diff_height / 2 + diff_y;
            }

            //move.X = v_Rect.tl.X + m_x;
            //move.Y = v_Rect.tl.Y + m_y;
            drawPoint = RotatePoint(new Point(0, 0), move, rotate.Angle * -1);
            zoom = z;
            DrawImage(drawPoint, drawSize);
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

        /// <summary>
        /// 删除本地 bitmap resource
        /// </summary>
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject(IntPtr hObject);

        private enum FrameDisposalMethod
        {
            Replace = 0,
            Combine = 1,
            RestoreBackground = 2,
            RestorePrevious = 3
        }
        private class FrameInfo
        {
            public int Delay { get; set; }
            public FrameDisposalMethod DisposalMethod { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Left { get; set; }
            public double Top { get; set; }
            public Rect Rect
            {
                get { return new Rect(Left, Top, Width, Height); }
            }

        }
        private class GifFrame
        {
            public FrameInfo info { get; set; }
            public BitmapSource frame { get; set; }
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

}

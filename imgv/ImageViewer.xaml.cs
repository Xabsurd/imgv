
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

namespace imgv
{
    /// <summary>
    /// ImageViewer.xaml 的交互逻辑
    /// </summary>
    public partial class ImageViewer : UserControl
    {
        BitmapDecoder uriBitmap;
        BitmapSource baseSource;
        DelayAction delayAction;
        IRect baseRect = new IRect();
        float sharpness = 1;
        public bool contain = true;
        public bool drawBase = true;
        public double zoom = 1;
        public double minZoom = 1;
        public double maxZoom = 100;
        public double zoomStep = 0.1;
        private bool canMove = false;
        private bool canScale = false;
        private Size drawSize = new Size(0, 0);
        private Point drawPoint = new Point(0, 0);


        private Point downPoint = new Point(0, 0);
        private Point downPoint_D = new Point(0, 0);
        private double downZoom = 1;
        IRect downRect;
        RotateTransform rotate = new RotateTransform(0);
        private int frameIndex = 0;
        private bool isAnima = false;
        Thread animationThread;
        Stopwatch stopwatch = new Stopwatch();
        List<GifFrame> gifFrames = new List<GifFrame>();
        DrawingVisual processVisual = new DrawingVisual();
        RenderTargetBitmap processRender;
        public ImageViewer()
        {
            InitializeComponent();
            delayAction = new DelayAction();


            //RenderOptions.SetEdgeMode((DependencyObject)this.dvc.drawingVisual, EdgeMode.Aliased);
            //RenderOptions.SetBitmapScalingMode((DependencyObject)this.dvc.drawingVisual, BitmapScalingMode.Fant);
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
            //media.Open(new Uri(@"D:\Users\absurd\Desktop\outputgif\5.gif", UriKind.Relative));

            //media.Play();
            //media.MediaEnded += (o, e) =>
            //{
            //    media.Position = TimeSpan.Zero;
            //    media.Play();
            //};
            //var drawing = this.dvc.drawingVisual.RenderOpen();
            //drawing.DrawVideo(media, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
            //drawing.Close();
            this.SizeChanged += ImageViewer_SizeChanged;
            this.MouseWheel += ImageViewer_MouseWheel;
            this.MouseDown += ImageViewer_MouseDown;
            this.MouseMove += ImageViewer_MouseMove;
            this.MouseUp += ImageViewer_MouseUp;
            InitImage(@"D:\Users\absurd\Pictures\art\95494859.jpg");

        }

        public void InitImage(string path)
        {
            isAnima = true;
            BitmapsourceHelp bh = new BitmapsourceHelp();
            BitmapsourceHelp.PictureTypeAndName type = bh.GetPictureType(path);
            if (type != null)
            {
                uriBitmap = BitmapDecoder.Create(
              new Uri(path, UriKind.Relative),
              BitmapCreateOptions.None,
              BitmapCacheOption.Default);
                //BitmapMetadata meta = uriBitmap.Frames[0].Metadata as BitmapMetadata;
                //var a =  meta.GetQuery("/grctlext/Delay");
                baseSource = uriBitmap.Frames[0];
                //FrameInfo info = GetFrameInfo(uriBitmap.Frames[0]);
                //return;
                isAnima = false;
                ChangeRotate(0);
                ResizeToContain();
                if (uriBitmap.Frames.Count > 1)
                {
                    isAnima = true;
                    gifFrames = new List<GifFrame>();


                    for (int i = 0; i < uriBitmap.Frames.Count; i++)
                    {
                        if (i > 0)
                        {
                            var frame = uriBitmap.Frames[i];
                            var info = GetFrameInfo(frame);

                            gifFrames.Add(new GifFrame()
                            {
                                info = info,
                                frame = MakeFrame(gifFrames[0].frame, frame, info, gifFrames[i - 1].frame, gifFrames[i - 1].info)
                            });

                        }
                        else
                        {
                            gifFrames.Add(new GifFrame() { info = GetFrameInfo(uriBitmap.Frames[i]), frame = uriBitmap.Frames[i] });
                        }

                    }

                    frameIndex = 0;
                    try
                    {
                        if (animationThread == null)
                        {

                            animationThread = new Thread(ChangeGifFrame);
                            animationThread.Start();
                        }
                    }
                    catch (Exception)
                    {

                        Debug.WriteLine("线程关闭");
                    }


                }
                //if (animationThread != null)
                //{
                //    animationThread.t();

                //}

                GC.Collect();
            }

        }
        private void DrawImage(Point location, Size size)
        {
            var drawing = this.dvc.drawingVisual.RenderOpen();
            drawing.PushTransform(rotate);
            if (!isAnima)
            {
                drawing.DrawImage(baseSource, new Rect(location, size));
            }
            else
            {
                //for (int i = 0; i < frameIndex; i++)
                //{
                //    var frame = uriBitmap.Frames[i];
                //    var info = frameInfos[i];
                //    drawing.DrawImage(frame, new Rect(new Point(location.X + info.Left * zoom, location.Y + info.Top * zoom), new Size(info.Width * zoom, info.Height * zoom)));
                //}
            }
            drawing.Close();
        }
        public void ChangeGifFrame()
        {
            while (true)
            {
                if (isAnima)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        frameIndex++;
                        if (frameIndex >= uriBitmap.Frames.Count)
                        {
                            frameIndex = 0;
                        }
                        DrawImage(drawPoint, drawSize);
                    }));

                }
                Thread.Sleep(30);
            }
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
            //if (canMove)
            //{
            //    Point movePoint = e.GetPosition(this);
            //    Point p = RotatePoint(new Point(0, 0), new Point((movePoint.X - downPoint.X), (movePoint.Y - downPoint.Y)), rotate.Angle * -1);
            //    drawPoint = new Point(downPoint_r.X + p.X, downPoint_r.Y + p.Y);
            //    DrawImage(drawPoint, drawSize);
            //}
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
        public BitmapSource MixBitmapSource(BitmapSource bs1, BitmapSource bs2)
        {
            DrawingVisual dv = new DrawingVisual();
            RenderTargetBitmap render = new RenderTargetBitmap(bs1.PixelWidth, bs2.PixelHeight, bs1.DpiX, bs2.DpiY, PixelFormats.Default);
            DrawingContext dc = dv.RenderOpen();
            dc.DrawImage(bs1, new Rect(0, 0, bs1.PixelWidth, bs1.PixelHeight));
            dc.DrawImage(bs2, new Rect(0, 0, bs1.PixelWidth, bs1.PixelHeight));
            dc.Close();
            render.Render(dv);
            return render;
        }
        private static FrameInfo GetFrameInfo(BitmapFrame frame)
        {

            var info = new FrameInfo
            {
                Delay = 100,
                DisposalMethod = FrameDisposalMethod.Replace,
                Width = frame.PixelWidth,
                Height = frame.PixelHeight,
                Left = 0,
                Top = 0
            };
            const string delayQuery = "/grctlext/Delay";
            const string disposalQuery = "/grctlext/Disposal";
            const string widthQuery = "/imgdesc/Width";
            const string heightQuery = "/imgdesc/Height";
            const string leftQuery = "/imgdesc/Left";
            const string topQuery = "/imgdesc/Top";
            try
            {
                BitmapMetadata metadata = frame.Metadata as BitmapMetadata;
                var delay = metadata.GetQuery(delayQuery);
                var dis = metadata.GetQuery(disposalQuery);
                var width = metadata.GetQuery(widthQuery);
                var height = metadata.GetQuery(heightQuery);
                var left = metadata.GetQuery(leftQuery);
                var top = metadata.GetQuery(topQuery);
                if (delay != null)
                {
                    info.Delay = Convert.ToInt32(delay);
                }
                if (dis != null)
                {

                    info.DisposalMethod = (FrameDisposalMethod)Convert.ToInt32(dis);
                }
                if (width != null)
                {
                    info.Width = Convert.ToDouble(width);
                }
                if (height != null)
                {
                    info.Height = Convert.ToDouble(height);
                }
                if (top != null)
                {
                    info.Top = Convert.ToDouble(top);
                }
                if (left != null)
                {
                    info.Left = Convert.ToDouble(left);
                }
            }
            catch (Exception)
            {

                return null;
            }
            return info;
        }
        private BitmapSource MakeFrame(
         BitmapSource fullImage,
         BitmapSource rawFrame, FrameInfo frameInfo,
         BitmapSource previousFrame, FrameInfo previousFrameInfo)
        {
            DrawingVisual visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                if (previousFrameInfo != null && previousFrame != null &&
                    previousFrameInfo.DisposalMethod == FrameDisposalMethod.Combine)
                {
                    var fullRect = new Rect(0, 0, fullImage.PixelWidth, fullImage.PixelHeight);
                    context.DrawImage(previousFrame, fullRect);
                }

                context.DrawImage(rawFrame, frameInfo.Rect);
            }
            var bitmap = new RenderTargetBitmap(
                fullImage.PixelWidth, fullImage.PixelHeight,
                fullImage.DpiX, fullImage.DpiY,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);
            return bitmap;
        }

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

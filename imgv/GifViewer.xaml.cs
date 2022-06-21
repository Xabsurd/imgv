using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace imgv
{
    /// <summary>
    /// GifViewer.xaml 的交互逻辑
    /// </summary>
    public partial class GifViewer : UserControl
    {
        private Bitmap gifBitmap;// gif动画的System.Drawing.Bitmap
        private BitmapSource bitmapSource;// 用于显示每一帧的BitmapSource
        public GifViewer()
        {
            InitializeComponent();
        }
        public void GetGifImage(string path)
        {
            try
            {
                StopAnimate();
            }
            catch (Exception)
            {

                throw;
            }
            
            this.gifBitmap = new Bitmap(path);
            this.bitmapSource = this.GetBitmapSource();
            this.imgGifShow.Source = this.bitmapSource;
            StartAnimate();
        }

        /// <summary>
        /// 从System.Drawing.Bitmap中获得用于显示的那一帧图像的BitmapSource
        /// </summary>
        /// <returns></returns>
        private BitmapSource GetBitmapSource()
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = this.gifBitmap.GetHbitmap();
                this.bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    DeleteObject(handle);
                }
            }
            return this.bitmapSource;
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
        public void clear()
        {
            try
            {
                StopAnimate();
                if (gifBitmap!=null)
                {
                    this.gifBitmap.Dispose();
                    this.gifBitmap = null;
                }
                if (this.bitmapSource!=null)
                {
                    bitmapSource = null;
                }
                GC.Collect();
            }
            catch (Exception)
            {
            }
           
        }
        /// <summary>
        /// 帧处理
        /// </summary>
        private void OnFrameChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                ImageAnimator.UpdateFrames(); // 更新到下一帧
                if (this.bitmapSource != null)
                {
                    this.bitmapSource.Freeze();
                }

                this.bitmapSource = this.GetBitmapSource();
                this.imgGifShow.Source = this.bitmapSource;
                this.InvalidateVisual();
            }));
        }

        /// <summary>
        /// 删除本地 bitmap resource
        /// </summary>
        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject(IntPtr hObject);
    }
}

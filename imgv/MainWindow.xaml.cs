using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ImageViewer imgv;
        public MainWindow()
        {
            InitializeComponent();
            imgv = new ImageViewer();
            this.contentControl.Content = imgv;
            this.KeyDown += MainWindow_KeyDown;
            this.AllowDrop = true;
            this.Drop += MainWindow_Drop;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            Array a = (System.Array)e.Data.GetData(DataFormats.FileDrop);
            imgv.InitImage(a.GetValue(0).ToString());
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine(e.Key);
            switch (e.Key)
            {
                case Key.Space:
                    if (imgv.contain)
                    {
                        imgv.ResizeToTile();
                    }
                    else
                    {
                        imgv.ResizeToContain();
                    }
                    break;
            }

        }
    }
}

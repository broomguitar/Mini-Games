using System;
using System.Collections.Generic;
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
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace mini_games
{
    /// <summary>
    /// JigsawPuzzle.xaml 的交互逻辑
    /// </summary>
    public partial class JigsawPuzzle : UserControl
    {
        public JigsawPuzzle()
        {
            InitializeComponent();
            this.Loaded += JigsawPuzzle_Loaded; ;

        }
        private void JigsawPuzzle_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher_timer.Interval = TimeSpan.FromSeconds(15);
            Dispatcher_timer.Tick += Dispatcher_timer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer.Tick += DispatcherTimer_Tick; ;
        }
        private long seconds=0;
        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            seconds++;
            lb_m.Content = seconds / 60;
            lb_s.Content = seconds % 60;
        }

        private BitmapSource _starBitmapSource;
        private void btn_selectImg_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Filter = "图片文件(*.jpg;*.bmp;*.jpeg;*.png;*.jfif)|*.jpg;*.bmp;*.jpeg;*.png;*.jfif|所有文件(*.*)|*.*";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == true)
            {
                img_src.Source = _starBitmapSource = new BitmapImage(new Uri(ofd.FileName));
            }
        }
        private List<Border> ls_orig = new List<Border>();
        private List<Border> ls_Moving = new List<Border>();
        private int num = 4;
        private void btn_ShuffleImg_Click(object sender, RoutedEventArgs e)
        {
            if (_starBitmapSource == null)
            {
                return;
            }
            ok.Visibility = Visibility.Collapsed;
            ResetTime();
            ResetSelect();
            ls_orig.Clear();
            grid.Children.Clear();
            grid.ColumnDefinitions.Clear();
            grid.RowDefinitions.Clear();
            if (rbt4.IsChecked == true)
            {
                num = 4;
            }
            if (rbt5.IsChecked == true)
            {
                num = 5;
            }
            else if (rbt7.IsChecked == true)
            {
                num = 7;
            }
            else if (rbt10.IsChecked == true)
            {
                num = 10;
            }
            int width = _starBitmapSource.PixelWidth / num;
            int height = _starBitmapSource.PixelHeight / num;
            ColumnDefinitionCollection columnDefinitions = grid.ColumnDefinitions;
            for (int i = 0; i < num; i++)
            {
                columnDefinitions.Add(new ColumnDefinition());
            }
            RowDefinitionCollection rowDefinitions = grid.RowDefinitions;
            for (int i = 0; i < num; i++)
            {
                rowDefinitions.Add(new RowDefinition());
            }
            for (int i = 0; i < num; i++)
            {
                for (int j = 0; j < num; j++)
                {
                    var data = CutImage(_starBitmapSource, new Int32Rect { X = i * width, Y = j * height, Width = width, Height = height });
                    SaveImage(data, $"partImags/{i}-{j}.jpg");
                    var border = new Border { Background = new ImageBrush(data), Margin = new Thickness(3)};
                    border.MouseLeftButtonDown -= Border_MouseLeftButtonDown;
                    border.MouseLeftButtonDown += Border_MouseLeftButtonDown;
                    ls_orig.Add(border);
                }
            }
            ls_Moving = ls_orig.OrderBy(a => Guid.NewGuid().ToString()).ToList();
            for (int i = 0; i < ls_Moving.Count; i++)
            {
                Grid.SetColumn(ls_Moving[i], i / num);
                Grid.SetRow(ls_Moving[i], i % num);
                grid.Children.Add(ls_Moving[i]);
            }
            dispatcherTimer.Start();
        }
        int clickCount = 0;
        private Border preBorder;
        private DispatcherTimer Dispatcher_timer = new DispatcherTimer(),dispatcherTimer=new DispatcherTimer();
        private Border tipBorder = null;
        private void Dispatcher_timer_Tick(object? sender, EventArgs e)
        {
            if (preBorder != null)
            {
                if (clickCount == 1)
                {
                    Dispatcher_timer.Stop();
                    int index = ls_orig.IndexOf(preBorder);
                    if (index >= 0)
                    {
                        tipBorder = ls_Moving[index] as Border;
                        tipBorder.BorderBrush = Brushes.LawnGreen;
                        tipBorder.BorderThickness = new Thickness(4);
                        //ThicknessAnimation dda = new ThicknessAnimation(new Thickness(0), new Thickness(4), new Duration(TimeSpan.FromSeconds(1)));
                        ////实例化动画处理对象
                        //dda.AutoReverse = true;//返回
                        //dda.RepeatBehavior = RepeatBehavior.Forever;//重复这个行为
                        //tipBorder.BeginAnimation(Border.MarginProperty, dda);//启动动画
                    }
                }
            }
        }
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                if (border == preBorder)
                {
                    return;
                }
                clickCount++;
                if (clickCount == 2)
                {
                    int preindex = ls_Moving.IndexOf(preBorder);
                    int index = ls_Moving.IndexOf(border);
                    ls_Moving[preindex] = border;
                    ls_Moving[index] = preBorder;
                    Grid.SetColumn(border, preindex / num);
                    Grid.SetRow(border, preindex % num);
                    Grid.SetColumn(preBorder, index / num);
                    Grid.SetRow(preBorder, index % num);
                    bool b = true;
                    for (int i = 0; i < ls_orig.Count; i++)
                    {
                        if (!ls_Moving[i].Equals(ls_orig[i]))
                        {
                            b = false;
                            break;
                        }

                    }
                    if (b)
                    {
                        dispatcherTimer.Stop();
                        seconds = 0;
                        ok.Visibility = Visibility.Visible;
                    }
                    ResetSelect();
                }
                else
                {
                    border.BorderBrush = Brushes.OrangeRed;
                    border.BorderThickness = new Thickness(4);
                    border.Effect = new DropShadowEffect { ShadowDepth = 5 };
                    preBorder = border;
                    Dispatcher_timer.Start();

                }
            }
        }
        private void btn_restorImg_Click(object sender, RoutedEventArgs e)
        {
            if (ls_orig.Count > 0)
            {
                ls_Moving = new List<Border>(ls_orig);
                ok.Visibility = Visibility.Collapsed;
                ResetTime();
                ResetSelect();
                grid.Children.Clear();
                grid.ColumnDefinitions.Clear();
                grid.RowDefinitions.Clear();
                ColumnDefinitionCollection columnDefinitions = grid.ColumnDefinitions;
                for (int i = 0; i < num; i++)
                {
                    columnDefinitions.Add(new ColumnDefinition());
                }
                RowDefinitionCollection rowDefinitions = grid.RowDefinitions;
                for (int i = 0; i < num; i++)
                {
                    rowDefinitions.Add(new RowDefinition());
                }
                for (int i = 0; i < ls_orig.Count; i++)
                {
                    Grid.SetColumn(ls_orig[i], i / num);
                    Grid.SetRow(ls_orig[i], i % num);
                    grid.Children.Add(ls_orig[i]);
                }
            }
        }
        private void btn_removeMargin_Click(object sender, RoutedEventArgs e)
        {
            if (ls_orig.Count > 0)
            {
                if (btn_removeMargin.IsChecked == true)
                {
                    foreach (var item in ls_orig)
                    {
                        item.Margin = new Thickness(0);
                    }
                }
                else
                {
                    foreach (var item in ls_orig)
                    {
                        item.Margin = new Thickness(3);
                    }
                }
            }
        }

        private void grid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ResetSelect();
        }
        private void ResetTime()
        {
            dispatcherTimer.Stop();
            seconds = 0;
            lb_m.Content = 0;
            lb_s.Content = 0;
        }
        private void ResetSelect()
        {
            this.Dispatcher.Invoke(new Action(() => {
                if (preBorder != null)
                {
                    clickCount = 0;
                    preBorder.BorderBrush = default;
                    preBorder.BorderThickness = default;
                    preBorder.Effect = default;
                    preBorder = null;
                    if (tipBorder != null)
                    {
                        tipBorder.BorderBrush = default;
                        tipBorder.BorderThickness = default;
                    }
                    Dispatcher_timer.Stop();
                }
            }));
        }

        private static BitmapSource CutImage(BitmapSource bitmapSource, Int32Rect cut)
        {
            try
            {
                //计算Stride
                var stride = bitmapSource.Format.BitsPerPixel * cut.Width / 8;
                //声明字节数组
                byte[] data = new byte[cut.Height * stride];
                //调用CopyPixels
                bitmapSource.CopyPixels(cut, data, stride, 0);
                return BitmapSource.Create(cut.Width, cut.Height, 0, 0, bitmapSource.Format, null, data, stride);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static void SaveImage(BitmapSource inputBitmapSource, string filePath, int qualityLevel = 75)
        {
            try
            {
                if (qualityLevel < 1)
                {
                    qualityLevel = 1;
                }
                else if (qualityLevel > 100)
                {
                    qualityLevel = 100;
                }
                BitmapEncoder encoder = null;
                var extName = System.IO.Path.GetExtension(filePath).ToLower();
                switch (extName)
                {
                    case ".png": encoder = new PngBitmapEncoder(); break;
                    case ".jpg": encoder = new JpegBitmapEncoder { QualityLevel = qualityLevel }; break;
                    case ".bmp": encoder = new BmpBitmapEncoder(); break;
                    default:
                        break;
                }
                encoder.Frames.Add(BitmapFrame.Create(inputBitmapSource));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

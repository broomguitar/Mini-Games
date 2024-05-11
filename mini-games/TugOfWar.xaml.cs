using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace mini_games
{
    public delegate void KeyDownHandler(object sender, KeyEventArgs e);
    /// <summary>
    /// TugOfWar.xaml 的交互逻辑
    /// </summary>
    public partial class TugOfWar : UserControl
    {
        public TugOfWar()
        {
            InitializeComponent();
            this.Loaded += TugOfWar_Loaded;
        }
        public KeyDownHandler KeyDownHandler;
        private void canvas_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    offsety-=2;
                    break;
                case Key.Down:
                    offsety+=2;
                    break;
                case Key.Left: 
                    offsetx-=2;
                    break;
                case Key.Right:
                    offsetx+=2;
                    break;
                case Key.W:
                    offsety -= 2;
                    break;
                case Key.S:
                    offsety += 2;
                    break;
                case Key.A:
                    offsetx -= 2;
                    break;
                case Key.D:
                    offsetx += 2;
                    break;
                default: break;
            }
        }

        private DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Render);
        List<string> ls_targets = new List<string>();
        List<Brush> brushes = new List<Brush> { Brushes.DarkViolet, Brushes.DarkCyan, Brushes.Cyan, Brushes.AliceBlue };
        private void TugOfWar_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 30; i++)
            {
                ls_targets.Add(i.ToString());
            }
            targets.Rows = 10;
            targets.Children.Clear();
            for (int i = 0; i < 100; i++)
            {
                targets.Children.Add(new Border { Width = 75, Height = 75, CornerRadius = new CornerRadius(37), Background = brushes[random.Next(0, 4)], Child = new Label { Content = ls_targets[i % 30], HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
            }
            SocketServer.Open(System.Net.IPAddress.Any, 9999);
            SocketServer.HasNewClientEvent -= SocketServer_HasNewClientEvent;
            SocketServer.HasNewClientEvent += SocketServer_HasNewClientEvent;
            SocketServer.ClientOfflineEvent -= SocketServer_ClientOfflineEvent;
            SocketServer.ClientOfflineEvent += SocketServer_ClientOfflineEvent; ;
            SocketServer.HasNewMsgEvent -= SocketServer_HasNewMsgEvent;
            SocketServer.HasNewMsgEvent += SocketServer_HasNewMsgEvent;
            Canvas.SetLeft(ball,random.Next((int)canvas.ActualWidth));
            Canvas.SetTop(ball, random.Next((int)canvas.ActualHeight));
            _moveChildren.Add(ball);
            dispatcherTimer.Interval =TimeSpan.FromMilliseconds(1000 / 60d);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Start();
            KeyDownHandler = new KeyDownHandler(canvas_KeyDown);
        }

        private void SocketServer_ClientOfflineEvent(object? sender, EventArgs e)
        {
            App.Current.Dispatcher.Invoke(new Action(() => { lb_remote.Content = ""; }));
        }

        private void SocketServer_HasNewClientEvent(object? sender, Client e)
        {
            App.Current.Dispatcher.Invoke(new Action(() => { lb_remote.Content = $"对方:{e.IP}:{e.Port}"; }));
        }

        private void SocketServer_HasNewMsgEvent(object? sender, MessageData e)
        {
            //switch(e.Message)
            //{
            //    case "W":
            //        offsety-=2;
            //        break;
            //    case "S":
            //        offsety+=2;
            //        break;
            //    case "A":
            //        offsetx-=2;
            //        break;
            //    case "D":
            //        offsetx+=2;
            //        break;
            //    default: break;
            //}
            App.Current.Dispatcher.Invoke(() =>
            {
                var lablel = new Label { Content = e.Message, Foreground = Brushes.LightCyan, Background = Brushes.Coral };
                Canvas.SetLeft(lablel, random.Next((int)canvas.ActualWidth));
                Canvas.SetTop(lablel, random.Next((int)canvas.ActualHeight));
                canvas.Children.Add(lablel);
                _moveChildren.Add(lablel);
            });
        }

        Random random=new Random(Environment.TickCount);
        private double offsetx=0,offsety=0, vx=1, vy=1;
        private List<FrameworkElement> _moveChildren = new List<FrameworkElement>();
        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            ObjectMove();
            dispatcherTimer.Start();
        }
        Random r = new Random(new Guid().GetHashCode());
        private void ObjectMove()
        {
            foreach (var item in _moveChildren)
            {
                double left = Canvas.GetLeft(item);
                double top = Canvas.GetTop(item);
                var dd = r.Next(50);
                vx = dd*(vx/Math.Abs(vx)) + offsetx;
                vy = dd *(vy / Math.Abs(vy))+ offsety;
                if (left >= canvas.ActualWidth - item.Width || left <= 0)
                {
                    vx = -vx;
                    offsetx=0;
                }
                if (top >= canvas.ActualHeight - item.Height || top <= 0)
                {
                    vy = -vy;
                    offsety=0;
                }
                Canvas.SetLeft(item, left+vx);
                Canvas.SetTop(item, top+vy);
            }
        }
    }
}

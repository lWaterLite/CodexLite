using CodexLite.Apps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CodexLite
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Navi.Content = new Home();
        }

        private void TodoButton_Click(object sender, RoutedEventArgs e)
        {
            if (Navi.Content.GetType() != typeof(Todo))
            {
                Navi.Content = new Todo();
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Navi.Content.GetType() != typeof(Home))
            {
                Navi.Content = new Home();
            }
        }

        private void FormatButton_Click(object sender, RoutedEventArgs e)
        {
            if (Navi.Content.GetType() != typeof(Format))
            {
                Navi.Content = new Format();
            }
        }

        private void TopBarCloseButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void TopBarDrag_LeftDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void TopBarMinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void TopBarMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }
}

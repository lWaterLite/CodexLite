using CodexLite.Apps;
using System;
using System.Windows;
using System.Windows.Input;

namespace CodexLite
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private readonly Home _home;
        public MainWindow()
        {
            InitializeComponent();
            _home = new Home();
            Navi.Content = _home;
            HomeButton.IsEnabled = false;
        }

        private void TodoButton_Click(object sender, RoutedEventArgs e)
        {
            Navi.Content = new Todo();
            HomeButton.IsEnabled = true;
            TodoButton.IsEnabled = false;
            FormatButton.IsEnabled = true;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Navi.Content = _home;
            HomeButton.IsEnabled = false;
            TodoButton.IsEnabled = true;
            FormatButton.IsEnabled = true;
        }

        private void FormatButton_Click(object sender, RoutedEventArgs e)
        {
            Navi.Content = new Format();
            HomeButton.IsEnabled = true;
            TodoButton.IsEnabled = true;
            FormatButton.IsEnabled = false;
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

        private void GithubBrowser_OnClick(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/lWaterLite");
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

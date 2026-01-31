// App.xaml.cs
using System.IO;
using System.Windows;

namespace ImageViewer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 创建主窗口
            MainWindow mainWindow = new MainWindow();
            
            // 如果有命令行参数，尝试加载图片
            if (e.Args.Length > 0)
            {
                string filePath = e.Args[0];
                if (File.Exists(filePath))
                {
                    // 在主窗口显示后加载图片
                    mainWindow.Loaded += (sender, args) => mainWindow.LoadImageFromPath(filePath);
                }
            }
            
            // 显示主窗口
            mainWindow.Show();
        }
    }
}
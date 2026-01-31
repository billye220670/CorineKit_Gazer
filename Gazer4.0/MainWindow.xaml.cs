using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ImageViewer
{
   
    public partial class MainWindow : Window
    {
        // 用于窗口标题绑定的属性
        public string WindowTitle
        {
            get 
            { 
                return settings?.CurrentLanguage != null ? 
                    LanguageManager.GetText("WindowTitle", settings.CurrentLanguage) : "凝視者4"; 
            }
        }

        private bool isAutoPlaying = false;
        // 自动播放相关字段
        private DispatcherTimer autoPlayTimer;
        private Random autoPlayRandom = new Random();
        
        private Point lastMousePosition;
        private bool isPanning = false;
        private double scaleX = 1.0;
        private double scaleY = 1.0;
        private double translateX = 0;
        private double translateY = 0;
        private HashSet<string> newlyAddedImages = new HashSet<string>();
        // 播放列表相关字段
        public List<string> playList = new List<string>();
        public int playListIndex = 0;
        public bool isPlayListMode = false; // 标记是否在播放列表模式
        
        // 原始平移位置（未加抖动效果的）
        private double baseTranslateX = 0;
        private double baseTranslateY = 0;
        
        private string currentImagePath;
        private List<string> imageFiles = new List<string>();
        private int currentIndex = 0;
        private string[] supportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };
        
        // 应用设置
        public AppSettings settings;
        
        // 相机抖动设置
        private double shakeTime = 0;
        
        // 脉冲抖动设置
        private double timeSinceLastPulse = 0;
        private double nextPulseTime = 0;
        private double currentPulseX = 0;
        private double currentPulseY = 0;
        private double targetPulseX = 0;
        private double targetPulseY = 0;
        private Random random = new Random();
        
        // 通知提示
        private TextBlock notificationText;
        private DispatcherTimer notificationTimer;
        
        // 控制事件处理
        private bool eventsEnabled = true;
        
        // 颜色相关
        private Color currentBackgroundColor;

        // 播放列表图片状态记录
        public Dictionary<string, ImageState> playListImageStates = new Dictionary<string, ImageState>();
        
        // 将ImageState���义为嵌套类
    public class ImageState
    {
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public double TranslateX { get; set; }
        public double TranslateY { get; set; }
        public double BaseTranslateX { get; set; }
        public double BaseTranslateY { get; set; }
        
        // 抖动相关状态
        public bool EnableShake { get; set; }
        public double ShakeAmount { get; set; }
        public double ShakeFrequency { get; set; }
        
        public bool EnablePulse { get; set; }
        public double PulseInterval { get; set; }
        public double PulseRandomness { get; set; }
        public double PulseDamping { get; set; }
        public double PulsePowerX { get; set; }
        public double PulsePowerY { get; set; }
        
        // 抖动运行时状态
        public double ShakeTime { get; set; }
        public double TimeSinceLastPulse { get; set; }
        public double NextPulseTime { get; set; }
        public double CurrentPulseX { get; set; }
        public double CurrentPulseY { get; set; }
        public double TargetPulseX { get; set; }
        public double TargetPulseY { get; set; }
        
        // 从当前状态创建
        public static ImageState FromCurrentState(MainWindow window)
        {
            // 添加调试输出，验证保存的值
            System.Diagnostics.Debug.WriteLine($"[FromCurrentState] 创建状态快照:");
            System.Diagnostics.Debug.WriteLine($"  Scale: {window.scaleX}, {window.scaleY}");
            System.Diagnostics.Debug.WriteLine($"  BaseTranslate: {window.baseTranslateX}, {window.baseTranslateY}");
            System.Diagnostics.Debug.WriteLine($"  Image Size: {window.imgDisplay.Width} x {window.imgDisplay.Height}");
            return new ImageState
            {
                
                ScaleX = window.scaleX,
                ScaleY = window.scaleY,
                // 重要：保存基础位置，而不是包含抖动的位置
                TranslateX = window.baseTranslateX,
                TranslateY = window.baseTranslateY,
                BaseTranslateX = window.baseTranslateX,
                BaseTranslateY = window.baseTranslateY,
            
                // 抖动设置
                EnableShake = window.settings.EnableShake,
                ShakeAmount = window.settings.ShakeAmount,
                ShakeFrequency = window.settings.ShakeFrequency,
            
                EnablePulse = window.settings.EnablePulse,
                PulseInterval = window.settings.PulseInterval,
                PulseRandomness = window.settings.PulseRandomness,
                PulseDamping = window.settings.PulseDamping,
                PulsePowerX = window.settings.PulsePowerX,
                PulsePowerY = window.settings.PulsePowerY,
            
                // 抖动运行时状态
                ShakeTime = window.shakeTime,
                TimeSinceLastPulse = window.timeSinceLastPulse,
                NextPulseTime = window.nextPulseTime,
                CurrentPulseX = window.currentPulseX,
                CurrentPulseY = window.currentPulseY,
                TargetPulseX = window.targetPulseX,
                TargetPulseY = window.targetPulseY
            };
        }
        
        // 应用状态到窗口
        public void ApplyToWindow(MainWindow window)
        {
            // 应用画布状态
            window.scaleX = ScaleX;
            window.scaleY = ScaleY;
            // 重要：恢复基础位置
            window.baseTranslateX = BaseTranslateX;
            window.baseTranslateY = BaseTranslateY;
            // translateX/Y将在渲染循环中根据baseTranslateX/Y计算
            window.translateX = BaseTranslateX;
            window.translateY = BaseTranslateY;
        
            // 暂停UI事件
            window.SuspendEvents();
        
            // 应用抖动设置
            window.settings.EnableShake = EnableShake;
            window.settings.ShakeAmount = ShakeAmount;
            window.settings.ShakeFrequency = ShakeFrequency;
        
            window.settings.EnablePulse = EnablePulse;
            window.settings.PulseInterval = PulseInterval;
            window.settings.PulseRandomness = PulseRandomness;
            window.settings.PulseDamping = PulseDamping;
            window.settings.PulsePowerX = PulsePowerX;
            window.settings.PulsePowerY = PulsePowerY;
        
            // 更新UI控件
            window.ApplySettingsToUI();
        
            // 恢复UI事件
            window.ResumeEvents();
        
            // 应用抖动运行时状态
            window.shakeTime = ShakeTime;
            window.timeSinceLastPulse = TimeSinceLastPulse;
            window.nextPulseTime = NextPulseTime;
            window.currentPulseX = CurrentPulseX;
            window.currentPulseY = CurrentPulseY;
            window.targetPulseX = TargetPulseX;
            window.targetPulseY = TargetPulseY;
        
            // 应用画布变换
            window.ApplyTransform();
        }
    }
        public MainWindow()
        {
            // 首先初始化组件
            InitializeComponent();
        
            // 初始化预设按钮
            InitializePresetButtons();
            // 然后初始化设置对象
            settings = AppSettings.Load();
            // 初始化自动播放定时器
            autoPlayTimer = new DispatcherTimer();
            autoPlayTimer.Tick += AutoPlayTimer_Tick;
            
            // 接着创建通知控件
            InitializeNotification();
            // 添加新的事件绑定
            chkShowNotifications.Checked += ChkShowNotifications_Changed;
            chkShowNotifications.Unchecked += ChkShowNotifications_Changed;
            // 禁用控件事件
            SuspendEvents();
            
            // 从设置中加载值到UI控件
            ApplySettingsToUI();
            
            // 应用语言设置
            ApplyLanguage();
            
            // 重新启用控件事件
            ResumeEvents();
            
            // 使用CompositionTarget.Rendering事件以获取最高帧率
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            
            // 初始化颜色设置
            currentBackgroundColor = settings.GetBackgroundColor();
            UpdateColorPreview();
            
            // 监听窗口大小变化
            SizeChanged += (s, e) => {
                if (imgDisplay.Source != null && !settings.AutoSizeWindow)
                {
                    FitImageToCanvas((BitmapImage)imgDisplay.Source);
                }
            };
            canvas.LostMouseCapture += Canvas_LostMouseCapture;
            
            // 初始化下一次脉冲时间
            CalculateNextPulseTime();
            
            // 窗口关闭时保存设置
            Closing += (s, e) => {
                autoPlayTimer?.Stop();
                settings.Save();
            };
            // 在构造函数末尾添加
            this.Loaded += Window_Loaded;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 检查文件关联状态
            if (chkRegisterFileType != null)
            {
                chkRegisterFileType.IsChecked = PlaylistManager.IsFileAssociationRegistered();
            }

            // 处理命令行参数
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string filePath = args[1];
        
                // 使用 Dispatcher 确保在下一个渲染周期执行
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (Path.GetExtension(filePath).ToLower() == ".gzpl")
                    {
                        // 加载播放列表文件
                        PlaylistManager.LoadPlaylist(filePath, this);
                    }
                    else if (File.Exists(filePath) && IsImageFile(filePath))
                    {
                        // 加载普通图片文件
                        LoadFromCommandLine(filePath);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
    
            // 更新播放列表信息
            UpdatePlaylistInfo();
        }
        // 添加辅助方法检查是否是图片文件
        private bool IsImageFile(string filePath)
        {
            return supportedExtensions.Contains(Path.GetExtension(filePath).ToLower());
        }
        private void ChkShowNotifications_Changed(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled) return;
    
            settings.ShowNotifications = chkShowNotifications.IsChecked == true;
            //SaveSettings();
        }
        // 更新播放列表控件的启用状态
        private void UpdatePlayListControlsState()
        {
            bool isEnabled = isPlayListMode;
    
            chkAutoPlay.IsEnabled = isEnabled;
            sliderAutoPlayInterval.IsEnabled = isEnabled && settings.AutoPlayEnabled;
            sliderAutoPlayRandomness.IsEnabled = isEnabled && settings.AutoPlayEnabled;
            chkRandomPlayback.IsEnabled = isEnabled;
    
            // 如果不在播放列表模式，停止自动播放
            if (!isEnabled && autoPlayTimer.IsEnabled)
            {
                StopAutoPlay();
            }
        }
        
        
        private void InitializePresetButtons()
        {
            UpdatePresetButtonStyles();
        }
        // 公开方法，用于从外部加载图片
        public void LoadImageFromPath(string filePath)
        {
            LoadImage(filePath);
        }
        private void SuspendEvents()
        {
            eventsEnabled = false;
        }
        
        private void ResumeEvents()
        {
            eventsEnabled = true;
        }
        
        private void InitializeNotification()
        {
            // 创建通知文本框
            notificationText = new TextBlock();
            notificationText.FontSize = 18;
            notificationText.Foreground = new SolidColorBrush(Colors.White);
            notificationText.Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0));
            notificationText.Padding = new Thickness(15);
            notificationText.HorizontalAlignment = HorizontalAlignment.Center;
            notificationText.VerticalAlignment = VerticalAlignment.Bottom;
            notificationText.Margin = new Thickness(0, 0, 0, 50);
            notificationText.Visibility = Visibility.Collapsed;
            
            // 添加到窗口
            Grid mainGrid = (Grid)Content;
            mainGrid.Children.Add(notificationText);
            
            // 创建通知计时器
            notificationTimer = new DispatcherTimer();
            notificationTimer.Interval = TimeSpan.FromSeconds(1.5);
            notificationTimer.Tick += (s, e) => {
                notificationTimer.Stop();
                HideNotification();
            };
        }
        private void BtnPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int index))
            {
                SavePreset(index);
            }
        }
        // 保存预设
        private void SavePreset(int index)
        {
            if (settings == null) return;
    
            // 保存当前设置到预设
            PresetManager.SavePreset(settings, index);
    
            // 更新预设按钮样式
            UpdatePresetButtonStyles();
    
            // 显示通知
            ShowNotification($"已保存预设 {index + 1}");
        }
        // 应用预设
        private void ApplyPreset(int index)
        {
            if (settings == null) return;
    
            // 检查预设是否存在
            if (!PresetManager.PresetExists(index))
            {
                ShowNotification($"预设 {index + 1} 不存在");
                return;
            }
    
            // 暂停UI事件
            SuspendEvents();
    
            // 应用预设到设置
            if (PresetManager.ApplyPreset(settings, index))
            {
                // 更新UI控件
                ApplySettingsToUI();
        
                // 显示通知
                ShowNotification($"已应用预设 {index + 1}");
            }
    
            // 恢复UI事件
            ResumeEvents();
        }
        // 应用预设按钮点击事件
        private void BtnApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            // 此方法暂时未使用，可以在未来扩展功能时使用
        }
        // 更新预设按钮样式
        private void UpdatePresetButtonStyles()
        {
            // 更新每个预设按钮的样式，显示哪些预设已保存
            UpdatePresetButtonStyle(btnPreset1, 0);
            UpdatePresetButtonStyle(btnPreset2, 1);
            UpdatePresetButtonStyle(btnPreset3, 2);
            UpdatePresetButtonStyle(btnPreset4, 3);
        }
        // 更新单个预设按钮样式
        private void UpdatePresetButtonStyle(Button button, int index)
        {
            if (button == null) return;
    
            if (PresetManager.PresetExists(index))
            {
                // 预设存在，使用不同的样式
                button.Background = new SolidColorBrush(Colors.DarkGreen);
                button.Foreground = new SolidColorBrush(Colors.White);
        
                // 添加预设名称作为工具提示
                var preset = PresetManager.GetPreset(index);
                if (preset != null)
                {
                    button.ToolTip = preset.Name;
                }
            }
            else
            {
                // 预设不存在，使用默认样式
                button.ClearValue(Button.BackgroundProperty);
                button.ClearValue(Button.ForegroundProperty);
                button.ToolTip = "点击保存当前设置为预设";
            }
        }

        public void ShowNotification(string message)
        {
            if (!settings.ShowNotifications) return;
            //if (isAutoPlaying) return;
            notificationText.Text = message;
            
            if (notificationText.Visibility != Visibility.Visible)
            {
                notificationText.Opacity = 0;
                notificationText.Visibility = Visibility.Visible;
                
                // 创建淡入动画
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                notificationText.BeginAnimation(OpacityProperty, fadeIn);
            }
            
            // 重置计时器
            notificationTimer.Stop();
            notificationTimer.Start();
        }
        
        private void HideNotification()
        {
            // 创建淡��动画
            DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => notificationText.Visibility = Visibility.Collapsed;
            notificationText.BeginAnimation(OpacityProperty, fadeOut);
        }
        private void ChkAutoPlay_Changed(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled || settings == null) return;
    
            settings.AutoPlayEnabled = chkAutoPlay.IsChecked == true;
    
            if (settings.AutoPlayEnabled && isPlayListMode)
            {
                // 只有在播放列表模式下才启动
                if (isPlayListMode && playList.Count > 1)
                {
                    StartAutoPlay();
                    //ShowNotification("自动播放已开始");
                }
            }
            else
            {
                StopAutoPlay();
            }
    
            UpdatePlayListControlsState();
        }

        private void SliderAutoPlayInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!eventsEnabled || settings == null) return;
    
            settings.AutoPlayInterval = sliderAutoPlayInterval.Value;
            lblAutoPlayInterval.Text = $"{settings.AutoPlayInterval:F1}s";
    
            // 如果正在自动播放，更新定时器间隔
            if (autoPlayTimer.IsEnabled)
            {
                UpdateAutoPlayInterval();
            }
        }

        private void SliderAutoPlayRandomness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!eventsEnabled || settings == null) return;
    
            settings.AutoPlayRandomness = sliderAutoPlayRandomness.Value;
            lblAutoPlayRandomness.Text = $"{settings.AutoPlayRandomness:F0}%";
        }
        private void StartAutoPlay()
        {
            //if (!isPlayListMode || playList.Count <= 1) return;
    
            UpdateAutoPlayInterval();
            autoPlayTimer.Start();
            ShowNotification("自动播放已开始");
        }

        private void StopAutoPlay()
        {
            autoPlayTimer.Stop();
            ShowNotification("自动播放已停止");
        }

        private void UpdateAutoPlayInterval()
        {
            double baseInterval = settings.AutoPlayInterval;
            double actualInterval = baseInterval;
    
            if (settings.AutoPlayRandomness > 0)
            {
                // 计算随机偏移 (-randomness% 到 +randomness%)
                double maxOffset = settings.AutoPlayRandomness / 100.0;
                double randomOffset = (autoPlayRandom.NextDouble() * 2 - 1) * maxOffset;
                actualInterval = baseInterval * (1.0 + randomOffset);
        
                // 确保间隔不会太短
                if (actualInterval < 0.1) actualInterval = 0.1;
            }
    
            autoPlayTimer.Interval = TimeSpan.FromSeconds(actualInterval);
    
            // 可选：显示调试信息
            // System.Diagnostics.Debug.WriteLine($"下次播放间隔: {actualInterval:F2}秒 (基础: {baseInterval:F1}秒)");
        }

        private void OnMouseClickImage(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (mouseButtonEventArgs.ChangedButton == MouseButton.Middle)
            {
                String[] myPaths = new string[1];
                myPaths[0] = currentImagePath;
                DropHelper(myPaths);
            }
        }
        
        
        private void AutoPlayTimer_Tick(object sender, EventArgs e)
        {
            // 只有在播放列表模式且有多张图片时才执行
            if (!isPlayListMode || playList.Count <= 1) 
                return;
    
            // 设置自动播放标志
            isAutoPlaying = true;
    
            if (settings.RandomPlayback)
            {
                // 随机播放
                int nextIndex;
                do
                {
                    nextIndex = autoPlayRandom.Next(playList.Count);
                } while (nextIndex == playListIndex && playList.Count > 1);
        
                playListIndex = nextIndex;
            }
            else
            {
                // 顺序播放
                playListIndex = (playListIndex + 1) % playList.Count;
            }
    
            // 加载新图片
            LoadFromPlayList();
    
            // 重置标志
            isAutoPlaying = false;
    
            // 重要：在每次触发后都更新下一次播放的间隔
            UpdateAutoPlayInterval();
        }
        private void ChkRandomPlayback_Changed(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled || settings == null) return;
    
            settings.RandomPlayback = chkRandomPlayback.IsChecked == true;
        }
        private void ApplySettingsToUI()
        {
            eventsEnabled = false;
            // 应用设置到UI控件
            sliderRed.Value = settings.BackgroundColorR;
            sliderGreen.Value = settings.BackgroundColorG;
            sliderBlue.Value = settings.BackgroundColorB;
            // 恢复提示设置
            chkShowNotifications.IsChecked = settings.ShowNotifications;
            chkAutoSizeWindow.IsChecked = settings.AutoSizeWindow;
            chkAutoBorderless.IsChecked = settings.AutoBorderless;
            chkEnableShake.IsChecked = settings.EnableShake;
            sliderShakeAmount.Value = settings.ShakeAmount;
            sliderShakeFrequency.Value = settings.ShakeFrequency;
            
            chkEnablePulse.IsChecked = settings.EnablePulse;
            sliderPulseInterval.Value = settings.PulseInterval;
            sliderPulseRandomness.Value = settings.PulseRandomness;
            sliderPulseDamping.Value = settings.PulseDamping;
            sliderPulsePowerX.Value = settings.PulsePowerX;
            sliderPulsePowerY.Value = settings.PulsePowerY;
            // 播放列表设置
            chkAutoPlay.IsChecked = settings.AutoPlayEnabled;
            sliderAutoPlayInterval.Value = settings.AutoPlayInterval;
            sliderAutoPlayRandomness.Value = settings.AutoPlayRandomness;
            chkRandomPlayback.IsChecked = settings.RandomPlayback;
    
            // 更新标签
            lblAutoPlayInterval.Text = $"{settings.AutoPlayInterval:F1}s";
            lblAutoPlayRandomness.Text = $"{settings.AutoPlayRandomness:F0}%";
            // 恢复播放列表设置
            if (chkUseAbsolutePaths != null)
            {
                chkUseAbsolutePaths.IsChecked = settings.UseAbsolutePaths;
            }
    
            if (chkRegisterFileType != null)
            {
                chkRegisterFileType.IsChecked = PlaylistManager.IsFileAssociationRegistered();
            }

            eventsEnabled = true;
            // 更新控件状态
            UpdatePlayListControlsState();
        }
        
        private void CalculateNextPulseTime()
        {
            if (settings == null) return;
            
            double randomFactor = 1.0 + (random.NextDouble() * 2 - 1) * (settings.PulseRandomness / 100.0);
            nextPulseTime = settings.PulseInterval * randomFactor;
            timeSinceLastPulse = 0;
        }
        
        private void TriggerPulse()
        {
            if (settings == null) return;
            
            // 确定脉冲初始强度 (随机方向)
            double directionX = random.NextDouble() > 0.5 ? 1 : -1;
            double directionY = random.NextDouble() > 0.5 ? 1 : -1;
            
            // 50%概率使X和Y方向相同
            if (random.NextDouble() < 0.5)
                directionY = directionX;
                
            // 设置目标脉冲强度
            double randomFactorX = 0.7 + random.NextDouble() * 0.6; // 70%-130%
            double randomFactorY = 0.7 + random.NextDouble() * 0.6;
            
            targetPulseX = directionX * settings.PulsePowerX * randomFactorX / 100.0;
            targetPulseY = directionY * settings.PulsePowerY * randomFactorY / 100.0;
            
            // 当前脉冲值从0开始，将会迅速上升到目标值
            currentPulseX = currentPulseY = 0;
            
            // 计算下一次脉冲时间
            CalculateNextPulseTime();
        }
        
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (imgDisplay.Source == null || settings == null) return;
            
            // 获取帧间隔时间 (假设60FPS)
            double deltaTime = 0.016; // 约16ms
            
            // 累积摄影机抖动时间
            shakeTime += deltaTime * settings.ShakeFrequency;
            
            // 处理脉冲效果
            double pulseOffsetX = 0;
            double pulseOffsetY = 0;
            
            if (settings.EnablePulse)
            {
                // 更新脉冲计时
                timeSinceLastPulse += deltaTime;
                
                // 检查是否应该触发新脉冲
                if (timeSinceLastPulse >= nextPulseTime)
                {
                    TriggerPulse();
                }
                
                // 更新脉冲值 - 先快速上升再衰减
                if (Math.Abs(currentPulseX) < Math.Abs(targetPulseX) * 0.95)
                {
                    // 快速上升阶段 (5倍于衰减速度)
                    double riseSpeed = settings.PulseDamping * 5;
                    currentPulseX += (targetPulseX - currentPulseX) * riseSpeed * deltaTime;
                    currentPulseY += (targetPulseY - currentPulseY) * riseSpeed * deltaTime;
                }
                else
                {
                    // 衰减阶段
                    double dampingFactor = Math.Exp(-settings.PulseDamping * deltaTime);
                    currentPulseX *= dampingFactor;
                    currentPulseY *= dampingFactor;
                    
                    // 同时降低目标值，确保不会回弹
                    targetPulseX *= dampingFactor;
                    targetPulseY *= dampingFactor;
                }
                
                // 获取当前脉冲偏移
                pulseOffsetX = currentPulseX;
                pulseOffsetY = currentPulseY;
            }
            
            // 生成平滑的持续抖动效果
            double shakeOffsetX = 0;
            double shakeOffsetY = 0;
            
            if (settings.EnableShake)
            {
                shakeOffsetX = GenerateSmoothShake(shakeTime, 0.5, 1.3) * settings.ShakeAmount;
                shakeOffsetY = GenerateSmoothShake(shakeTime + 100, 0.7, 1.1) * settings.ShakeAmount;
            }
            
            // 组合基础位置、抖动和脉冲效果
            translateX = baseTranslateX + shakeOffsetX + pulseOffsetX;
            translateY = baseTranslateY + shakeOffsetY + pulseOffsetY;
            
            ApplyTransform();
        }
        
        // 生成平滑的摄影机抖动
        private double GenerateSmoothShake(double time, double speed1, double speed2)
        {
            // 使用简单的正弦波叠加，创建自然的摄影机抖动效果
            double value = 0;
            
            // 不同频率的正弦波叠加
            value += Math.Sin(time * speed1) * 0.4;              // 主要摇晃
            value += Math.Sin(time * speed2 * 2.5) * 0.1;         // 较快的小幅度抖动
            value += Math.Sin(time * speed1 * 0.6 + 0.5) * 0.25;  // 较慢的中等抖动
            
            // 归一化到[-0.5, 0.5]范围
            return value * 0.5; 
        }
        
        private void UpdateColorPreview()
        {
            colorPreview.Fill = new SolidColorBrush(currentBackgroundColor);
            canvas.Background = new SolidColorBrush(currentBackgroundColor);
        }
        private void InitializeOrUpdatePlayList(List<string> newImages, bool includeCurrentImage = true)
        {
            // 如果还不在播放列表模式
            if (!isPlayListMode)
            {
                isPlayListMode = true;
                playList.Clear();
        
                // 如果需要包含当前图片
                if (includeCurrentImage && !string.IsNullOrEmpty(currentImagePath))
                {
                    playList.Add(currentImagePath);
                    playListIndex = 0;
            
                    // 如果不自动调整窗口，保存当前图片状态
                    if (!settings.AutoSizeWindow)
                    {
                        string stateKey = $"{currentImagePath}_{playListIndex}";
                        var state = ImageState.FromCurrentState(this);
                        playListImageStates[stateKey] = state;
                    }
                }
            }
    
            // 添加新图片
            foreach (string imagePath in newImages)
            {
                    playList.Add(imagePath);
            }
        }
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    string firstFile = files[0];
                    string extension = Path.GetExtension(firstFile).ToLower();
                    
            
            // 检查是否是播放列表文件
            if (extension == ".gzpl")
            {
                // 只处理第一个gzpl文件
                PlaylistManager.LoadPlaylist(firstFile, this);
                if (files.Length > 1)
                {
                    ShowNotification("只能打开一个播放列表文件，已忽略其他文件");
                }
                return;
            }
            
            // 过滤出图片文件
            
            var imageFiles = files.Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower())).ToList();
            
            if (imageFiles.Count == 0)
                return;

            DropHelper(imageFiles.ToArray());
                }

                //DropHelper();
            }
    
    // 删除这里的所有重复代码！
    // 不要在这里再次处理图片加载
}
        // 添加辅助方法

        private void DropHelper(string[] imageFiles)
        {
            // 如果当前没有加载图片
            if (imgDisplay.Source == null)
            {
                // 直接加载第一个图片
                LoadFromCommandLine(imageFiles[0]);
                
                // 如果拖入了多个图片，创建播放列表
                if (imageFiles.Length > 1)
                {
                    // 创建新的播放列表
                    playList.Clear();
                    playListImageStates.Clear();
                    playList.AddRange(imageFiles);
                    playListIndex = playList.Count-1;
                    isPlayListMode = true;
                    // 检查是否需要启动自动播放
                    if (chkAutoPlay.IsChecked == true)
                    {
                        isAutoPlaying = true;
                        StartAutoPlay();
                        ShowNotification($"已创建播放列表 ({playList.Count} 张图片)，自动播放已启动");
                    }
                    ShowNotification($"已创建播放列表 ({playList.Count} 张图片)");
                }
            }
            else
            {
                // 当前已有图片
                if (!isPlayListMode)
                {
                    // 第一次进入播放列表模式
                    playList.Clear();
                    playList.Add(currentImagePath);
                    playListIndex = 1;
                    isPlayListMode = true;
                    
                    // 保存当前图片状态
                    if (!settings.AutoSizeWindow)
                    {
                        SaveCurrentImageState();
                    }
                    
                    
                }
                else
                {
                    // 已经在播放列表模式
                    if (!settings.AutoSizeWindow)
                    {
                        // 使用 Dispatcher 确保当前渲染完成后再保存
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SaveCurrentImageState();
                            
                            // 然后处理新图片
                            HandleNewImagesInPlayList(imageFiles.ToList());
                        }), System.Windows.Threading.DispatcherPriority.Render);
                        
                        return; // 重要：直接返回，避免执行后面的代码
                    }
                    else
                    {
                        HandleNewImagesInPlayList(imageFiles.ToList());
                        return; // 重要：直接返回
                    }
                }
                
                // 记住第一张新图片
                string firstNewImage = imageFiles[0];
                
                // 添加新图片到播放列表（不清空现有列表）
                foreach (string imagePath in imageFiles)
                {
                    //if (!playList.Contains(imagePath))
                    //{ 禁用，允许用户添加重复图片
                        playList.Add(imagePath);
                        newlyAddedImages.Add(imagePath); // 标记为新添加
                    //}
                }
                
                // 添加完新图片后，检查是否需要启动自动播放
                if (chkAutoPlay.IsChecked == true && playList.Count > 1 && !autoPlayTimer.IsEnabled)
                {
                    isAutoPlaying = true;
                    StartAutoPlay();
                }
                
                // 切换到第一张新图片
                int newIndex = playList.IndexOf(firstNewImage);
                if (newIndex >= 0)
                {
                    playListIndex = newIndex;
                    LoadFromPlayList();
                    ShowNotification($"已打开新图片，播放列表: {playListIndex + 1}/{playList.Count}");
                }
            }
        }
        
        
        
        private void HandleNewImagesInPlayList(List<string> imageFiles)
        {
            // 记住第一张新图片
            string firstNewImage = imageFiles[0];

            // 添加新图片到播放列表
            foreach (string imagePath in imageFiles)
            {
                //if (!playList.Contains(imagePath))
                //{
                    playList.Add(imagePath);
                    newlyAddedImages.Add(imagePath); // 重要：标记为新添加
                //}
            }

            // 切换到第一张新图片
            int newIndex = playList.LastIndexOf(firstNewImage);
            if (newIndex >= 0)
            {
                playListIndex = newIndex;
                LoadFromPlayList();
                ShowNotification($"已打开新图片，播放列表: {playListIndex + 1}/{playList.Count}");
            }
        }
        private void HandleDropWithExistingImage(List<string> newImageFiles)
        {
            // 如果还不在播放列表模式，进入播放列表模式
            if (!isPlayListMode)
            {
                InitializePlayListWithCurrentImage();
            }
    
            // 保存当前图片状态
            if (!settings.AutoSizeWindow)
            {
                SaveCurrentImageState();
            }
    
            // 记住第一张新图片
            string firstNewImage = newImageFiles[0];
    
            // 添加新图片到播放列表
            AddImagesToPlayList(newImageFiles);
    
            // 找到并切换到第一张新图片
            int newImageIndex = playList.IndexOf(firstNewImage);
            if (newImageIndex >= 0)
            {
                playListIndex = newImageIndex;
                LoadFromPlayList();
                ShowNotification($"已打开新图片，播放列表: {playListIndex + 1}/{playList.Count}");
            }
        }
        // 初始化播放列表（从多个拖入的文件）
        private void InitializePlayList(List<string> imageFiles)
        {
            playList.Clear();
            playList.AddRange(imageFiles);
            playListIndex = 0;
            isPlayListMode = true;
        }
        // 初始化播放列表（以当前图片为起点）
        // 修改InitializePlayListWithCurrentImage方法
        // 修改InitializePlayListWithCurrentImage方法
        private void InitializePlayListWithCurrentImage()
        {
            // 如果已经在播放列表模式，不要重新初始化
            if (isPlayListMode)
                return;
        
            playList.Clear();
            // 不清空状态记录
            // playListImageStates.Clear();
    
            if (!string.IsNullOrEmpty(currentImagePath))
            {
                playList.Add(currentImagePath);
                playListIndex = 0;
                isPlayListMode = true;
                // 检查是否需要启动自动播放
                if (chkAutoPlay.IsChecked == true && playList.Count > 1)
                {
                    isAutoPlaying = true;
                    StartAutoPlay();
                }
            }
        }

        // 添加图片到播放列表
        // 修改AddImagesToPlayList方法，在添加第一张图片时保存当前状态
        private void AddImagesToPlayList(List<string> imageFiles)
        {
            
    
            foreach (string imagePath in imageFiles)
            {
                //if (!playList.Contains(imagePath))
                //{
                    playList.Add(imagePath);
                //}
            }
        }
        // 在播放列表中切换到上一张
        private void PlayListPrevious()
        {
            UpdatePlaylistInfo();
            if (!isPlayListMode || playList.Count == 0)
                return;
    
            // 1. 保存当前图片的画布变换（仅在非自动调整窗口模式下）
            if (!settings.AutoSizeWindow)
            {
                SaveCurrentImageState();
            }
    
            // 2. 切换到上一张图片
            playListIndex = (playListIndex - 1 + playList.Count) % playList.Count;
    
            // 3. 加载新图片
            LoadFromPlayList();
        }


// 修改PlayListNext方法
        private void PlayListNext()
        {
            UpdatePlaylistInfo();
            if (!isPlayListMode || playList.Count == 0)
                return;
    
            // 1. 保存当前图片的画布变换（仅在非自动调整窗口模式下）
            if (!settings.AutoSizeWindow)
            {
                SaveCurrentImageState();
            }
    
            // 2. 切换到下一张图片
            playListIndex = (playListIndex + 1) % playList.Count;
    
            // 3. 加载新图片
            LoadFromPlayList();
        }
        // 从播放列表加载图片
        // 修改LoadFromPlayList方法
        
        
        private void ResetTransformForNewImage()
        {
            // 重置所有变换到默认值
            // 这不会影响已保存的状态，只是确保干净的起点
            scaleX = 1.0;
            scaleY = 1.0;
            translateX = 0;
            translateY = 0;
            baseTranslateX = 0;
            baseTranslateY = 0;
    
            // 重置抖动状态
            shakeTime = 0;
            currentPulseX = 0;
            currentPulseY = 0;
            targetPulseX = 0;
            targetPulseY = 0;
        }
        public void LoadFromPlayList()
{
    UpdatePlaylistInfo();
    // 更新播放列表控件状态
    UpdatePlayListControlsState();
    if (playListIndex >= 0 && playListIndex < playList.Count)
    {
        string imagePath = playList[playListIndex];
        // 如果要加载的图片就是当前图片，直接返回
       
        if (File.Exists(imagePath))
        {
            isLoadingFromPlayList = true;
            
            try
            {
                // 重要：在加载新图片前，先重置变换状态
                // 这样可以防���新图片的加载影响到其他��片的保存状态
                ResetTransformForNewImage();
                
                // 加载图片
                LoadImage(imagePath);
                
                // 确保图片完全加载后再处理状态
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (imgDisplay.Source is BitmapImage bitmap)
                    {
                        // 确保设置了图片的原始尺寸
                        imgDisplay.Width = bitmap.PixelWidth;
                        imgDisplay.Height = bitmap.PixelHeight;
                        string stateKey = $"{imagePath}_{playListIndex}";
                        if (!settings.AutoSizeWindow)
                        {
                            // 检查是否是新添加的图片
                            if (newlyAddedImages.Contains(imagePath))
                            {
                                // 新图片，适配到画布
                                FitImageToCanvas(bitmap);
                                if (!isAutoPlaying)
                                {
                                    ShowNotification($"播放列表: {playListIndex + 1}/{playList.Count} (新图片)");
                                }

                                // 从新图片集合中移除
                                newlyAddedImages.Remove(imagePath);
                            }
                            
                            else if (playListImageStates.ContainsKey(stateKey))
                            {
                                // 恢复保存的状态
                                RestoreImageState(imagePath);
                                if (!isAutoPlaying)
                                {
                                    ShowNotification($"播放列表: {playListIndex + 1}/{playList.Count}");
                                }
                            }
                            else
                            {
                                // 没有保存状态的旧图片，也执行 fit
                                FitImageToCanvas(bitmap);
                                if (!isAutoPlaying)
                                {
                                    ShowNotification($"播放列表: {playListIndex + 1}/{playList.Count}");
                                }
                            }
                        }
                        else
                        {
                            // 自动调整窗口模式
                            ResizeWindowToFitImage(bitmap);
                            FitImageToCanvas(bitmap);
                            if (!isAutoPlaying)
                            {
                                ShowNotification($"播放列表: {playListIndex + 1}/{playList.Count}");
                            }
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            finally
            {
                isLoadingFromPlayList = false;
            }
        }
        else
        {
            // 文件不存在的处理...
        }
    }
}
        // 添加一个标志来跟踪是否从播放列表加载
        private bool isLoadingFromPlayList = false;
        // 添加无参数版本的FitImageToCanvas方法
        private void FitImageToCanvas()
        {
            if (imgDisplay.Source is BitmapImage bitmap)
            {
                FitImageToCanvas(bitmap);
            }
        }
        // 保存当前图片状态
        private void SaveCurrentImageState()
        {
            if (!isPlayListMode || settings.AutoSizeWindow || string.IsNullOrEmpty(currentImagePath))
                return;
    
            var state = ImageState.FromCurrentState(this);
            // 使用组合键保存状态
            string stateKey = $"{currentImagePath}_{playListIndex}";
            playListImageStates[stateKey] = state;
    
            // 调试输出
            System.Diagnostics.Debug.WriteLine($"=== 保存状态 (切换时) ===");
            System.Diagnostics.Debug.WriteLine($"图片: {Path.GetFileName(currentImagePath)}");
            System.Diagnostics.Debug.WriteLine($"Scale: {scaleX}, {scaleY}");
            System.Diagnostics.Debug.WriteLine($"BaseTranslate: {baseTranslateX}, {baseTranslateY}");
        }
        // 恢复图片状态
        private void RestoreImageState(string imagePath)
        {
            string stateKey = $"{imagePath}_{playListIndex}";
            if (!playListImageStates.ContainsKey(stateKey))
                return;
    
            var state = playListImageStates[stateKey];
    
            // 调试输出
            System.Diagnostics.Debug.WriteLine($"=== 恢复状态 ===");
            System.Diagnostics.Debug.WriteLine($"图片: {Path.GetFileName(imagePath)}");
            System.Diagnostics.Debug.WriteLine($"恢复的Scale: {state.ScaleX}, {state.ScaleY}");
            System.Diagnostics.Debug.WriteLine($"恢复的BaseTranslate: {state.BaseTranslateX}, {state.BaseTranslateY}");
    
            state.ApplyToWindow(this);
        }
        // 退出播放列表模式
        // 修改ExitPlayListMode方法
        private void ExitPlayListMode()
        {
            UpdatePlaylistInfo();
            isPlayListMode = false;
            playList.Clear();
            playListIndex = 0;
    
            // 只在退出播放列表模式时清空状态记录
            playListImageStates.Clear();
            // 停止自动播放
            if (autoPlayTimer.IsEnabled)
            {
                StopAutoPlay();
            }
            // 更新控件状态
            UpdatePlayListControlsState();
            ShowNotification("已退出播放列表模式");
        }
        // 添加手动应用变换的方法
        private void ApplyTransform()
        {
            if (imgDisplay != null)
            {
                TransformGroup transformGroup = new TransformGroup();
        
                // 先缩放（从中心点）
                ScaleTransform scaleTransform = new ScaleTransform(scaleX, scaleY);
                transformGroup.Children.Add(scaleTransform);
        
                // 后平移
                TranslateTransform translateTransform = new TranslateTransform(translateX, translateY);
                transformGroup.Children.Add(translateTransform);
        
                imgDisplay.RenderTransform = transformGroup;
                imgDisplay.RenderTransformOrigin = new Point(0, 0);
            }
        }
        private void LoadImage(string filePath)
        {
            if (!File.Exists(filePath)) return;
    
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
        
                imgDisplay.Source = bitmap;
                if (hintText != null)
                    hintText.Visibility = Visibility.Collapsed;
        
                currentImagePath = filePath;
        
                // 只在非播放列表加载时处理目录
                if (!isLoadingFromPlayList)
                {
                    LoadImagesFromDirectory(Path.GetDirectoryName(filePath));
                    currentIndex = imageFiles.IndexOf(filePath);
                }
        
                // 不要在这里修改任何变换状态！
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载图片: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResizeWindowToFitImage(BitmapImage bitmap)
        {
            if (bitmap == null) return;
            
            // 获取屏幕工作区尺寸
            double screenWidth = SystemParameters.WorkArea.Width * 0.9;
            double screenHeight = SystemParameters.WorkArea.Height * 0.9;
            
            // 图片的原始宽高比
            double imageRatio = bitmap.PixelWidth / (double)bitmap.PixelHeight;
            
            // 计算适合屏幕的图片尺寸（不超过屏幕90%）
            double fittedWidth, fittedHeight;
            if (imageRatio > screenWidth / screenHeight)
            {
                // 图片较宽，以宽度为基准
                fittedWidth = screenWidth;
                fittedHeight = fittedWidth / imageRatio;
            }
            else
            {
                // 图片较高，以高度为基准
                fittedHeight = screenHeight;
                fittedWidth = fittedHeight * imageRatio;
            }
            
            // 这里减去窗口边框和标题栏，获取需要的客户区大小
            double nonClientWidth = SystemParameters.ResizeFrameVerticalBorderWidth * 2;
            double nonClientHeight = SystemParameters.ResizeFrameHorizontalBorderHeight * 2 + 
                                     SystemParameters.CaptionHeight;
            
            // 调整窗口大小 (加上窗口边框)
            Width = fittedWidth + nonClientWidth;
            Height = fittedHeight + nonClientHeight;
        }

        
        private void FitImageToCanvas(BitmapImage bitmap)
        {
            if (bitmap == null || canvas == null) return;
    
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;
            // 如果画布尺寸还未确定，延迟执行
            if (canvasWidth <= 0 || canvasHeight <= 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitImageToCanvas(bitmap);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            if (canvasWidth <= 0 || canvasHeight <= 0) return;
    
            double imageWidth = bitmap.PixelWidth;
            double imageHeight = bitmap.PixelHeight;
    
            // 计算缩放比例
            double scaleWidth = canvasWidth / imageWidth;
            double scaleHeight = canvasHeight / imageHeight;
            double scale = Math.Min(scaleWidth, scaleHeight);
    
            // 更新缩放
            scaleX = scale;
            scaleY = scale;
    
            // 计算居中位置
            double scaledWidth = imageWidth * scale;
            double scaledHeight = imageHeight * scale;
    
            // 设置基础位置（不含抖动）
            baseTranslateX = (canvasWidth - scaledWidth) / 2;
            baseTranslateY = (canvasHeight - scaledHeight) / 2;
            translateX = baseTranslateX;
            translateY = baseTranslateY;
    
            // 设置图片原始大小（重要！）
            imgDisplay.Width = imageWidth;
            imgDisplay.Height = imageHeight;
    
            // 应用变换
            ApplyTransform();
        }

        private void LoadImagesFromDirectory(string directory)
        {
            imageFiles.Clear();
            
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;
                
            imageFiles = Directory.GetFiles(directory)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .OrderBy(f => f)
                .ToList();
        }

        private void ResetView()
        {
            // 如果在播放列表模式且不自动调整窗口，不要重置视图
            if (isPlayListMode && !settings.AutoSizeWindow)
                return;
    
            scaleX = 1;
            scaleY = 1;
            translateX = 0;
            translateY = 0;
            baseTranslateX = 0;
            baseTranslateY = 0;
    
            if (imgDisplay != null)
            {
                imgDisplay.RenderTransform = new TransformGroup();
            }
        }
/*
        private void UpdateImageTransform()
        {
            if (imgDisplay.Source == null) return;
            
            imgDisplay.Width = ((BitmapImage)imgDisplay.Source).PixelWidth * scaleX;
            imgDisplay.Height = ((BitmapImage)imgDisplay.Source).PixelHeight * scaleY;
        }

        private void UpdateImagePosition()
        {
            Canvas.SetLeft(imgDisplay, translateX);
            Canvas.SetTop(imgDisplay, translateY);
        }
*/
        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (imgDisplay.Source != null)
            {
                if (imgDisplay.Source == null) return;
    
                lastMousePosition = e.GetPosition(canvas);
    
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    isPanning = true;
                    canvas.CaptureMouse(); // 添加这一行 - 捕获鼠标
                    e.Handled = true;
                }
                else if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
                {
                    // 双击重置图片到适合画布
                    if (imgDisplay.Source is BitmapImage bitmap)
                    {
                        FitImageToCanvas(bitmap);
                    }
                    e.Handled = true;
                }
            }
        }

        private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Released && isPanning)
            {
                isPanning = false;
                canvas.ReleaseMouseCapture(); // 添加这一行 - 释放鼠标捕获
        
                // ���果在播放列表模式下，保存当前图片的状态
                if (!settings.AutoSizeWindow)
                {
                    SaveCurrentImageState();
                }
        
                e.Handled = true;
            }
        }

        private void Canvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            // 如果失去鼠标捕获时仍在拖拽，结束拖拽状态
            if (isPanning)
            {
                isPanning = false;
        
                // 保存当前状态
                if (!settings.AutoSizeWindow)
                {
                    SaveCurrentImageState();
                }
        
                System.Diagnostics.Debug.WriteLine("拖拽因失去鼠标捕获而结束");
            }
        }
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning && imgDisplay.Source != null)
            {
                Point currentPosition = e.GetPosition(canvas);
                
                baseTranslateX += currentPosition.X - lastMousePosition.X;
                baseTranslateY += currentPosition.Y - lastMousePosition.Y;
                
                // 不在这里更新translateX/Y，而是在渲染循环中统一处理
                
                lastMousePosition = currentPosition;
                e.Handled = true;
            }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (imgDisplay.Source == null) return;
    
            // 获取鼠标在画布上的位置
            Point mousePos = e.GetPosition(canvas);
            double scaleFactor = e.Delta > 0 ? 1.1 : 0.9;
    
            // 计算鼠标位置相对于图片当前位置的偏移
            double mouseOffsetX = mousePos.X - baseTranslateX;
            double mouseOffsetY = mousePos.Y - baseTranslateY;
    
            // 保存旧的缩放值
            double oldScaleX = scaleX;
            double oldScaleY = scaleY;
    
            // 应用新的缩放
            scaleX *= scaleFactor;
            scaleY *= scaleFactor;
    
            // 限制缩放范围
            if (scaleX < 0.1) scaleX = scaleY = 0.1;
            if (scaleX > 10) scaleX = scaleY = 10;
    
            // 调整位置，使鼠标指向的点保持不变
            // 新的偏移 = 旧的偏移 * 缩放比例
            double newMouseOffsetX = mouseOffsetX * (scaleX / oldScaleX);
            double newMouseOffsetY = mouseOffsetY * (scaleY / oldScaleY);
    
            // ���整基础位置
            baseTranslateX = mousePos.X - newMouseOffsetX;
            baseTranslateY = mousePos.Y - newMouseOffsetY;
    
            e.Handled = true;
        }
        
        // 修改现有的Window_KeyDown方法中的方向键处理
private void Window_KeyDown(object sender, KeyEventArgs e)
{
    if (settings == null) return;
    
    bool isCtrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
    if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
    {
        e.Handled = true;
        BtnSavePlaylist_Click(null, null);
    }
    // Tab键显示/隐藏设置面板
    if (e.Key == Key.Tab)
    {
        // 切换设置面板的可见性
        if (settingsPanel.Visibility == Visibility.Visible)
        {
            settingsPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            settingsPanel.Visibility = Visibility.Visible;
        }
        e.Handled = true;
        return;
    }
    // 添加 F 键处理 - 适配图片到画布
    if (e.Key == Key.F)
    {
        if (imgDisplay.Source is BitmapImage bitmap)
        {
            FitImageToCanvas(bitmap);
            ShowNotification("图片已适配到画布");
            
            // 如果在播放列表模式下，保存当前状态
            if (!settings.AutoSizeWindow && isPlayListMode)
            {
                SaveCurrentImageState();
            }
        }
        e.Handled = true;
        return;
    }
    // 设置快捷键
    if (e.Key == Key.Up)
    {
        if (isCtrlPressed)
        {
            // 调整脉冲强度
            settings.PulsePowerX += 50;
            settings.PulsePowerY += 50;
            
            if (settings.PulsePowerX > 1000) settings.PulsePowerX = 1000;
            if (settings.PulsePowerY > 1000) settings.PulsePowerY = 1000;
            
            sliderPulsePowerX.Value = settings.PulsePowerX;
            sliderPulsePowerY.Value = settings.PulsePowerY;
            
            ShowNotification($"脉冲强度: {settings.PulsePowerX:F0}");
        }
        else
        {
            // 调整脉冲频率
            settings.PulseInterval += 0.1;
            if (settings.PulseInterval > 2.0) settings.PulseInterval = 2.0;
            
            sliderPulseInterval.Value = settings.PulseInterval;
            
            ShowNotification($"脉冲间隔: {settings.PulseInterval:F1}s");
        }
        e.Handled = true;
        return;
    }
    
    if (e.Key == Key.Down)
    {
        if (isCtrlPressed)
        {
            // 调整脉冲强度
            settings.PulsePowerX -= 50;
            settings.PulsePowerY -= 50;
            
            if (settings.PulsePowerX < 0) settings.PulsePowerX = 0;
            if (settings.PulsePowerY < 0) settings.PulsePowerY = 0;
            
            sliderPulsePowerX.Value = settings.PulsePowerX;
            sliderPulsePowerY.Value = settings.PulsePowerY;
            
            ShowNotification($"脉冲强度: {settings.PulsePowerX:F0}");
        }
        else
        {
            // 调整脉冲频率
            settings.PulseInterval -= 0.1;
            if (settings.PulseInterval < 0.1) settings.PulseInterval = 0.1;
            
            sliderPulseInterval.Value = settings.PulseInterval;
            
            ShowNotification($"脉冲间隔: {settings.PulseInterval:F1}s");
        }
        e.Handled = true;
        return;
    }
    // 空格键切换自动播放
    if (e.Key == Key.Space)
    {
        // 只在播放列表模式下响应
        if (isPlayListMode && playList.Count > 1)
        {
            chkAutoPlay.IsChecked = !chkAutoPlay.IsChecked;
            e.Handled = true;
        }
        return;
    }
    // 处理数字键1-4切换预设
    if ((e.Key >= Key.D1 && e.Key <= Key.D4) || (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad4))
    {
        int presetIndex;
        
        if (e.Key >= Key.D1 && e.Key <= Key.D4)
            presetIndex = (int)e.Key - (int)Key.D1;
        else
            presetIndex = (int)e.Key - (int)Key.NumPad1;
            
        ApplyPreset(presetIndex);
        e.Handled = true;
        return;
    }
    
    // 左右方向键处理 - 根据模式选择不同的行为
    if (e.Key == Key.Left)
    {
        if (isPlayListMode)
        {
            // 播放列表模式：在播放列表中切换
            PlayListPrevious();
        }
        else
        {
            // 目录模式：在目录中切换
            if (imageFiles.Count > 1)
            {
                currentIndex = (currentIndex - 1 + imageFiles.Count) % imageFiles.Count;
                LoadFromCommandLine(imageFiles[currentIndex]);
            }
        }
        e.Handled = true;
    }
    else if (e.Key == Key.Right)
    {
        if (isPlayListMode)
        {
            // 播放列表模式：在播放列表中切换
            PlayListNext();
        }
        else
        {
            // 目录模式：在目录中切换
            if (imageFiles.Count > 1)
            {
                currentIndex = (currentIndex + 1) % imageFiles.Count;
                LoadFromCommandLine(imageFiles[currentIndex]);
            }
        }
        e.Handled = true;
    }
    
    // ESC键退出播放列表模式
    if (e.Key == Key.Escape && isPlayListMode)
    {
        ExitPlayListMode();
        e.Handled = true;
    }
}
        
// 修改LoadFromCommandLine方法，在加载新图片前保存当前状态
        public void LoadFromCommandLine(string filePath)
        {
            if (!File.Exists(filePath) || settings == null) return;
    
            // 如果在播放列表模式下从外部加载新图片，退出播放列表模式
            if (isPlayListMode && !playList.Contains(filePath))
            {
                ExitPlayListMode();
            }
    
            try
            {
                LoadImage(filePath);
        
                // 重置视图
                scaleX = 1;
                scaleY = 1;
                translateX = 0;
                translateY = 0;
                baseTranslateX = 0;
                baseTranslateY = 0;
        
                // 重置抖动
                shakeTime = 0;
                timeSinceLastPulse = 0;
                nextPulseTime = 0;
                currentPulseX = 0;
                currentPulseY = 0;
                targetPulseX = 0;
                targetPulseY = 0;
        
                if (imgDisplay.Source is BitmapImage bitmap)
                {
                    if (settings.AutoSizeWindow)
                    {
                        ResizeWindowToFitImage(bitmap);
                    }
            
                    FitImageToCanvas(bitmap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载图片: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
// 添加获取当前模式状态的方法（用于调试或状态显示）
private string GetCurrentModeStatus()
{
    if (isPlayListMode)
    {
        return $"播放列表模式 ({playListIndex + 1}/{playList.Count})";
    }
    else
    {
        return $"目录模式 ({currentIndex + 1}/{imageFiles.Count})";
    }
}
        private void BtnSelectColor_Click(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled) return;
            
            // 这个按钮现在打开高级颜色选项
            var expanders = settingsPanel.FindChildren<Expander>();
            if (expanders.Any())
            {
                expanders.First().IsExpanded = true;
            }
        }
        
        private void BtnResetColor_Click(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled || settings == null) return;
            
            Color defaultColor = Color.FromRgb(240, 240, 240);
            currentBackgroundColor = defaultColor;
            UpdateColorPreview();
            
            // 更新滑块
            sliderRed.Value = defaultColor.R;
            sliderGreen.Value = defaultColor.G;
            sliderBlue.Value = defaultColor.B;
            
            // 更新设置
            settings.SetBackgroundColor(defaultColor);
        }
        
        private void ColorSwatch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!eventsEnabled || settings == null) return;
            
            if (sender is Border border && border.Tag is string colorHex)
            {
                try
                {
                    // 从十六进制颜色代码转换为Color
                    Color color = (Color)ColorConverter.ConvertFromString(colorHex);
                    currentBackgroundColor = color;
                    
                    // 更新滑块值
                    sliderRed.Value = color.R;
                    sliderGreen.Value = color.G;
                    sliderBlue.Value = color.B;
                    
                    UpdateColorPreview();
                    
                    // 更新设置
                    settings.SetBackgroundColor(color);
                }
                catch { }
            }
        }
        
        private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!eventsEnabled || settings == null || !IsLoaded) return;
            
            currentBackgroundColor = Color.FromRgb(
                (byte)sliderRed.Value,
                (byte)sliderGreen.Value,
                (byte)sliderBlue.Value);
                
            UpdateColorPreview();
            
            // 更新设置
            settings.SetBackgroundColor(currentBackgroundColor);
        }
        
        private void ChkAutoSizeWindow_Changed(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled || settings == null) return;
            
            settings.AutoSizeWindow = chkAutoSizeWindow.IsChecked ?? true;
            
            // 如果当前已加载图片且选项被启用，则重新调整窗口大小
            if (settings.AutoSizeWindow && imgDisplay.Source != null)
            {
                ResizeWindowToFitImage((BitmapImage)imgDisplay.Source);
                FitImageToCanvas((BitmapImage)imgDisplay.Source);
            }
        }
        private void ChkAutoBorderless_Changed(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled || settings == null) return;
            
            settings.AutoBorderless = chkAutoBorderless.IsChecked ?? true;
        
        }
        private void ChkEnableShake_Changed(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled || settings == null) return;
            
            settings.EnableShake = chkEnableShake.IsChecked ?? false;
        }
        
        private void ShakeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!eventsEnabled || settings == null || !IsLoaded) return;
            
            if (sender == sliderShakeAmount)
            {
                settings.ShakeAmount = sliderShakeAmount.Value;
            }
            else if (sender == sliderShakeFrequency)
            {
                settings.ShakeFrequency = sliderShakeFrequency.Value;
            }
        }
        
        // 脉冲相关方法
        private void ChkEnablePulse_Changed(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled || settings == null) return;
            
            settings.EnablePulse = chkEnablePulse.IsChecked ?? false;
            
            // 如果启用脉冲，重置计时器
            if (settings.EnablePulse)
            {
                CalculateNextPulseTime();
            }
            else
            {
                // 如果禁用脉冲，清除当前脉冲效果
                currentPulseX = currentPulseY = 0;
                targetPulseX = targetPulseY = 0;
            }
        }
        // 窗口获得焦点事件
        private void Window_Activated(object sender, System.EventArgs e)
        {
            // 切换成正常边框
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            this.ResizeMode = ResizeMode.CanResize;
            this.Background = null; // 恢复背景
        }

        // 窗口失去焦点事件
        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            // 切换成无边框
            if(settings.AutoBorderless){
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.Background = Brushes.Transparent; // 设置背景为透明
            }
            else
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.ResizeMode = ResizeMode.CanResize;
                this.Background = null; // 恢复背景
            }
        }
        private void PulseSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!eventsEnabled || settings == null || !IsLoaded) return;
            
            if (sender == sliderPulseInterval)
            {
                settings.PulseInterval = sliderPulseInterval.Value;
            }
            else if (sender == sliderPulseRandomness)
            {
                settings.PulseRandomness = sliderPulseRandomness.Value;
            }
            else if (sender == sliderPulseDamping)
            {
                settings.PulseDamping = sliderPulseDamping.Value;
            }
            else if (sender == sliderPulsePowerX)
            {
                settings.PulsePowerX = sliderPulsePowerX.Value;
            }
            else if (sender == sliderPulsePowerY)
            {
                settings.PulsePowerY = sliderPulsePowerY.Value;
            }
        }
        
        private void BtnTriggerPulse_Click(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled || settings == null) return;
            
            if (settings.EnablePulse && imgDisplay.Source != null)
            {
                TriggerPulse();
            }
        }
        // 获取当前图片状态
        public PlaylistItemState GetCurrentPlaylistItemState()
        {
            return new PlaylistItemState
            {
                ScaleX = this.scaleX,
                ScaleY = this.scaleY,
                TranslateX = this.translateX,
                TranslateY = this.translateY,
                BaseTranslateX = this.baseTranslateX,
                BaseTranslateY = this.baseTranslateY,
                EnableShake = this.settings.EnableShake,
                ShakeAmount = this.settings.ShakeAmount,
                ShakeFrequency = this.settings.ShakeFrequency,
                EnablePulse = this.settings.EnablePulse,
                PulseInterval = this.settings.PulseInterval,
                PulseRandomness = this.settings.PulseRandomness,
                PulseDamping = this.settings.PulseDamping,
                PulsePowerX = this.settings.PulsePowerX,
                PulsePowerY = this.settings.PulsePowerY
            };
        }
        // 获取播放列表设置
        public PlaylistSettings GetCurrentPlaylistSettings()
        {
            return new PlaylistSettings
            {
                AutoPlayEnabled = this.settings.AutoPlayEnabled,
                AutoPlayInterval = this.settings.AutoPlayInterval,
                AutoPlayRandomness = this.settings.AutoPlayRandomness,
                RandomPlayback = this.settings.RandomPlayback,
                AutoSizeWindow = this.settings.AutoSizeWindow
            };
        }
        // 应用播放列表项状态
        public void ApplyPlaylistItemState(PlaylistItemState state)
        {
            this.scaleX = state.ScaleX;
            this.scaleY = state.ScaleY;
            this.translateX = state.TranslateX;
            this.translateY = state.TranslateY;
            this.baseTranslateX = state.BaseTranslateX;
            this.baseTranslateY = state.BaseTranslateY;

            // 暂停事件
            SuspendEvents();

            // 应用抖动设置
            this.settings.EnableShake = state.EnableShake;
            this.settings.ShakeAmount = state.ShakeAmount;
            this.settings.ShakeFrequency = state.ShakeFrequency;
            this.settings.EnablePulse = state.EnablePulse;
            this.settings.PulseInterval = state.PulseInterval;
            this.settings.PulseRandomness = state.PulseRandomness;
            this.settings.PulseDamping = state.PulseDamping;
            this.settings.PulsePowerX = state.PulsePowerX;
            this.settings.PulsePowerY = state.PulsePowerY;

            // 更新UI
            ApplySettingsToUI();
    
            // 恢复事件
            ResumeEvents();

            // 应用变换
            ApplyTransform();
        }

// 应用播放列表设置
        public void ApplyPlaylistSettings(PlaylistSettings settings)
        {
            SuspendEvents();

            this.settings.AutoPlayEnabled = settings.AutoPlayEnabled;
            this.settings.AutoPlayInterval = settings.AutoPlayInterval;
            this.settings.AutoPlayRandomness = settings.AutoPlayRandomness;
            this.settings.RandomPlayback = settings.RandomPlayback;
            this.settings.AutoSizeWindow = settings.AutoSizeWindow;

            ApplySettingsToUI();
            ResumeEvents();
        }

        // 在MainWindow类中添加这些方法

        private void BtnSavePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlayListMode || playList.Count == 0)
            {
                ShowNotification("请先创建播放列表");
                return;
            }

            // 保存当前图片状态
            if (!settings.AutoSizeWindow)
            {
                SaveCurrentImageState();
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "保存播放列表",
                Filter = "Gazer4 播放列表 (*.gzpl)|*.gzpl",
                DefaultExt = ".gzpl",
                FileName = $"播放列表_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                if (PlaylistManager.SavePlaylist(saveDialog.FileName, this))
                {
                    ShowNotification($"播放列表已保存到: {Path.GetFileName(saveDialog.FileName)}");
                }
            }
        }

        private void BtnLoadPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "加载播放列表",
                Filter = "Gazer4 播放列表 (*.gzpl)|*.gzpl|所有文件 (*.*)|*.*",
                DefaultExt = ".gzpl"
            };

            if (openDialog.ShowDialog() == true)
            {
                PlaylistManager.LoadPlaylist(openDialog.FileName, this);
            }
        }

        private void ChkRegisterFileType_Changed(object sender, RoutedEventArgs e)
        {
            if (!eventsEnabled) return;

            if (chkRegisterFileType.IsChecked == true)
            {
                if (PlaylistManager.RegisterFileAssociation())
                {
                    ShowNotification("文件关联注册成功");
                }
            }
        }

// 更新播放列表信息显示
        private void UpdatePlaylistInfo()
        {
            if (lblPlaylistInfo != null)
            {
                if (isPlayListMode)
                {
                    lblPlaylistInfo.Text = $"プレイリスト: {playListIndex + 1}/{playList.Count}";
                }
                else
                {
                    lblPlaylistInfo.Text = "現在プレイリストはありません";
                }
            }
        }
        
        // 语言选择ComboBox事件处理
        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!eventsEnabled) return;
            
            if (cmbLanguage.SelectedItem == cmbItemChinese)
            {
                settings.SetLanguageByID(0);
            }
            else if (cmbLanguage.SelectedItem == cmbItemJapanese)
            {
                settings.SetLanguageByID(2);
            }
            else if (cmbLanguage.SelectedItem == cmbItemEnglish)
            {
                settings.SetLanguageByID(1);
            }
            
            // 保存设置
            settings.Save();
            
            // 应用新语言
            ApplyLanguage();
            
            // 显示通知
            ShowNotification(LanguageManager.GetText("SettingsSaved", settings.CurrentLanguage));
        }
        
        // 应用当前语言到UI
        private void ApplyLanguage()
        {
            Language lang = settings.CurrentLanguage;
            
            // 设置窗口标题
            Title = LanguageManager.GetText("WindowTitle", lang);
            
            // 提示文字
            hintText.Text = LanguageManager.GetText("DragDropHint", lang);
            
            // 设置面板标题
            var settingsPanelTitle = settingsPanel.FindName("settingsPanelTitle") as TextBlock;
            if (settingsPanelTitle != null)
            {
                settingsPanelTitle.Text = LanguageManager.GetText("SettingsPanel", lang);
            }
            
            var settingsPanelCloseHint = settingsPanel.FindName("settingsPanelCloseHint") as TextBlock;
            if (settingsPanelCloseHint != null)
            {
                settingsPanelCloseHint.Text = LanguageManager.GetText("CloseHint", lang);
            }
            
            generalSettingsTitle.Text = LanguageManager.GetText("GeneralSettings", lang);
            chkShowNotifications.Content = LanguageManager.GetText("ShowNotifications", lang);
            chkAutoSizeWindow.Content = LanguageManager.GetText("AutoSizeWindow", lang);
            chkAutoBorderless.Content = LanguageManager.GetText("AutoHideBorder", lang);
            chkEnableShake.Content = LanguageManager.GetText("EnableShake", lang);
            playbackControlTitle.Text = LanguageManager.GetText("PlaybackControl", lang);
            chkAutoPlay.Content = LanguageManager.GetText("AutoPlay", lang);
            chkRandomPlayback.Content = LanguageManager.GetText("RandomPlayback", lang);
            chkRegisterFileType.Content = LanguageManager.GetText("RegisterFileType", lang);
            chkEnablePulse.Content = LanguageManager.GetText("EnablePulse", lang);
            playbackIntervalLabel.Text = LanguageManager.GetText("PlayInterval", lang);
            randomnessLabel.Text = LanguageManager.GetText("RandomnessAmount", lang);
            btnSavePlaylist.Content = LanguageManager.GetText("SavePlaylist", lang);
            btnLoadPlaylist.Content = LanguageManager.GetText("LoadPlaylist", lang);
            btnSelectColor.Content = LanguageManager.GetText("SelectColor", lang);
            btnResetColor.Content = LanguageManager.GetText("ResetColor", lang);
            VisualSetting.Text = LanguageManager.GetText("VisualEffects", lang);
            ShakeSettings.Text = LanguageManager.GetText("ShakeControl", lang);
            chkEnableShake.Content = LanguageManager.GetText("GeneralShake", lang);
            ShakeStrength.Text = LanguageManager.GetText("ShakeStrength", lang);
            ShakeSpeed.Text = LanguageManager.GetText("ShakeSpeed", lang);
            chkEnablePulse.Content = LanguageManager.GetText("PulseShake", lang);
            
            ImpulseInterval.Text = LanguageManager.GetText("PulseInterval", lang);
            ImpulseRandomness.Text = LanguageManager.GetText("PulseRandomness", lang);
            ImpulseDamping.Text = LanguageManager.GetText("PulseDamping", lang);
            ImpulseHStrength.Text = LanguageManager.GetText("PulseHorizontalPower", lang);
            ImpulseVStrength.Text = LanguageManager.GetText("PulseVerticalPower", lang);
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            // 语言设置
            lblLanguage.Text = LanguageManager.GetText("Language", lang);
            
            // 在ComboBox中选择当前语言
            int languageID = settings.GetCurrentLanguageID();
            switch (languageID)
            {
                case 0:
                    cmbLanguage.SelectedItem = cmbItemChinese;
                    break;
                case 2:
                    cmbLanguage.SelectedItem = cmbItemJapanese;
                    break;
                case 1:
                    cmbLanguage.SelectedItem = cmbItemEnglish;
                    break;
            }
        }
        
    }
   
    // 扩展方法用于查找控件
    public static class UIHelperExtensions
    {
        public static IEnumerable<T> FindChildren<T>(this DependencyObject parent)
            where T : DependencyObject
        {
            if (parent == null) yield break;
            
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T item)
                {
                    yield return item;
                }
                
                foreach (var childOfChild in FindChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }
    
}


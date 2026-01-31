using System;
using System.IO;
using System.Windows.Media;
using System.Xml.Serialization;

namespace ImageViewer
{
    [Serializable]
    public class AppSettings
    {
        // 语言设置
        public Language CurrentLanguage { get; set; } = Language.Japanese; // 默认语言
        
        // 在现有属性中添加这个
        public bool UseAbsolutePaths { get; set; } = false;
        public bool ShowNotifications { get; set; } = false; // 默认启用提示
        // 背景颜色
        public byte BackgroundColorR { get; set; } = 240;
        public byte BackgroundColorG { get; set; } = 240;
        public byte BackgroundColorB { get; set; } = 240;
        
        // 基本设置
        public bool AutoSizeWindow { get; set; } = true;
        public bool AutoBorderless { get; set; } = true;
        
        // 播放列表自动播放设置
        public bool AutoPlayEnabled { get; set; } = false;
        public double AutoPlayInterval { get; set; } = 3.0; // 默认3秒
        public double AutoPlayRandomness { get; set; } = 0.0; // 0-100，随机偏移程度
        public bool RandomPlayback { get; set; } = false; // 是否随机播放
        // 抖动设置
        public bool EnableShake { get; set; } = true;
        public double ShakeAmount { get; set; } = 20;
        public double ShakeFrequency { get; set; } = 1.5;
        
        // 脉冲设置
        public bool EnablePulse { get; set; } = false;
        public double PulseInterval { get; set; } = 0.5;
        public double PulseRandomness { get; set; } = 30;
        public double PulseDamping { get; set; } = 10;
        public double PulsePowerX { get; set; } = 100;
        public double PulsePowerY { get; set; } = 100;
        
        // 从设置中获取颜色
        public Color GetBackgroundColor()
        {
            return Color.FromRgb(BackgroundColorR, BackgroundColorG, BackgroundColorB);
        }
        
        // 设置颜色
        public void SetBackgroundColor(Color color)
        {
            BackgroundColorR = color.R;
            BackgroundColorG = color.G;
            BackgroundColorB = color.B;
        }
        
        public int GetCurrentLanguageID()
        {
            switch (CurrentLanguage)
            {
                case Language.Chinese:
                    return 0;
                case Language.English:
                    return 1;
                case Language.Japanese:
                    return 2;
                default:
                    return 2;
            }
        }
        
        public void SetLanguageByID(int id)
        {
            switch (id)
            {
                case 0:
                    CurrentLanguage = Language.Chinese;
                    break;
                case 1:
                    CurrentLanguage = Language.English;
                    break;
                case 2:
                    CurrentLanguage = Language.Japanese;
                    break;
                default:
                    CurrentLanguage = Language.Japanese; // 默认日本语
                    break;
            }
        }
        
        
        
        
        
        
        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ImageViewer",
            "settings.xml");
            
        // 保存设置
        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                    
                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                using (FileStream stream = new FileStream(SettingsPath, FileMode.Create))
                {
                    serializer.Serialize(stream, this);
                }
            }
            catch { /* 忽略保存错误 */ }
        }
        
        // 加载设置
        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();
                
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                using (FileStream stream = new FileStream(SettingsPath, FileMode.Open))
                {
                    return (AppSettings)serializer.Deserialize(stream);
                }
            }
            catch
            {
                return new AppSettings();
            }
        }
    }
}


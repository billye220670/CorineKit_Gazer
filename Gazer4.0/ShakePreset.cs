// ShakePreset.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ImageViewer
{
    [Serializable]
    public class ShakePreset
    {
        // 正弦抖动参数
        public bool EnableShake { get; set; }
        public double ShakeAmount { get; set; }
        public double ShakeFrequency { get; set; }
        
        // 脉冲抖动参数
        public bool EnablePulse { get; set; }
        public double PulseInterval { get; set; }
        public double PulseRandomness { get; set; }
        public double PulseDamping { get; set; }
        public double PulsePowerX { get; set; }
        public double PulsePowerY { get; set; }
        
        // 预设名称
        public string Name { get; set; }
        
        // 从应用程序设置创建预设
        public static ShakePreset FromSettings(AppSettings settings, string name)
        {
            return new ShakePreset
            {
                // 复制正弦抖动参数
                EnableShake = settings.EnableShake,
                ShakeAmount = settings.ShakeAmount,
                ShakeFrequency = settings.ShakeFrequency,
                
                // 复制脉冲抖动参数
                EnablePulse = settings.EnablePulse,
                PulseInterval = settings.PulseInterval,
                PulseRandomness = settings.PulseRandomness,
                PulseDamping = settings.PulseDamping,
                PulsePowerX = settings.PulsePowerX,
                PulsePowerY = settings.PulsePowerY,
                
                // 设置预设名称
                Name = name
            };
        }
        
        // 将预设应用到设置对象
        public void ApplyToSettings(AppSettings settings)
        {
            // 应用正弦抖动参数
            settings.EnableShake = EnableShake;
            settings.ShakeAmount = ShakeAmount;
            settings.ShakeFrequency = ShakeFrequency;
            
            // 应用脉冲抖动参数
            settings.EnablePulse = EnablePulse;
            settings.PulseInterval = PulseInterval;
            settings.PulseRandomness = PulseRandomness;
            settings.PulseDamping = PulseDamping;
            settings.PulsePowerX = PulsePowerX;
            settings.PulsePowerY = PulsePowerY;
        }
    }
    
    public static class PresetManager
    {
        private static List<ShakePreset> _presets = new List<ShakePreset>(4);
        private static string PresetsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ImageViewer",
            "presets.xml");
            
        // 初始化预设列表
        static PresetManager()
        {
            // 加载预设
            LoadPresets();
        }
        
        // 获取指定索引的预设
        public static ShakePreset GetPreset(int index)
        {
            EnsurePresetList();
            
            if (index >= 0 && index < _presets.Count)
                return _presets[index];
                
            return null;
        }
        
        // 保存预设到指定索引
        public static void SavePreset(AppSettings settings, int index, string name = null)
        {
            EnsurePresetList();
            
            if (index < 0 || index >= 4)
                return;
                
            // 创建预设
            string presetName = name ?? $"预设 {index + 1}";
            var preset = ShakePreset.FromSettings(settings, presetName);
            
            // 设置或替换预设
            if (index < _presets.Count)
                _presets[index] = preset;
            else
                _presets.Add(preset);
                
            // 保存预设到文件
            SavePresets();
        }
        
        // 应用预设到设置
        public static bool ApplyPreset(AppSettings settings, int index)
        {
            var preset = GetPreset(index);
            if (preset == null)
                return false;
                
            preset.ApplyToSettings(settings);
            return true;
        }
        
        // 确保预设列表已初始化
        private static void EnsurePresetList()
        {
            if (_presets == null)
                _presets = new List<ShakePreset>(4);
        }
        
        // 加载预设从文件
        private static void LoadPresets()
        {
            try
            {
                if (File.Exists(PresetsPath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<ShakePreset>));
                    using (FileStream stream = new FileStream(PresetsPath, FileMode.Open))
                    {
                        _presets = (List<ShakePreset>)serializer.Deserialize(stream);
                    }
                }
                else
                {
                    _presets = new List<ShakePreset>(4);
                }
            }
            catch
            {
                _presets = new List<ShakePreset>(4);
            }
        }
        
        // 保存预设到文件
        private static void SavePresets()
        {
            try
            {
                string directory = Path.GetDirectoryName(PresetsPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                    
                XmlSerializer serializer = new XmlSerializer(typeof(List<ShakePreset>));
                using (FileStream stream = new FileStream(PresetsPath, FileMode.Create))
                {
                    serializer.Serialize(stream, _presets);
                }
            }
            catch { /* 忽略保存错误 */ }
        }
        
        // 检查预设是否存在
        public static bool PresetExists(int index)
        {
            return index >= 0 && index < _presets.Count && _presets[index] != null;
        }
    }
}
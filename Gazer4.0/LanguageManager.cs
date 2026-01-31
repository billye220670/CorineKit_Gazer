using System;
using System.Collections.Generic;
using System.Globalization;

namespace ImageViewer
{
    public enum Language
    {
        Chinese,
        Japanese,
        English
    }

    public static class LanguageManager
    {
        private static Dictionary<string, Dictionary<Language, string>> translations = new Dictionary<string, Dictionary<Language, string>>();

        static LanguageManager()
        {
            InitializeTranslations();
        }

        private static void InitializeTranslations()
        {
            // 窗口标题
            AddTranslation("WindowTitle", 
                "凝视者", 
                "凝視者", 
                "Gazer");

            // 提示文字
            AddTranslation("DragDropHint", 
                "将图像拖放到此处\n右击并拖动可移动\n滚轮缩放\n左右箭头切换图像\nTab键打开设置", 
                "画像をここにドラッグ＆ドロップ\n右クリックでドラッグすると移動\nホイールでズーム\n左右の矢印キーで画像切替\nTabキーで設定を開く", 
                "Drag & drop images here\nRight-click and drag to move\nWheel to zoom\nArrow keys to switch images\nTab to open settings");

            // 设置面板
            AddTranslation("SettingsPanel", 
                "设置面板", 
                "設定パネル", 
                "Settings Panel");

            AddTranslation("CloseHint", 
                "按Tab关闭", 
                "Tabを押して閉じる", 
                "Press Tab to close");

            // 通用设置
            AddTranslation("GeneralSettings", 
                "常规设置", 
                "一般的な設定", 
                "General Settings");

            AddTranslation("ShowNotifications", 
                "显示通知", 
                "通知を表示", 
                "Show notifications");

            AddTranslation("AutoSizeWindow", 
                "自动调整窗口大小", 
                "画像サイズのウィンドウ", 
                "Auto-size window");

            AddTranslation("AutoHideBorder", 
                "自动隐藏边框", 
                "自動でボーダーを隠す", 
                "Auto-hide border");

            // 播放控制
            AddTranslation("PlaybackControl", 
                "播放控制", 
                "再生コントロール", 
                "Playback Control");

            AddTranslation("AutoPlay", 
                "自动播放 (空格键)", 
                "自動再生（space）", 
                "Auto play (space)");

            AddTranslation("RandomPlayback", 
                "随机播放", 
                "シャッフル再生", 
                "Random playback");

            AddTranslation("PlayInterval", 
                "播放间隔", 
                "再生間隔", 
                "Play interval");

            AddTranslation("RandomnessAmount", 
                "随机程度", 
                "ランダム度", 
                "Randomness");

            // 抖动设置
            AddTranslation("ShakeSettings", 
                "抖动设置", 
                "揺れ設定", 
                "Shake Settings");

            AddTranslation("EnableShake", 
                "启用抖动", 
                "揺れを有効化", 
                "Enable shake");

            AddTranslation("ShakeAmount", 
                "抖动幅度", 
                "揺れ量", 
                "Shake amount");

            AddTranslation("ShakeFrequency", 
                "抖动频率", 
                "揺れ頻度", 
                "Shake frequency");

            // 脉冲设置
            AddTranslation("PulseSettings", 
                "脉冲设置", 
                "パルス設定", 
                "Pulse Settings");

            AddTranslation("EnablePulse", 
                "启用脉冲", 
                "パルスを有効化", 
                "Enable pulse");

            AddTranslation("PulseInterval", 
                "脉冲间隔", 
                "パルス間隔", 
                "Pulse interval");

            AddTranslation("PulseRandomness", 
                "脉冲随机度", 
                "パルスランダム度", 
                "Pulse randomness");

            AddTranslation("PulseDamping", 
                "脉冲阻尼", 
                "パルス減衰", 
                "Pulse damping");

            AddTranslation("PulsePower", 
                "脉冲强度", 
                "パルス強度", 
                "Pulse power");

            // 预设
            AddTranslation("ShakePresets", 
                "抖动预设", 
                "揺れプリセット", 
                "Shake presets");

            AddTranslation("PresetNone", 
                "无", 
                "なし", 
                "None");

            AddTranslation("PresetMild", 
                "轻微", 
                "微弱", 
                "Mild");

            AddTranslation("PresetMedium", 
                "中等", 
                "普通", 
                "Medium");

            AddTranslation("PresetStrong", 
                "强烈", 
                "強い", 
                "Strong");

            AddTranslation("PresetExtreme", 
                "极端", 
                "激しい", 
                "Extreme");

            AddTranslation("PresetCustom", 
                "自定义", 
                "カスタム", 
                "Custom");

            // 语言设置
            AddTranslation("LanguageSettings", 
                "语言设置", 
                "言語設定", 
                "Language Settings");

            AddTranslation("Language", 
                "语言", 
                "言語", 
                "Language");

            AddTranslation("ChineseText", 
                "中文", 
                "中国語", 
                "Chinese");

            AddTranslation("JapaneseText", 
                "日语", 
                "日本語", 
                "Japanese");

            AddTranslation("EnglishText", 
                "英语", 
                "英語", 
                "English");

            // 其他UI元素
            AddTranslation("SaveSettings", 
                "保存设置", 
                "設定を保存", 
                "Save Settings");

            AddTranslation("ResetSettings", 
                "重置设置", 
                "設定をリセット", 
                "Reset Settings");

            // 通知消息
            AddTranslation("SettingsSaved", 
                "设置已保存", 
                "設定が保存されました", 
                "Settings saved");

            AddTranslation("SettingsReset", 
                "设置已重置", 
                "設定がリセットされました", 
                "Settings reset");

            AddTranslation("ImageLoaded", 
                "图像已加载", 
                "画像が読み込まれました", 
                "Image loaded");

            AddTranslation("NoImagesFound", 
                "未找到图像", 
                "画像が見つかりませんでした", 
                "No images found");

            // 视觉效果
            AddTranslation("VisualEffects", 
                "视觉效果", 
                "視覚効果", 
                "Visual Effects");

            AddTranslation("BackgroundColor", 
                "背景颜色", 
                "背景色", 
                "Background Color");

            AddTranslation("SelectColor", 
                "选择颜色", 
                "色を選択", 
                "Select Color");

            AddTranslation("ResetToDefault", 
                "恢复默认", 
                "デ���ォルトに戻す", 
                "Reset to Default");

            AddTranslation("AdvancedColorSettings", 
                "高级颜色设置", 
                "高度な色彩設定", 
                "Advanced Color Settings");

            // 播放列表相关
            AddTranslation("SavePlaylist", 
                "保存播放列表", 
                "プレイリスト保存", 
                "Save Playlist");

            AddTranslation("LoadPlaylist", 
                "加载播放列表", 
                "プレイリスト読み込み", 
                "Load Playlist");

            AddTranslation("UseAbsolutePaths", 
                "使用绝对路径", 
                "絶対パスで保存", 
                "Use Absolute Paths");

            AddTranslation("AssociateFileType", 
                "关联.gzpl文件", 
                ".gzpl を関連付け", 
                "Associate .gzpl files");

            AddTranslation("NotInPlaylistMode", 
                "当前不在播放列表模式", 
                "現在プレイリストモードではありません", 
                "Not in playlist mode");

            // 预设控制
            AddTranslation("PresetControl", 
                "预设控制", 
                "プリセット制御", 
                "Preset Control");

            AddTranslation("PresetInstructions", 
                "点击保存，按数字键1-4切换", 
                "保存をクリックし、数字1-4で切り替えます", 
                "Click save, switch with keys 1-4");

            AddTranslation("ApplyPreset", 
                "应用预设", 
                "プリセットを適用する", 
                "Apply Preset");

            // 脉冲设置详细项
            AddTranslation("PulseHorizontalPower", 
                "横向强度", 
                "横方向強度", 
                "Horizontal Power");

            AddTranslation("PulseVerticalPower", 
                "纵向强度", 
                "縦方向強度", 
                "Vertical Power");

            AddTranslation("PulseTrigger", 
                "单次触发", 
                "単回トリガー", 
                "Single Trigger");

            AddTranslation("ShakeControl", 
                "抖动控制", 
                "揺れ制御", 
                "Shake Control");

            AddTranslation("GeneralShake", 
                "常规抖动", 
                "一般的な揺れ", 
                "General Shake");

            AddTranslation("ShakeStrength", 
                "强度", 
                "強度", 
                "Strength");

            AddTranslation("ShakeSpeed", 
                "速度", 
                "速さ", 
                "Speed");

            AddTranslation("PulseShake", 
                "脉冲抖动", 
                "パルスの揺れ", 
                "Pulse Shake");

            AddTranslation("IntervalRandomOffset", 
                "间隔随机偏移", 
                "間隔のランダムオフセット", 
                "Interval Random Offset");
        }

        private static void AddTranslation(string key, string chinese, string japanese, string english)
        {
            translations[key] = new Dictionary<Language, string>
            {
                { Language.Chinese, chinese },
                { Language.Japanese, japanese },
                { Language.English, english }
            };
        }

        public static string GetText(string key, Language language)
        {
            if (translations.TryGetValue(key, out var languageDict) && 
                languageDict.TryGetValue(language, out var text))
            {
                return text;
            }
            
            // 如果没找到翻译，返回key值作为备用
            return key;
        }

        public static Language GetSystemLanguage()
        {
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            string languageCode = currentCulture.TwoLetterISOLanguageName.ToLower();
            
            switch (languageCode)
            {
                case "zh":
                    return Language.Chinese;
                case "ja":
                    return Language.Japanese;
                default:
                    return Language.English;
            }
        }
    }
}

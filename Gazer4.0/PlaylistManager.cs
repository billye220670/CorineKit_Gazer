
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Microsoft.Win32;
using System.Windows;

namespace ImageViewer
{
    // 播放列表文件数据结构
    public class PlaylistFile
    {
        public string Name { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";
        public List<PlaylistItem> Items { get; set; } = new List<PlaylistItem>();
        public PlaylistSettings Settings { get; set; } = new PlaylistSettings();
    }

    // 播放列表项
    public class PlaylistItem
    {
        public string FilePath { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public DateTime AddedDate { get; set; } = DateTime.Now;
        public PlaylistItemState State { get; set; } = new PlaylistItemState();
    }

    // 播放列表项状态
    public class PlaylistItemState
    {
        public double ScaleX { get; set; } = 1.0;
        public double ScaleY { get; set; } = 1.0;
        public double TranslateX { get; set; } = 0;
        public double TranslateY { get; set; } = 0;
        public double BaseTranslateX { get; set; } = 0;
        public double BaseTranslateY { get; set; } = 0;
        
        // 抖动设置
        public bool EnableShake { get; set; } = false;
        public double ShakeAmount { get; set; } = 1.0;
        public double ShakeFrequency { get; set; } = 1.0;
        
        public bool EnablePulse { get; set; } = false;
        public double PulseInterval { get; set; } = 1.0;
        public double PulseRandomness { get; set; } = 50;
        public double PulseDamping { get; set; } = 5.0;
        public double PulsePowerX { get; set; } = 200;
        public double PulsePowerY { get; set; } = 200;
    }

    // 播放列表设置
    public class PlaylistSettings
    {
        public bool AutoPlayEnabled { get; set; } = false;
        public double AutoPlayInterval { get; set; } = 3.0;
        public double AutoPlayRandomness { get; set; } = 0;
        public bool RandomPlayback { get; set; } = false;
        public bool AutoSizeWindow { get; set; } = true;
    }

    // 播放列表管理器
    public static class PlaylistManager
    {
        private const string PLAYLIST_EXTENSION = ".gzpl";
        private const string REGISTRY_KEY = @"SOFTWARE\Classes\.gzpl";
        private const string PROGRAM_ID = "Gazer4.Playlist";

        // 保存播放列表
        public static bool SavePlaylist(string filePath, MainWindow mainWindow)
        {
            try
            {
                var playlist = new PlaylistFile
                {
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                // 获取播放列表基准目录（用于计算相对路径）
                string baseDirectory = Path.GetDirectoryName(filePath);

                // 添加播放列表项
                for (int i = 0; i < mainWindow.playList.Count; i++)
                {
                    string imagePath = mainWindow.playList[i];
                    string stateKey = $"{imagePath}_{i}";
                    var item = new PlaylistItem
                    {
                        FilePath = imagePath,
                        RelativePath = GetRelativePath(baseDirectory, imagePath),
                        AddedDate = DateTime.Now
                    };

                    // 保存图片状态
                    
                    if (mainWindow.playListImageStates.ContainsKey(stateKey))
                    {
                        var state = mainWindow.playListImageStates[stateKey];
                        item.State = ConvertImageStateToPlaylistState(state);
                    }
                    else if (i == mainWindow.playListIndex)
                    {
                        // 如果是当前图片，保存当前状态
                        item.State = GetCurrentImageState(mainWindow);
                    }

                    playlist.Items.Add(item);
                }

                // 保存播放列表设置
                playlist.Settings = new PlaylistSettings
                {
                    AutoPlayEnabled = mainWindow.settings.AutoPlayEnabled,
                    AutoPlayInterval = mainWindow.settings.AutoPlayInterval,
                    AutoPlayRandomness = mainWindow.settings.AutoPlayRandomness,
                    RandomPlayback = mainWindow.settings.RandomPlayback,
                    AutoSizeWindow = mainWindow.settings.AutoSizeWindow
                };

                // 序列化并保存
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(playlist, options);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存播放列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // 加载播放列表
        public static bool LoadPlaylist(string filePath, MainWindow mainWindow)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("播放列表文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                string json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                var playlist = JsonSerializer.Deserialize<PlaylistFile>(json);

                if (playlist == null || playlist.Items.Count == 0)
                {
                    MessageBox.Show("播放列表为空或格式错误", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // 获取播放列表目录（用于解析相对路径）
                string playlistDirectory = Path.GetDirectoryName(filePath);

                // 清空当前播放列表
                mainWindow.playList.Clear();
                mainWindow.playListImageStates.Clear();

                // 验证并加载图片
                var validItems = new List<PlaylistItem>();
                var missingFiles = new List<string>();
                var iteratorID = 0;
                foreach (var item in playlist.Items)
                {
                    
                    string actualPath = ResolveImagePath(item, playlistDirectory);
    
                    if (File.Exists(actualPath))
                    {
                        validItems.Add(item);
                        mainWindow.playList.Add(actualPath);

                        // 将PlaylistItemState转换为ImageState并保存
                        var imageState = new MainWindow.ImageState
                        {
                            ScaleX = item.State.ScaleX,
                            ScaleY = item.State.ScaleY,
                            TranslateX = item.State.TranslateX,
                            TranslateY = item.State.TranslateY,
                            BaseTranslateX = item.State.BaseTranslateX,
                            BaseTranslateY = item.State.BaseTranslateY,
                            EnableShake = item.State.EnableShake,
                            ShakeAmount = item.State.ShakeAmount,
                            ShakeFrequency = item.State.ShakeFrequency,
                            EnablePulse = item.State.EnablePulse,
                            PulseInterval = item.State.PulseInterval,
                            PulseRandomness = item.State.PulseRandomness,
                            PulseDamping = item.State.PulseDamping,
                            PulsePowerX = item.State.PulsePowerX,
                            PulsePowerY = item.State.PulsePowerY
                        };
                        string stateKey = $"{actualPath}_{iteratorID}";
                        mainWindow.playListImageStates[stateKey] = imageState;
                    }
                    else
                    {
                        missingFiles.Add(item.FilePath);
                    }

                    iteratorID++;
                }

                if (validItems.Count == 0)
                {
                    MessageBox.Show("播放列表中的所有图片文件都不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                // 显示丢失文件警告
                if (missingFiles.Count > 0)
                {
                    string missingList = string.Join("\n", missingFiles.Take(5));
                    if (missingFiles.Count > 5)
                    {
                        missingList += $"\n... 还有 {missingFiles.Count - 5} 个文件";
                    }
                    
                    MessageBox.Show($"以下文件未找到，已跳过:\n{missingList}", 
                                    "部分文件丢失", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // 设置播放列表模式
                mainWindow.isPlayListMode = true;
                mainWindow.playListIndex = 0;

                // 应用播放列表设置
                ApplyPlaylistSettings(playlist.Settings, mainWindow);

                // 加载第一张图片
                mainWindow.LoadFromPlayList();

                mainWindow.ShowNotification($"已加载播放列表 \"{playlist.Name}\" ({validItems.Count} 张图片)");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载播放列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // 注册文件关联
        public static bool RegisterFileAssociation()
        {
            try
            {
                string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                
                // 注册扩展名
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY))
                {
                    key.SetValue("", PROGRAM_ID);
                }

                // 注册程序ID
                using (var key = Registry.CurrentUser.CreateSubKey($@"SOFTWARE\Classes\{PROGRAM_ID}"))
                {
                    key.SetValue("", "Gazer4 播放列表");
                    key.SetValue("FriendlyTypeName", "Gazer4 播放列表");
                }

                // 注册打开命令
                using (var key = Registry.CurrentUser.CreateSubKey($@"SOFTWARE\Classes\{PROGRAM_ID}\shell\open\command"))
                {
                    key.SetValue("", $"\"{executablePath}\" \"%1\"");
                }

                // 注册图标（可选）
                using (var key = Registry.CurrentUser.CreateSubKey($@"SOFTWARE\Classes\{PROGRAM_ID}\DefaultIcon"))
                {
                    key.SetValue("", $"\"{executablePath}\",0");
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"注册文件关联失败: {ex.Message}\n可能需要管理员权限", "警告", 
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        // 检查是否已注册文件关联
        public static bool IsFileAssociationRegistered()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    return key?.GetValue("")?.ToString() == PROGRAM_ID;
                }
            }
            catch
            {
                return false;
            }
        }

        // 辅助方法
        private static string GetRelativePath(string fromPath, string toPath)
        {
            try
            {
                var fromUri = new Uri(fromPath + Path.DirectorySeparatorChar);
                var toUri = new Uri(toPath);
                var relative = fromUri.MakeRelativeUri(toUri);
                return Uri.UnescapeDataString(relative.ToString()).Replace('/', Path.DirectorySeparatorChar);
            }
            catch
            {
                return toPath; // 如果无法创建相对路径，返回绝对路径
            }
        }

        private static string ResolveImagePath(PlaylistItem item, string playlistDirectory)
        {
            // 优先尝试绝对路径
            if (File.Exists(item.FilePath))
                return item.FilePath;

            // 尝试相对路径
            if (!string.IsNullOrEmpty(item.RelativePath))
            {
                string relativePath = Path.Combine(playlistDirectory, item.RelativePath);
                if (File.Exists(relativePath))
                    return Path.GetFullPath(relativePath);
            }

            // 尝试仅文件名（在播放列表目录中查找）
            string fileName = Path.GetFileName(item.FilePath);
            string fileInPlaylistDir = Path.Combine(playlistDirectory, fileName);
            if (File.Exists(fileInPlaylistDir))
                return fileInPlaylistDir;

            return item.FilePath; // 找不到文件，返回原路径
        }

        private static PlaylistItemState ConvertImageStateToPlaylistState(MainWindow.ImageState imageState)
        {
            return new PlaylistItemState
            {
                ScaleX = imageState.ScaleX,
                ScaleY = imageState.ScaleY,
                TranslateX = imageState.TranslateX,
                TranslateY = imageState.TranslateY,
                BaseTranslateX = imageState.BaseTranslateX,
                BaseTranslateY = imageState.BaseTranslateY,
                EnableShake = imageState.EnableShake,
                ShakeAmount = imageState.ShakeAmount,
                ShakeFrequency = imageState.ShakeFrequency,
                EnablePulse = imageState.EnablePulse,
                PulseInterval = imageState.PulseInterval,
                PulseRandomness = imageState.PulseRandomness,
                PulseDamping = imageState.PulseDamping,
                PulsePowerX = imageState.PulsePowerX,
                PulsePowerY = imageState.PulsePowerY
            };
        }

        private static MainWindow.ImageState ConvertPlaylistStateToImageState(PlaylistItemState playlistState, MainWindow mainWindow)
        {
            return null; // 暂时返回null，我们会用新的方法
        }

        private static PlaylistItemState GetCurrentImageState(MainWindow mainWindow)
        {
            return mainWindow.GetCurrentPlaylistItemState();
        }

        private static void ApplyPlaylistSettings(PlaylistSettings settings, MainWindow mainWindow)
        {
            mainWindow.ApplyPlaylistSettings(settings);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ScoreManagerForSchool.Core.Logging;

namespace ScoreManagerForSchool.Core.Storage
{
    /// <summary>
    /// 可配置的关键积分等级系统
    /// </summary>
    public static class CriticalScoreLevels
    {
        private static List<CriticalScoreLevel>? _levels;
        private static readonly object _lock = new object();

        /// <summary>
        /// 默认的关键积分等级定义
        /// </summary>
        private static readonly List<CriticalScoreLevel> DefaultLevels = new()
        {
            new CriticalScoreLevel { Threshold = 6, Name = "轻度关注", Color = "#2196F3", DisplayOrder = 1 },   // 蓝色
            new CriticalScoreLevel { Threshold = 8, Name = "中度关注", Color = "#FF9800", DisplayOrder = 2 },   // 黄色
            new CriticalScoreLevel { Threshold = 16, Name = "重度关注", Color = "#FF5722", DisplayOrder = 3 },  // 橙色
            new CriticalScoreLevel { Threshold = 32, Name = "严重关注", Color = "#F44336", DisplayOrder = 4 }   // 红色
        };

        /// <summary>
        /// 获取关键积分等级配置
        /// </summary>
        public static List<CriticalScoreLevel> Levels
        {
            get
            {
                if (_levels == null)
                {
                    lock (_lock)
                    {
                        if (_levels == null)
                        {
                            _levels = LoadLevels();
                        }
                    }
                }
                return _levels;
            }
        }

        /// <summary>
        /// 从配置文件加载等级设置
        /// </summary>
        private static List<CriticalScoreLevel> LoadLevels()
        {
            try
            {
                var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "base");
                var configPath = Path.Combine(baseDir, "critical_score_levels.json");
                
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var levels = JsonSerializer.Deserialize<List<CriticalScoreLevel>>(json);
                    if (levels != null && levels.Count > 0)
                    {
                        return levels;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("加载关键积分等级配置失败，使用默认配置", "CriticalScoreLevels", ex);
            }

            return new List<CriticalScoreLevel>(DefaultLevels);
        }

        /// <summary>
        /// 保存等级设置到配置文件
        /// </summary>
        public static void SaveLevels(List<CriticalScoreLevel> levels)
        {
            try
            {
                var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "base");
                Directory.CreateDirectory(baseDir);
                var configPath = Path.Combine(baseDir, "critical_score_levels.json");
                
                var json = JsonSerializer.Serialize(levels, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                
                lock (_lock)
                {
                    _levels = new List<CriticalScoreLevel>(levels);
                }
                
                Logger.LogInfo("关键积分等级配置已保存", "CriticalScoreLevels");
            }
            catch (Exception ex)
            {
                Logger.LogError("保存关键积分等级配置失败", "CriticalScoreLevels", ex);
                throw;
            }
        }

        /// <summary>
        /// 重置为默认配置
        /// </summary>
        public static void ResetToDefault()
        {
            SaveLevels(new List<CriticalScoreLevel>(DefaultLevels));
        }

        /// <summary>
        /// 根据积分确定关键等级
        /// </summary>
        /// <param name="totalScore">学生总积分</param>
        /// <returns>关键等级，如果积分正常则返回null</returns>
        public static CriticalScoreLevel? GetCriticalLevel(double totalScore)
        {
            CriticalScoreLevel? criticalLevel = null;
            
            foreach (var level in Levels)
            {
                if (totalScore >= level.Threshold)
                {
                    // 找到最严重的等级（阈值最高的）
                    if (criticalLevel == null || level.Threshold > criticalLevel.Threshold)
                    {
                        criticalLevel = level;
                    }
                }
            }
            
            return criticalLevel;
        }

        /// <summary>
        /// 检查积分是否达到关键状态
        /// </summary>
        /// <param name="totalScore">学生总积分</param>
        /// <returns>是否达到关键状态</returns>
        public static bool IsCritical(double totalScore)
        {
            return GetCriticalLevel(totalScore) != null;
        }

        /// <summary>
        /// 获取最高关键阈值
        /// </summary>
        public static double GetHighestThreshold()
        {
            double highest = 0;
            foreach (var level in Levels)
            {
                if (level.Threshold > highest)
                {
                    highest = level.Threshold;
                }
            }
            return highest;
        }
    }

    /// <summary>
    /// 关键积分等级定义
    /// </summary>
    public class CriticalScoreLevel
    {
        /// <summary>
        /// 积分阈值（小于等于此值时触发该等级）
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// 等级名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示颜色（十六进制格式）
        /// </summary>
        public string Color { get; set; } = "#000000";

        /// <summary>
        /// 显示顺序（用于UI排序）
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// 获取颜色的描述文本
        /// </summary>
        public string ColorDescription => Name switch
        {
            "轻度关注" => "蓝色",
            "中度关注" => "黄色", 
            "重度关注" => "橙色",
            "严重关注" => "红色",
            _ => "未知"
        };
    }
}

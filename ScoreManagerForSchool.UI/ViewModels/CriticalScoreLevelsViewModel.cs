using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.Core.Logging;
using ScoreManagerForSchool.UI.Services;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class CriticalScoreLevelsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<CriticalScoreLevel> Levels { get; } = new();

        public ICommand AddLevelCommand { get; }
        public ICommand RemoveLevelCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand CancelCommand { get; }

        public Action? CloseWindow { get; set; }

        public CriticalScoreLevelsViewModel()
        {
            AddLevelCommand = new RelayCommand(_ => AddLevel());
            RemoveLevelCommand = new RelayCommand(p => RemoveLevel(p as CriticalScoreLevel));
            SaveCommand = new RelayCommand(_ => Save());
            ResetCommand = new RelayCommand(_ => Reset());
            CancelCommand = new RelayCommand(_ => Cancel());

            LoadLevels();
        }

        private void LoadLevels()
        {
            try
            {
                Levels.Clear();
                var levels = CriticalScoreLevels.Levels
                    .OrderBy(l => l.DisplayOrder)
                    .ToList();

                foreach (var level in levels)
                {
                    Levels.Add(new CriticalScoreLevel
                    {
                        Threshold = level.Threshold,
                        Name = level.Name,
                        Color = level.Color,
                        DisplayOrder = level.DisplayOrder
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("加载关键积分等级失败", "CriticalScoreLevelsViewModel", ex);
                ErrorHandler.HandleError(ex, "加载关键积分等级配置时发生错误", "CriticalScoreLevelsViewModel.LoadLevels");
            }
        }

        private void AddLevel()
        {
            try
            {
                var newLevel = new CriticalScoreLevel
                {
                    Threshold = 10,
                    Name = "新等级",
                    Color = "#808080",
                    DisplayOrder = Levels.Count + 1
                };

                Levels.Add(newLevel);
                Logger.LogInfo("添加新的关键积分等级", "CriticalScoreLevelsViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("添加关键积分等级失败", "CriticalScoreLevelsViewModel", ex);
                ErrorHandler.HandleError(ex, "添加新等级时发生错误", "CriticalScoreLevelsViewModel.AddLevel");
            }
        }

        private void RemoveLevel(CriticalScoreLevel? level)
        {
            if (level == null) return;

            try
            {
                Levels.Remove(level);
                
                // 重新排序
                for (int i = 0; i < Levels.Count; i++)
                {
                    Levels[i].DisplayOrder = i + 1;
                }

                Logger.LogInfo($"删除关键积分等级: {level.Name}", "CriticalScoreLevelsViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("删除关键积分等级失败", "CriticalScoreLevelsViewModel", ex);
                ErrorHandler.HandleError(ex, "删除等级时发生错误", "CriticalScoreLevelsViewModel.RemoveLevel");
            }
        }

        private void Save()
        {
            try
            {
                // 验证数据
                if (Levels.Count == 0)
                {
                    ErrorHandler.HandleError(new InvalidOperationException("至少需要一个等级"), "至少需要配置一个关键积分等级", "CriticalScoreLevelsViewModel.Save");
                    return;
                }

                foreach (var level in Levels)
                {
                    if (level.Threshold < 0)
                    {
                        ErrorHandler.HandleError(new ArgumentException($"等级 '{level.Name}' 的阈值不能为负数"), "阈值不能为负数", "CriticalScoreLevelsViewModel.Save");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(level.Name))
                    {
                        ErrorHandler.HandleError(new ArgumentException("等级名称不能为空"), "等级名称不能为空", "CriticalScoreLevelsViewModel.Save");
                        return;
                    }
                }

                // 检查重复阈值
                var duplicateThresholds = Levels.GroupBy(l => l.Threshold).Where(g => g.Count() > 1).ToList();
                if (duplicateThresholds.Any())
                {
                    ErrorHandler.HandleError(new ArgumentException("不能有重复的阈值"), "阈值不能重复", "CriticalScoreLevelsViewModel.Save");
                    return;
                }

                // 保存
                var levelsToSave = Levels.Select((level, index) => new CriticalScoreLevel
                {
                    Threshold = level.Threshold,
                    Name = level.Name,
                    Color = level.Color,
                    DisplayOrder = index + 1
                }).ToList();

                CriticalScoreLevels.SaveLevels(levelsToSave);

                Logger.LogInfo("关键积分等级配置已保存", "CriticalScoreLevelsViewModel");
                CloseWindow?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.LogError("保存关键积分等级失败", "CriticalScoreLevelsViewModel", ex);
                ErrorHandler.HandleError(ex, "保存配置时发生错误", "CriticalScoreLevelsViewModel.Save");
            }
        }

        private void Reset()
        {
            try
            {
                CriticalScoreLevels.ResetToDefault();
                LoadLevels();
                Logger.LogInfo("关键积分等级配置已重置为默认", "CriticalScoreLevelsViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("重置关键积分等级失败", "CriticalScoreLevelsViewModel", ex);
                ErrorHandler.HandleError(ex, "重置配置时发生错误", "CriticalScoreLevelsViewModel.Reset");
            }
        }

        private void Cancel()
        {
            CloseWindow?.Invoke();
        }
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.ComponentModel;

namespace ScoreManagerForSchool.UI.Views
{
    public enum ExportTimeRange
    {
        Today,
        Yesterday,
        ThisWeek,
        Custom
    }

    public class ExportDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private ExportTimeRange _selectedRange = ExportTimeRange.Today;
        public ExportTimeRange SelectedRange
        {
            get => _selectedRange;
            set
            {
                _selectedRange = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRange)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCustomDateVisible)));
                UpdateDateRange();
            }
        }

        private DateTime _startDate = DateTime.Today;
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartDate)));
            }
        }

        private DateTime _endDate = DateTime.Today.AddDays(1).AddSeconds(-1);
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndDate)));
            }
        }

        public bool IsCustomDateVisible => SelectedRange == ExportTimeRange.Custom;

        public string RangeDescription
        {
            get
            {
                return SelectedRange switch
                {
                    ExportTimeRange.Today => "今天的积分记录",
                    ExportTimeRange.Yesterday => "昨天的积分记录",
                    ExportTimeRange.ThisWeek => "本周的积分记录",
                    ExportTimeRange.Custom => $"自定义时间范围：{StartDate:yyyy-MM-dd} 到 {EndDate:yyyy-MM-dd}",
                    _ => "未知范围"
                };
            }
        }

        public ExportDialogViewModel()
        {
            UpdateDateRange();
        }

        private void UpdateDateRange()
        {
            var now = DateTime.Now;
            switch (SelectedRange)
            {
                case ExportTimeRange.Today:
                    StartDate = DateTime.Today;
                    EndDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                    break;
                case ExportTimeRange.Yesterday:
                    StartDate = DateTime.Today.AddDays(-1);
                    EndDate = DateTime.Today.AddSeconds(-1);
                    break;
                case ExportTimeRange.ThisWeek:
                    var daysFromMonday = (int)now.DayOfWeek - 1;
                    if (daysFromMonday < 0) daysFromMonday = 6; // Sunday = 6 days from Monday
                    StartDate = DateTime.Today.AddDays(-daysFromMonday);
                    EndDate = StartDate.AddDays(7).AddSeconds(-1);
                    break;
                case ExportTimeRange.Custom:
                    // Keep current values
                    break;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RangeDescription)));
        }

        public void OnRangeChanged(string rangeValue)
        {
            if (Enum.TryParse<ExportTimeRange>(rangeValue, out var range))
            {
                SelectedRange = range;
            }
        }
    }

    public partial class ExportScoreDialog : Window
    {
        public ExportDialogViewModel ViewModel { get; }
        public bool IsConfirmed { get; private set; }

        public ExportScoreDialog()
        {
            ViewModel = new ExportDialogViewModel();
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnExportClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            IsConfirmed = true;
            Close();
        }

        private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            IsConfirmed = false;
            Close();
        }

        private void OnRangeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                ViewModel.OnRangeChanged(tag);
            }
        }
    }
}

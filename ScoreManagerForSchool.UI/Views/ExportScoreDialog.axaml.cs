using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartDateOnly)));
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndDateOnly)));
            }
        }

        // DatePicker兼容的DateOnly属性，避免转换错误
        public DateOnly StartDateOnly
        {
            get => DateOnly.FromDateTime(_startDate);
            set 
            { 
                _startDate = value.ToDateTime(TimeOnly.MinValue);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartDate)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartDateOnly)));
            }
        }

        public DateOnly EndDateOnly
        {
            get => DateOnly.FromDateTime(_endDate);
            set 
            { 
                _endDate = value.ToDateTime(new TimeOnly(23, 59, 59));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndDate)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndDateOnly)));
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartDateOnly)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EndDateOnly)));
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
        
        private bool _suppressStartSelectedDateClose = false;
        private bool _suppressEndSelectedDateClose = false;

        public ExportScoreDialog()
        {
            ViewModel = new ExportDialogViewModel();
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            // 设置日历事件处理
            var startCalendar = this.FindControl<Calendar>("StartDateCalendar");
            var endCalendar = this.FindControl<Calendar>("EndDateCalendar");
            
            if (startCalendar != null)
            {
                startCalendar.PropertyChanged += OnStartCalendarPropertyChanged;
            }
            
            if (endCalendar != null)
            {
                endCalendar.PropertyChanged += OnEndCalendarPropertyChanged;
            }
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
        
        // 日期选择器事件处理
        private void OnStartDateButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("StartDatePopup");
            var endPopup = this.FindControl<Popup>("EndDatePopup");
            
            // 关闭其他弹窗
            if (endPopup != null)
            {
                endPopup.IsOpen = false;
            }
            
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
                
                // 如果打开了弹窗，设置Calendar的初始选中日期
                if (popup.IsOpen)
                {
                    var calendar = this.FindControl<Calendar>("StartDateCalendar");
                    if (calendar != null)
                    {
                        _suppressStartSelectedDateClose = true;
                        try
                        {
                            calendar.SelectedDate = ViewModel.StartDate;
                        }
                        finally
                        {
                            _suppressStartSelectedDateClose = false;
                        }
                    }
                }
            }
        }

        private void OnEndDateButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var popup = this.FindControl<Popup>("EndDatePopup");
            var startPopup = this.FindControl<Popup>("StartDatePopup");
            
            // 关闭其他弹窗
            if (startPopup != null)
            {
                startPopup.IsOpen = false;
            }
            
            if (popup != null)
            {
                popup.IsOpen = !popup.IsOpen;
                
                // 如果打开了弹窗，设置Calendar的初始选中日期
                if (popup.IsOpen)
                {
                    var calendar = this.FindControl<Calendar>("EndDateCalendar");
                    if (calendar != null)
                    {
                        _suppressEndSelectedDateClose = true;
                        try
                        {
                            calendar.SelectedDate = ViewModel.EndDate;
                        }
                        finally
                        {
                            _suppressEndSelectedDateClose = false;
                        }
                    }
                }
            }
        }

        private void OnStartCalendarPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if ((e.Property == Calendar.SelectedDateProperty || e.Property?.Name == "SelectedDate") && sender is Calendar calendar)
            {
                if (_suppressStartSelectedDateClose)
                {
                    return;
                }
                if (calendar.SelectedDate.HasValue)
                {
                    // 直接设置 ViewModel 的 StartDate 属性
                    var selectedDate = calendar.SelectedDate.Value;
                    ViewModel.StartDate = selectedDate;
                    
                    // 延迟关闭弹窗
                    System.Threading.Tasks.Task.Delay(150).ContinueWith(_ =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            var popup = this.FindControl<Popup>("StartDatePopup");
                            if (popup != null)
                            {
                                popup.IsOpen = false;
                            }
                        });
                    });
                }
            }
        }

        private void OnEndCalendarPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if ((e.Property == Calendar.SelectedDateProperty || e.Property?.Name == "SelectedDate") && sender is Calendar calendar)
            {
                if (_suppressEndSelectedDateClose)
                {
                    return;
                }
                if (calendar.SelectedDate.HasValue)
                {
                    // 直接设置 ViewModel 的 EndDate 属性
                    var selectedDate = calendar.SelectedDate.Value;
                    ViewModel.EndDate = selectedDate.AddHours(23).AddMinutes(59).AddSeconds(59); // 设置为当天结束时间
                    
                    // 延迟关闭弹窗
                    System.Threading.Tasks.Task.Delay(150).ContinueWith(_ =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            var popup = this.FindControl<Popup>("EndDatePopup");
                            if (popup != null)
                            {
                                popup.IsOpen = false;
                            }
                        });
                    });
                }
            }
        }
    }
}

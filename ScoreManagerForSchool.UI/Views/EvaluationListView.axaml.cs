using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.UI.ViewModels;
using System;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class EvaluationListView : UserControl
    {
    private bool _suppressStartSelectedDateClose;
    private bool _suppressEndSelectedDateClose;

        public EvaluationListView()
        {
            InitializeComponent();
            if (DataContext == null)
            {
                DataContext = new EvaluationListViewModel();
            }
            
            // 延迟设置Calendar的事件监听
            this.Loaded += OnViewLoaded;
        }
        
        private void OnViewLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 为Calendar控件添加属性变化监听
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
        
        private void OnStartCalendarPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if ((e.Property == Calendar.SelectedDateProperty || e.Property?.Name == "SelectedDate") && sender is Calendar calendar)
            {
                if (_suppressStartSelectedDateClose)
                {
                    return;
                }
                if (calendar.SelectedDate.HasValue && DataContext is EvaluationListViewModel vm)
                {
                    // 使用与快捷时间按钮相同的逻辑直接设置
                    var selectedDate = calendar.SelectedDate.Value;
                    vm.StartDate = new DateTimeOffset(selectedDate);
                    
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
        }        private void OnEndCalendarPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if ((e.Property == Calendar.SelectedDateProperty || e.Property?.Name == "SelectedDate") && sender is Calendar calendar)
            {
                if (_suppressEndSelectedDateClose)
                {
                    return;
                }
                if (calendar.SelectedDate.HasValue && DataContext is EvaluationListViewModel vm)
                {
                    // 使用与快捷时间按钮相同的逻辑直接设置
                    var selectedDate = calendar.SelectedDate.Value;
                    vm.EndDate = new DateTimeOffset(selectedDate);
                    
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

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnSetToday(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EvaluationListViewModel vm)
            {
                var today = DateTime.Today;
                vm.StartDate = new DateTimeOffset(today);
                vm.EndDate = new DateTimeOffset(today);
            }
        }

        private void OnSetYesterday(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EvaluationListViewModel vm)
            {
                var yesterday = DateTime.Today.AddDays(-1);
                vm.StartDate = new DateTimeOffset(yesterday);
                vm.EndDate = new DateTimeOffset(yesterday);
            }
        }

        private void OnSetThisWeek(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EvaluationListViewModel vm)
            {
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1); // 周一为起始
                var endOfWeek = startOfWeek.AddDays(6); // 周日为结束
                vm.StartDate = new DateTimeOffset(startOfWeek);
                vm.EndDate = new DateTimeOffset(endOfWeek);
            }
        }

        private void OnSetThisMonth(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EvaluationListViewModel vm)
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                vm.StartDate = new DateTimeOffset(startOfMonth);
                vm.EndDate = new DateTimeOffset(endOfMonth);
            }
        }

        private void OnClearDates(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EvaluationListViewModel vm)
            {
                vm.StartDate = null;
                vm.EndDate = null;
            }
        }

        // 日历式日期选择事件处理
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
                    if (calendar != null && DataContext is EvaluationListViewModel vm)
                    {
                        _suppressStartSelectedDateClose = true;
                        try
                        {
                            calendar.SelectedDate = vm.StartDate?.DateTime ?? DateTime.Today;
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
                    if (calendar != null && DataContext is EvaluationListViewModel vm)
                    {
                        _suppressEndSelectedDateClose = true;
                        try
                        {
                            calendar.SelectedDate = vm.EndDate?.DateTime ?? DateTime.Today;
                        }
                        finally
                        {
                            _suppressEndSelectedDateClose = false;
                        }
                    }
                }
            }
        }
    }
}

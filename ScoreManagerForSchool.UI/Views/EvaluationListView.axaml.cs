using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.UI.ViewModels;
using System;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class EvaluationListView : UserControl
    {
        public EvaluationListView()
        {
            InitializeComponent();
            if (DataContext == null)
            {
                DataContext = new EvaluationListViewModel();
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
                vm.StartDate = today;
                vm.EndDate = today;
            }
        }

        private void OnSetYesterday(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EvaluationListViewModel vm)
            {
                var yesterday = DateTime.Today.AddDays(-1);
                vm.StartDate = yesterday;
                vm.EndDate = yesterday;
            }
        }

        private void OnSetThisWeek(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EvaluationListViewModel vm)
            {
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1); // 周一为起始
                var endOfWeek = startOfWeek.AddDays(6); // 周日为结束
                vm.StartDate = startOfWeek;
                vm.EndDate = endOfWeek;
            }
        }

        private void OnSetThisMonth(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is EvaluationListViewModel vm)
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
                vm.StartDate = startOfMonth;
                vm.EndDate = endOfMonth;
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
    }
}

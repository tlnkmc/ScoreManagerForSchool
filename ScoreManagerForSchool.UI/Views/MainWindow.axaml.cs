using Avalonia.Controls;
using System;
using System.Collections.Generic;
using ScoreManagerForSchool.UI.ViewModels;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.UI.Views;
using ScoreManagerForSchool.Core.Logging;
using ScoreManagerForSchool.UI.Services;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Logger.LogInfo("MainWindow 初始化开始", "MainWindow");
                
                var vm = this.DataContext as MainWindowViewModel ?? new MainWindowViewModel();
                this.DataContext = vm;
                
                Logger.LogInfo("MainWindow 构造完成", "MainWindow");
                
                // sidebar initialization moved to MainWindow_Opened to avoid being overridden by App
                vm.PropertyChanged += (s, e) =>
                {
                    try
                    {
                        if (e.PropertyName == nameof(MainWindowViewModel.SelectedPage))
                        {
                            UpdatePage(vm.SelectedPage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("PropertyChanged 事件处理失败", "MainWindow", ex);
                        ErrorHandler.HandleError(ex, "页面切换时发生错误", "MainWindow.PropertyChanged");
                    }
                };
                this.Opened += MainWindow_Opened;
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow 构造失败", "MainWindow", ex);
                ErrorHandler.HandleError(ex, "主窗口初始化失败", "MainWindow.Constructor");
                throw;
            }
        }

        private void MainWindow_Opened(object? sender, System.EventArgs e)
        {
            try
            {
                Logger.LogInfo("MainWindow_Opened 开始", "MainWindow");
                
                if (this.DataContext is MainWindowViewModel vm)
                {
                    // set sidebar items now that App likely finished setting DataContext
                    var sidebarItems = new List<string> { "首页", "信息录入", "积分统计", "积分记录管理", "学生列表管理", "教师和班级管理", "班级及评价方案管理", "设置", "关于" };
                    vm.GetType().GetProperty("SidebarItems")?.SetValue(vm, sidebarItems);
                    
                    try
                    {
                        var lb = this.FindControl<Avalonia.Controls.ListBox>("SidebarList");
                        if (lb != null)
                        {
                            lb.ItemsSource = sidebarItems;
                            Logger.LogInfo($"侧边栏项目已设置，共 {sidebarItems.Count} 项", "MainWindow");
                            
                            // ensure ListBox selection follows VM
                            lb.SelectedItem = vm.SelectedPage;
                            
                            // add selection changed handler
                            lb.SelectionChanged += (ss, ee) =>
                            {
                                try
                                {
                                    Logger.LogDebug($"ListBox.SelectionChanged selected: {lb.SelectedItem}", "MainWindow");
                                    if (this.DataContext is MainWindowViewModel mvm && lb.SelectedItem is string s) 
                                    {
                                        mvm.SelectedPage = s;
                                    }
                                }
                                catch (Exception selectionEx)
                                {
                                    Logger.LogError("侧边栏选择处理失败", "MainWindow", selectionEx);
                                }
                            };
                            
                            if (string.IsNullOrEmpty(vm.SelectedPage) && sidebarItems.Count > 0)
                            {
                                vm.SelectedPage = sidebarItems[0];
                                Logger.LogInfo($"SelectedPage 为空，设置为第一项: {sidebarItems[0]}", "MainWindow");
                            }
                        }
                        else
                        {
                            Logger.LogWarning("未找到 SidebarList 控件", "MainWindow");
                        }
                    }
                    catch (Exception sidebarEx)
                    {
                        Logger.LogError("设置侧边栏失败", "MainWindow", sidebarEx);
                        ErrorHandler.HandleError(sidebarEx, "侧边栏初始化失败", "MainWindow.Sidebar");
                    }

                    UpdatePage(vm.SelectedPage);
                }
                
                Logger.LogInfo("MainWindow_Opened 完成", "MainWindow");
            }
            catch (Exception ex)
            {
                Logger.LogError("MainWindow_Opened 失败", "MainWindow", ex);
                ErrorHandler.HandleError(ex, "主窗口打开时发生错误", "MainWindow.Opened");
            }
        }

        private void UpdatePage(string pageName)
        {
            try
            {
                var host = this.FindControl<ContentControl>("PageHost");
                if (host == null) 
                {
                    Logger.LogWarning("未找到 PageHost 控件", "MainWindow.UpdatePage");
                    return;
                }
                
                Logger.LogInfo($"切换页面: {pageName}", "MainWindow.UpdatePage");
                
                switch (pageName)
                {
                    case "首页":
                        host.Content = new HomeView();
                        break;
                    case "信息录入":
                        host.Content = new InfoEntryView();
                        break;
                    case "积分统计":
                        host.Content = new StatsView();
                        break;
                    case "积分记录管理":
                        host.Content = new EvaluationListView();
                        break;
                    case "学生列表管理":
                        host.Content = new StudentsListView();
                        break;
                    case "教师和班级管理":
                        host.Content = new TeacherManagementView();
                        break;
                    case "班级及评价方案管理":
                        host.Content = new SchemeManagementView();
                        break;
                    case "设置":
                        host.Content = new SettingsView();
                        break;
                    case "关于":
                        host.Content = new AboutView();
                        break;
                    default:
                        Logger.LogWarning($"未知页面: {pageName}", "MainWindow.UpdatePage");
                        host.Content = new TextBlock { Text = pageName };
                        break;
                }
                
                Logger.LogDebug($"页面切换完成: {pageName}", "MainWindow.UpdatePage");
            }
            catch (Exception ex)
            {
                Logger.LogError($"页面切换失败: {pageName}", "MainWindow.UpdatePage", ex);
                ErrorHandler.HandleError(ex, $"切换到页面 '{pageName}' 时发生错误", "MainWindow.UpdatePage");
            }
        }

        // click handler for DataTemplate buttons in the Sidebar ListBox
        private void OnSidebarButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Avalonia.Controls.Button;
                if (btn?.Content is Avalonia.Controls.TextBlock tb)
                {
                    var text = tb.Text ?? string.Empty;
                    Logger.LogInfo($"侧边栏按钮点击: {text}", "MainWindow.OnSidebarButtonClick");
                    
                    // immediate visible feedback: set PageHost to a highlighted message so user sees response
                    try
                    {
                        var host = this.FindControl<ContentControl>("PageHost");
                        if (host != null)
                        {
                            host.Content = new TextBlock 
                            { 
                                Text = $"正在加载 {text}...", 
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                            };
                        }
                    }
                    catch (Exception feedbackEx)
                    {
                        Logger.LogError("设置页面加载反馈失败", "MainWindow.OnSidebarButtonClick", feedbackEx);
                    }
                    
                    // propagate to VM and call UpdatePage to load real content
                    if (this.DataContext is MainWindowViewModel vm)
                    {
                        vm.SelectedPage = text;
                        try { UpdatePage(text); } catch (Exception updateEx)
                        {
                            Logger.LogError("更新页面失败", "MainWindow.OnSidebarButtonClick", updateEx);
                            ErrorHandler.HandleError(updateEx, $"加载页面 '{text}' 失败", "MainWindow.OnSidebarButtonClick");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("侧边栏按钮点击处理失败", "MainWindow.OnSidebarButtonClick", ex);
                ErrorHandler.HandleError(ex, "侧边栏按钮响应失败", "MainWindow.OnSidebarButtonClick");
            }
        }
    }
}

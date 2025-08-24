namespace ScoreManagerForSchool.UI.ViewModels;

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Input;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Greeting { get; } = "Welcome to ScoreManagerForSchool";

    private List<string> _sidebarItems = new List<string>();
    public List<string> SidebarItems { get => _sidebarItems; set { _sidebarItems = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SidebarItems))); } }

    private string _selectedPage = "首页";
    public string SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (_selectedPage == value) return;
            _selectedPage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPage)));
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "smfs_mainwindow.log"), DateTime.UtcNow.ToString("o") + " VM.SelectedPage changed to=" + (_selectedPage ?? "(null)") + "\n"); } catch { }
        }
    }

    public ICommand ShowInfoCommand { get; }
    public ICommand ShowStudentsCommand { get; }
    public ICommand ShowStatsCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand ShowAboutCommand { get; }
    public ICommand SidebarItemCommand { get; }

    public MainWindowViewModel()
    {
        ShowInfoCommand = new RelayCommand(_ => SelectedPage = "信息录入");
        ShowStudentsCommand = new RelayCommand(_ => SelectedPage = "学生管理");
        ShowStatsCommand = new RelayCommand(_ => SelectedPage = "统计与报表");
        ShowSettingsCommand = new RelayCommand(_ => SelectedPage = "设置");
        ShowAboutCommand = new RelayCommand(_ => SelectedPage = "关于");
    SidebarItemCommand = new RelayCommand(p => { if (p is string s) SelectedPage = s; });
    }
}

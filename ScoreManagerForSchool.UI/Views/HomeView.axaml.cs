using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.UI.ViewModels;
using Avalonia;

namespace ScoreManagerForSchool.UI.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        if (this.DataContext is not HomeViewModel)
            this.DataContext = new HomeViewModel();
    }

    private void NavigateTo(string page)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is Views.MainWindow mw && mw.DataContext is MainWindowViewModel mvm)
        {
            mvm.SelectedPage = page;
            return;
        }
        if (top is Window w && w.DataContext is MainWindowViewModel mvm2)
        {
            mvm2.SelectedPage = page;
        }
    }

    // quick entry handlers removed with UI

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

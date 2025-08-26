using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.UI.ViewModels;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class CriticalScoreLevelsWindow : Window
    {
        public CriticalScoreLevelsWindow()
        {
            InitializeComponent();
            
            // 创建并设置ViewModel
            var viewModel = new CriticalScoreLevelsViewModel();
            DataContext = viewModel;
            viewModel.CloseWindow = () => Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

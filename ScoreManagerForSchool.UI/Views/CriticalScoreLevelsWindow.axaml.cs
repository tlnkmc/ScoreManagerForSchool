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
            
            if (DataContext is CriticalScoreLevelsViewModel vm)
            {
                vm.CloseWindow = () => Close();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

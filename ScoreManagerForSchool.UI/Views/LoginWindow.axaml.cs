using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ScoreManagerForSchool.UI.ViewModels;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            if (DataContext == null) DataContext = new LoginViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class ClassEvaluationView : UserControl
    {
        public ClassEvaluationView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
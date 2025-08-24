using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ScoreManagerForSchool.UI.Controls
{
    public partial class PasswordTextBox : UserControl
    {
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<PasswordTextBox, string>(nameof(Text), defaultValue: string.Empty);

        public static readonly StyledProperty<string> WatermarkProperty =
            AvaloniaProperty.Register<PasswordTextBox, string>(nameof(Watermark), defaultValue: string.Empty);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        private bool _isPasswordVisible = false;

        public PasswordTextBox()
        {
            InitializeComponent();
        }

        private void TogglePasswordVisibility(object? sender, RoutedEventArgs e)
        {
            var passwordTextBox = this.FindControl<TextBox>("InnerPasswordTextBox");
            var eyeIcon = this.FindControl<TextBlock>("EyeIcon");
            
            if (passwordTextBox != null && eyeIcon != null)
            {
                _isPasswordVisible = !_isPasswordVisible;
                
                if (_isPasswordVisible)
                {
                    // 显示明文
                    passwordTextBox.PasswordChar = '\0';
                    eyeIcon.Text = "🙈"; // 闭眼图标，表示当前是明文状态
                }
                else
                {
                    // 显示密码
                    passwordTextBox.PasswordChar = '●';
                    eyeIcon.Text = "👁"; // 睁眼图标，表示当前是密码状态
                }
            }
        }
    }
}

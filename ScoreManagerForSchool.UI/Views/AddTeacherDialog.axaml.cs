using Avalonia.Controls;
using Avalonia.Interactivity;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.Core.Logging;
using System;

namespace ScoreManagerForSchool.UI.Views
{
    public partial class AddTeacherDialog : Window
    {
        public Teacher? Result { get; private set; }

        public AddTeacherDialog()
        {
            InitializeComponent();
            
            // 生成默认工号
            IdTextBox.Text = $"T{DateTime.Now:yyyyMMddHHmmss}";
            
            // 设置焦点到姓名输入框
            this.Opened += (s, e) => NameTextBox.Focus();
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    ShowError("请输入教师姓名");
                    NameTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(IdTextBox.Text))
                {
                    ShowError("请输入教师工号");
                    IdTextBox.Focus();
                    return;
                }

                // 创建教师对象
                var teacher = new Teacher
                {
                    Id = IdTextBox.Text.Trim(),
                    Name = NameTextBox.Text.Trim(),
                    Subject = SubjectTextBox.Text?.Trim() ?? "",
                    SubjectGroup = SubjectGroupTextBox.Text?.Trim() ?? "",
                    Classes = new System.Collections.Generic.List<string>()
                };

                // 处理班级列表
                if (!string.IsNullOrWhiteSpace(ClassesTextBox.Text))
                {
                    var classes = ClassesTextBox.Text.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var cls in classes)
                    {
                        var trimmed = cls.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            teacher.Classes.Add(trimmed);
                        }
                    }
                }

                // 生成拼音
                if (!string.IsNullOrEmpty(teacher.Name))
                {
                    var (pinyin, initials) = PinyinUtil.MakeKeys(teacher.Name);
                    teacher.NamePinyin = pinyin;
                    teacher.NamePinyinInitials = initials;
                }

                Result = teacher;
                Close(true);
            }
            catch (Exception ex)
            {
                Logger.LogError("创建教师失败", "AddTeacherDialog.OnOkClick", ex);
                ShowError($"创建教师失败：{ex.Message}");
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Result = null;
            Close(false);
        }

        private async void ShowError(string message)
        {
            var messageBox = new Window
            {
                Title = "输入错误",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 16
            };

            content.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            });

            var okButton = new Button
            {
                Content = "确定",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                MinWidth = 80
            };

            okButton.Click += (s, e) => messageBox.Close();
            content.Children.Add(okButton);

            messageBox.Content = content;

            if (Owner is Window owner)
            {
                await messageBox.ShowDialog(owner);
            }
            else
            {
                messageBox.Show();
            }
        }
    }
}

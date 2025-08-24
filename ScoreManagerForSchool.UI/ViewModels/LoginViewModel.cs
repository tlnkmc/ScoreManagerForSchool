using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.Core.Security;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;
        private Database1Model? _db;

        public string UserPassword { get; set; } = string.Empty;

        private string? _userHint;
        public string? UserHint { get => _userHint; set { _userHint = value; PropertyChanged?.Invoke(this, new(nameof(UserHint))); } }

        private string? _message;
        public string? Message { get => _message; set { _message = value; PropertyChanged?.Invoke(this, new(nameof(Message))); } }

        public ICommand NextCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ShowHintCommand { get; }
        public ICommand StartRecoverCommand { get; }
        public ICommand VerifyRecoveryCommand { get; }

        public Action<bool>? CloseAction { get; set; }

        private static readonly string Prefix = "0D0007211145141919810";

        public LoginViewModel(string? baseDir = null)
        {
            _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
            _db = new Database1Store(_baseDir).Load();
            var hints = new PwhintStore(_baseDir).LoadHints();
            UserHint = hints.Count > 0 ? hints[0] : null;
            NextCommand = new RelayCommand(_ => Next());
            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke(false));
            ShowHintCommand = new RelayCommand(_ => ShowHint());
            StartRecoverCommand = new RelayCommand(_ => StartRecover());
            VerifyRecoveryCommand = new RelayCommand(_ => VerifyRecovery());
        }

        private bool _userTriedOnce = false;

        private void Next()
        {
            if (!TryVerifyUser(UserPassword))
            {
                Message = "用户密码不正确。" + (!_userTriedOnce && !string.IsNullOrEmpty(UserHint) ? $" 提示：{UserHint}" : string.Empty);
                _userTriedOnce = true;
                return;
            }
            CloseAction?.Invoke(true);
        }

        private bool TryVerifyUser(string pwd)
        {
            try
            {
                if (_db == null || string.IsNullOrEmpty(_db.ID1) || string.IsNullOrEmpty(_db.Salt1)) return false;
                var salt1 = Convert.FromBase64String(_db.Salt1);
                var key1 = CryptoUtil.DeriveKey(pwd, salt1, 32, _db.Iterations > 0 ? _db.Iterations : 100000);
                var plain = CryptoUtil.DecryptFromBase64(_db.ID1, key1);
                return !string.IsNullOrEmpty(plain) && plain.StartsWith(Prefix, StringComparison.Ordinal);
            }
            catch { return false; }
        }

        private static string BuildPayload()
        {
            // 前缀 + 235位大写十六进制随机
            var bytes = new byte[118]; // 236 hex chars; we'll trim to 235
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var hex = BitConverter.ToString(bytes).Replace("-", string.Empty).ToUpperInvariant();
            if (hex.Length > 235) hex = hex.Substring(0, 235);
            return Prefix + hex;
        }

        private void ShowHint()
        {
            if (!string.IsNullOrEmpty(UserHint))
                Message = $"提示：{UserHint}";
            else
                Message = "暂无提示信息";
        }

        // Recovery UI state
        private bool _showRecovery;
        public bool ShowRecovery { get => _showRecovery; set { _showRecovery = value; PropertyChanged?.Invoke(this, new(nameof(ShowRecovery))); } }
        public string? RecoveryQuestion1 { get; set; }
        public string? RecoveryAnswer1 { get; set; }
        public string? RecoveryQuestion2 { get; set; }
        public string? RecoveryAnswer2 { get; set; }
        public string? NewPassword1 { get; set; }
        public string? NewPassword2 { get; set; }

        private void StartRecover()
        {
            try
            {
                var store = new ScoreManagerForSchool.Core.Storage.SecurityQuestionsStore(_baseDir);
                var model = store.Load();
                if (model.Items.Count == 0) { Message = "未设置密保问题，无法找回。"; return; }
                // bind first two
                RecoveryQuestion1 = model.Items.Count > 0 ? model.Items[0].Question : null;
                RecoveryQuestion2 = model.Items.Count > 1 ? model.Items[1].Question : null;
                PropertyChanged?.Invoke(this, new(nameof(RecoveryQuestion1)));
                PropertyChanged?.Invoke(this, new(nameof(RecoveryQuestion2)));
                ShowRecovery = true;
                Message = "请回答密保问题并设置新密码。";
            }
            catch { Message = "找回失败。"; }
        }

        private void VerifyRecovery()
        {
            try
            {
                var store = new ScoreManagerForSchool.Core.Storage.SecurityQuestionsStore(_baseDir);
                var ok = store.VerifyAnswers(new (string, string)[] {
                    (RecoveryQuestion1 ?? string.Empty, RecoveryAnswer1 ?? string.Empty),
                    (RecoveryQuestion2 ?? string.Empty, RecoveryAnswer2 ?? string.Empty)
                });
                if (!ok) { Message = "答案不正确。"; return; }
                if (string.IsNullOrWhiteSpace(NewPassword1) || NewPassword1 != NewPassword2 || NewPassword1!.Length < 8) { Message = "新密码无效或不匹配。"; return; }
                TryResetUserPassword(NewPassword1!);
                Message = "密码已重置，请使用新密码登录。";
                ShowRecovery = false;
            }
            catch { Message = "操作失败。"; }
        }

        private void TryResetUserPassword(string newPwd)
        {
            try
            {
                if (_db == null) return;
                var rawSalt1 = Guid.NewGuid().ToByteArray();
                var key1 = CryptoUtil.DeriveKey(newPwd, rawSalt1, 32, _db.Iterations > 0 ? _db.Iterations : 100000);
                var id1 = CryptoUtil.EncryptToBase64(BuildPayload(), key1);
                _db.Salt1 = Convert.ToBase64String(rawSalt1);
                _db.ID1 = id1;
                new Database1Store(_baseDir).Save(_db);
            }
            catch { }
        }
    }
}

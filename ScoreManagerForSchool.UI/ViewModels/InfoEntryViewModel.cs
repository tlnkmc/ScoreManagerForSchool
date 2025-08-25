using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using ScoreManagerForSchool.Core.Storage;
using ScoreManagerForSchool.Core.Logging;
using ScoreManagerForSchool.UI.Services;
using NPinyin;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class InfoEntryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;
        private readonly List<Student> _students;
        private readonly EnhancedEvaluationService _evaluationService;
    private List<Student>? _multiChosen;

        public InfoEntryViewModel(string? baseDir = null)
        {
            try
            {
                Logger.LogInfo("InfoEntryViewModel 初始化开始", "InfoEntryViewModel");
                
                _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
                _students = new StudentStore(_baseDir).Load();
                _evaluationService = new EnhancedEvaluationService(_baseDir);
                
                ClassOptions = new ObservableCollection<string>();
                foreach (var c in _students.Select(s => (s.Class ?? string.Empty).Trim()).Where(s => s.Length > 0).Distinct())
                    ClassOptions.Add(c);

                // 设置默认积分为2分
                ScoreInput = "2";

                NextCommand = new RelayCommand(async _ => await NextAsync());
                SkipCommand = new RelayCommand(_ => { Advance(); AutoFillCurrent(); });
                AddToPendingCommand = new RelayCommand(_ => AddToPending());
                ProcessPendingCommand = new RelayCommand(p => ProcessPending(p as EvaluationEntry));
                DeletePendingCommand = new RelayCommand(p => DeletePending(p as EvaluationEntry));
                
                LoadPendingItems();
                
                Logger.LogInfo($"InfoEntryViewModel 初始化完成，加载 {_students.Count} 名学生", "InfoEntryViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("InfoEntryViewModel 初始化失败", "InfoEntryViewModel", ex);
                ErrorHandler.HandleError(ex, "信息录入模块初始化失败", "InfoEntryViewModel.Constructor");
                throw;
            }
        }

        // Callback set by View to allow showing a selection dialog when multiple candidates
    public Func<List<Student>, string, Task<Student?>>? SelectStudentAsync { get; set; }
    public Func<List<Student>, string, Task<List<Student>?>>? SelectMultiAsync { get; set; }

        private string? _pasteText;
        public string? PasteText
        {
            get => _pasteText; 
            set { _pasteText = value; ParseLines(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PasteText))); }
        }

        private List<string> _lines = new();
        private int _index = 0;
        public string? CurrentLineText => (_index >= 0 && _index < _lines.Count) ? _lines[_index] : null;

        private string? _classInput;
    public string? ClassInput { get => _classInput; set { _classInput = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClassInput))); } }

    private string? _studentIdInput;
    public string? StudentIdInput { get => _studentIdInput; set { _studentIdInput = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StudentIdInput))); OnIdOrNameChanged(); } }
        private string? _nameInput;
    public string? NameInput { get => _nameInput; set { _nameInput = value; UpdateClassOptions(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameInput))); OnIdOrNameChanged(); } }

        private string? _reasonInput;
        public string? ReasonInput { get => _reasonInput; set { _reasonInput = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReasonInput))); } }

        private string? _scoreInput;
        public string? ScoreInput { get => _scoreInput; set { _scoreInput = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScoreInput))); } }

        // 新增教师相关属性
        private string? _teacherInput;
        public string? TeacherInput { get => _teacherInput; set { _teacherInput = value; OnTeacherInputChanged(); PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TeacherInput))); } }

        private string? _matchedTeacherInfo;
        public string? MatchedTeacherInfo { get => _matchedTeacherInfo; set { _matchedTeacherInfo = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MatchedTeacherInfo))); } }

        public ObservableCollection<string> ClassOptions { get; }
        public ObservableCollection<EvaluationEntry> PendingItems { get; } = new();

        public ICommand NextCommand { get; }
        public ICommand SkipCommand { get; }
        public ICommand AddToPendingCommand { get; }
        public ICommand ProcessPendingCommand { get; }
        public ICommand DeletePendingCommand { get; }

        private void ParseLines()
        {
            _lines = (_pasteText ?? string.Empty)
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
            _index = 0;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLineText)));
            AutoFillCurrent();
        }

        private void Advance()
        {
            // remove current line once processed so that it auto disappears from待录入
            if (_index >= 0 && _index < _lines.Count)
            {
                _lines.RemoveAt(_index);
                if (_index >= _lines.Count) _index = _lines.Count - 1;
            }
            else
                {
                    _index = Math.Min(_index + 1, Math.Max(0, _lines.Count));
            }
            // sync remaining lines back to paste box without re-parsing
            _pasteText = string.Join(Environment.NewLine, _lines);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PasteText)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLineText)));
            // clear class/name/reason, keep score for next entry
            ClassInput = string.Empty;
            NameInput = string.Empty;
            ReasonInput = string.Empty;
        }

        private void UpdateClassOptions()
        {
            ClassOptions.Clear();
            foreach (var c in _students.Select(s => (s.Class ?? string.Empty).Trim()).Where(s => s.Length > 0).Distinct())
                ClassOptions.Add(c);
            var st = MatchStudentByIdOrName(StudentIdInput, NameInput);
            if (st != null && !string.IsNullOrWhiteSpace(st.Class))
                ClassInput = st.Class;
        }

        private void OnTeacherInputChanged()
        {
            if (string.IsNullOrWhiteSpace(TeacherInput))
            {
                MatchedTeacherInfo = null;
                return;
            }

            var teacher = _evaluationService.MatchTeacher(TeacherInput, ClassInput);
            if (teacher != null)
            {
                MatchedTeacherInfo = $"{teacher.Name} ({teacher.Subject} - {teacher.SubjectGroup})";
            }
            else
            {
                MatchedTeacherInfo = "未找到匹配的教师";
            }
        }

        private Student? MatchStudentByIdOrName(string? id, string? name)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                var st = _students.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
                if (st != null) return st;
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                var exact = _students.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;

                // Normalize input for pinyin matching
                var asciiLetters = LettersOnlyLower(name);
                bool inputIsAscii = asciiLetters.Length > 0 && asciiLetters.Length == name.Count(ch => char.IsLetter(ch));

                // If user typed ASCII, try full pinyin match then initials (prefer预计算字段)
                if (inputIsAscii)
                {
                    var inputFull = asciiLetters; // full pinyin without spaces
                    var byFull = _students.FirstOrDefault(s => (s.NamePinyin ?? string.Empty).Equals(inputFull, StringComparison.OrdinalIgnoreCase)
                                                             || ToFullPinyinKey(s.Name ?? string.Empty).Equals(inputFull, StringComparison.OrdinalIgnoreCase));
                    if (byFull != null) return byFull;

                    var inputInit = asciiLetters; // also allow matching initials typed (e.g., zs)
                    var byInit = _students.FirstOrDefault(s => (s.NamePinyinInitials ?? string.Empty).Equals(inputInit, StringComparison.OrdinalIgnoreCase)
                                                               || ToPinyinAcronym(s.Name ?? string.Empty).Equals(inputInit, StringComparison.OrdinalIgnoreCase));
                    if (byInit != null) return byInit;
                }
                else
                {
                    // If user pasted Chinese, compare pinyin initials of input with candidates
                    var pyInit = ToPinyinAcronym(name);
                    var byPy = _students.FirstOrDefault(s => (s.NamePinyinInitials ?? string.Empty).Equals(pyInit, StringComparison.OrdinalIgnoreCase)
                                                             || ToPinyinAcronym(s.Name ?? string.Empty).Equals(pyInit, StringComparison.OrdinalIgnoreCase));
                    if (byPy != null) return byPy;
                }
            }
            return null;
        }

        private static string ToPinyinAcronym(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            // NPinyin returns full pinyin with spaces, we take initials
            var full = Pinyin.GetPinyin(text); // e.g., "张三" -> "zhang san"
            var parts = full.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return string.Empty;
            var sb = new System.Text.StringBuilder(parts.Length);
            foreach (var p in parts)
            {
                sb.Append(char.ToLowerInvariant(p[0]));
            }
            return sb.ToString();
        }

        private static string ToFullPinyinKey(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var full = Pinyin.GetPinyin(text);
            return LettersOnlyLower(full);
        }

        private static string LettersOnlyLower(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var arr = s.Where(ch => char.IsLetter(ch)).Select(ch => char.ToLowerInvariant(ch)).ToArray();
            return new string(arr);
        }

        private void OnIdOrNameChanged()
        {
            var st = MatchStudentByIdOrName(StudentIdInput, NameInput);
            if (st != null)
            {
                if (!string.Equals(NameInput, st.Name, StringComparison.OrdinalIgnoreCase)) NameInput = st.Name;
                if (!string.Equals(StudentIdInput, st.Id, StringComparison.OrdinalIgnoreCase)) StudentIdInput = st.Id;
                if (!string.Equals(ClassInput, st.Class, StringComparison.OrdinalIgnoreCase)) ClassInput = st.Class;
            }
        }

        private static string? ExtractClassFromText(string line)
        {
            // Arabic digits before or with 班
            var m1 = Regex.Match(line, "(?<n>\\d{1,3})\\s*班?");
            if (m1.Success) return m1.Groups["n"].Value;
            // Chinese numerals before 班
            var m2 = Regex.Match(line, "(?<c>[一二三四五六七八九十]{1,3})\\s*班");
            if (m2.Success)
            {
                var c = m2.Groups["c"].Value;
                return ChineseNumeralToNumber(c);
            }
            return null;
        }

        private static string ChineseNumeralToNumber(string c)
        {
            // very simple map for 1-20 and up to 99 (十/二十/二十三) style
            var map = new System.Collections.Generic.Dictionary<char, int>
            {
                ['零'] = 0, ['一'] = 1, ['二'] = 2, ['两'] = 2, ['三'] = 3, ['四'] = 4, ['五'] = 5, ['六'] = 6, ['七'] = 7, ['八'] = 8, ['九'] = 9, ['十'] = 10
            };
            int val = 0;
            if (c.Length == 1)
            {
                if (map.TryGetValue(c[0], out var v)) val = v;
            }
            else
            {
                // handle 十x, x十y
                int tenIdx = c.IndexOf('十');
                if (tenIdx >= 0)
                {
                    int tens = tenIdx == 0 ? 1 : (map.TryGetValue(c[0], out var t) ? t : 0);
                    int ones = (tenIdx < c.Length - 1 && map.TryGetValue(c.Last(), out var o)) ? o : 0;
                    val = tens * 10 + ones;
                }
            }
            return val == 0 ? c : val.ToString();
        }

        private (Student? student, int matchIndex, string matchedText, List<Student> candidates) FindBestMatch(string line)
        {
            // Prefer ID exact containment match
            foreach (var st in _students)
            {
                if (!string.IsNullOrEmpty(st.Id))
                {
                    var idx = line.IndexOf(st.Id, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                        return (st, idx, st.Id!, new List<Student> { st });
                }
            }

            // Name substring candidates
            var hits = new List<(Student st, int idx)>();
            foreach (var st in _students)
            {
                if (string.IsNullOrWhiteSpace(st.Name)) continue;
                var idx = line.IndexOf(st.Name!, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    hits.Add((st, idx));
            }
            if (hits.Count == 1)
                return (hits[0].st, hits[0].idx, hits[0].st.Name ?? string.Empty, new List<Student> { hits[0].st });
            if (hits.Count > 1)
            {
                // try filter by class extracted from text
                var clsHint = ExtractClassFromText(line);
                var ordered = hits.OrderBy(h => h.idx).Select(h => h.st).ToList();
                if (!string.IsNullOrWhiteSpace(clsHint))
                {
                    var narrowed = ordered.Where(s => string.Equals((s.Class ?? string.Empty).Trim(), clsHint, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (narrowed.Count == 1)
                        return (narrowed[0], -1, narrowed[0].Name ?? string.Empty, new List<Student> { narrowed[0] });
                    if (narrowed.Count > 1)
                        return (null, -1, string.Empty, narrowed);
                }
                return (null, -1, string.Empty, ordered);
            }

            return (null, -1, string.Empty, new List<Student>());
        }

        private async void AutoFillCurrent()
        {
            var line = CurrentLineText;
            if (string.IsNullOrWhiteSpace(line)) return;

            var (student, idx, matched, candidates) = FindBestMatch(line);
            var classHint = ExtractClassFromText(line);
            if (student != null)
            {
                NameInput = student.Name;
                ClassInput = string.IsNullOrWhiteSpace(classHint) ? student.Class : classHint;
                StudentIdInput = student.Id;
                // reason is the tail after matched token
                if (idx >= 0)
                {
                    var tailStart = idx + matched.Length;
                    ReasonInput = tailStart < line.Length ? line.Substring(tailStart).Trim() : string.Empty;
                }
                else
                {
                    ReasonInput = string.Empty;
                }
                return;
            }

            if (candidates.Count > 1)
            {
                // Prefer multi-select if available; fallback to single selection
                if (SelectMultiAsync != null)
                {
                    var chosenMany = await SelectMultiAsync(candidates, line);
                    if (chosenMany != null && chosenMany.Count == 1)
                    {
                        var c = chosenMany[0];
                        NameInput = c.Name;
            ClassInput = string.IsNullOrWhiteSpace(classHint) ? c.Class : classHint;
                        StudentIdInput = c.Id;
                        ReasonInput = string.Empty;
                    }
                    else if (chosenMany != null && chosenMany.Count > 1)
                    {
                        // defer to Next(): create records for all
                        _multiChosen = chosenMany;
                        NameInput = string.Empty;
                        ClassInput = classHint; // keep hint if any
                        // keep ReasonInput as user will type a common reason
                    }
                    // if multiple, we keep inputs empty; Next() will create records for all chosen entries using Reason/Score
                }
                else if (SelectStudentAsync != null)
                {
                    var chosen = await SelectStudentAsync(candidates, line);
                    if (chosen != null)
                    {
                        NameInput = chosen.Name;
            ClassInput = string.IsNullOrWhiteSpace(classHint) ? chosen.Class : classHint;
                        StudentIdInput = chosen.Id;
                        ReasonInput = string.Empty;
                    }
                }
            }
        }

        private void AddToPending()
        {
            var line = CurrentLineText;
            if (string.IsNullOrWhiteSpace(line)) { Advance(); AutoFillCurrent(); return; }

            double score = 0;
            if (!string.IsNullOrWhiteSpace(ScoreInput))
            {
                if (!double.TryParse(ScoreInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out score))
                {
                    double.TryParse(ScoreInput, out score);
                }
            }

            if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) return;

            var remark = string.IsNullOrWhiteSpace(ReasonInput) ? line : ReasonInput;
            
            // 添加到待处理列表（暂不分配给特定学生）
            var store = new EvaluationStore(_baseDir);
            var all = store.Load();
            all.Add(new EvaluationEntry
            {
                Class = ClassInput,
                Name = NameInput, // 可能为空，等待后续处理
                Remark = remark,
                Score = score,
                Date = DateTime.Now,
                StudentId = StudentIdInput,
            });
            store.Save(all);

            Advance();
            AutoFillCurrent();
        }

        private async Task NextAsync()
        {
            try
            {
                Logger.LogDebug("NextAsync 开始", "InfoEntryViewModel");
                
                var line = CurrentLineText;
                if (string.IsNullOrWhiteSpace(line)) 
                { 
                    Advance(); 
                    AutoFillCurrent(); 
                    Logger.LogDebug("当前行为空，跳过", "InfoEntryViewModel");
                    return; 
                }

                double score = 0;
                if (!string.IsNullOrWhiteSpace(ScoreInput))
                {
                    if (!double.TryParse(ScoreInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out score))
                    {
                        // try current culture
                        if (!double.TryParse(ScoreInput, out score))
                        {
                            Logger.LogWarning($"无法解析分数: {ScoreInput}", "InfoEntryViewModel");
                            ErrorHandler.HandleError(new ArgumentException($"无法解析分数: {ScoreInput}"), "分数格式不正确", "InfoEntryViewModel.NextAsync");
                            return;
                        }
                    }
                }

                if (!ScoreManagerForSchool.UI.Security.AuthManager.Ensure(_baseDir)) 
                {
                    Logger.LogWarning("身份验证失败", "InfoEntryViewModel");
                    return;
                }

                var remark = string.IsNullOrWhiteSpace(ReasonInput) ? line : ReasonInput;

                if (_multiChosen != null && _multiChosen.Count > 0)
                {
                    Logger.LogInfo($"批量录入 {_multiChosen.Count} 名学生", "InfoEntryViewModel");
                    foreach (var st in _multiChosen)
                    {
                        var className = string.IsNullOrWhiteSpace(ClassInput) ? st.Class : ClassInput;
                        _evaluationService.AddEvaluation(
                            className ?? string.Empty,
                            st.Id ?? string.Empty,
                            st.Name ?? string.Empty,
                            remark ?? string.Empty,
                            score,
                            null,
                            TeacherInput
                        );
                    }
                    _multiChosen = null;
                }
                else if (string.IsNullOrWhiteSpace(NameInput))
                {
                    Logger.LogInfo("录入临时记录（未指定学生）", "InfoEntryViewModel");
                    // 临时记录（未指定学生）
                    var store = new EvaluationStore(_baseDir);
                    var all = store.Load();
                    all.Add(new EvaluationEntry
                    {
                        Class = ClassInput,
                        Name = null,
                        Remark = line,
                        Score = score,
                        Date = DateTime.Now,
                    });
                    store.Save(all);
                }
                else
                {
                    Logger.LogInfo($"录入学生积分: {NameInput}, 分数: {score}", "InfoEntryViewModel");
                    _evaluationService.AddEvaluation(
                        ClassInput ?? string.Empty,
                        StudentIdInput ?? string.Empty,
                        NameInput ?? string.Empty,
                        remark ?? string.Empty,
                        score,
                        null,
                        TeacherInput
                    );
                }

                Advance();
                AutoFillCurrent();
                Logger.LogDebug("NextAsync 完成", "InfoEntryViewModel");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError("NextAsync 执行失败", "InfoEntryViewModel", ex);
                ErrorHandler.HandleError(ex, "录入积分信息时发生错误", "InfoEntryViewModel.NextAsync");
            }
        }

        private void LoadPendingItems()
        {
            try
            {
                PendingItems.Clear();
                var store = new EvaluationStore(_baseDir);
                var allEvaluations = store.Load();
                
                // 加载待处理项（没有姓名的记录）
                var pendingItems = allEvaluations
                    .Where(e => string.IsNullOrWhiteSpace(e.Name))
                    .OrderByDescending(e => e.Date)
                    .ToList();
                
                foreach (var item in pendingItems)
                {
                    PendingItems.Add(item);
                }
                
                Logger.LogInfo($"加载了 {PendingItems.Count} 个待处理项", "InfoEntryViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("加载待处理项失败", "InfoEntryViewModel", ex);
                ErrorHandler.HandleError(ex, "加载待处理项时发生错误", "InfoEntryViewModel.LoadPendingItems");
            }
        }

        private void ProcessPending(EvaluationEntry? entry)
        {
            if (entry == null) return;
            
            try
            {
                // 将待处理项添加到多行粘贴框的最后一行
                var currentText = PasteText ?? string.Empty;
                var newLine = entry.Remark ?? string.Empty;
                
                if (string.IsNullOrWhiteSpace(currentText))
                {
                    PasteText = newLine;
                }
                else
                {
                    PasteText = currentText.TrimEnd() + Environment.NewLine + newLine;
                }
                
                // 重新解析行
                ParseLines();
                
                // 从待处理列表中移除
                PendingItems.Remove(entry);
                
                // 从数据库中删除
                var store = new EvaluationStore(_baseDir);
                var allEvaluations = store.Load();
                allEvaluations.RemoveAll(e => e.Id == entry.Id);
                store.Save(allEvaluations);
                
                Logger.LogInfo($"处理待处理项: {entry.Remark}", "InfoEntryViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("处理待处理项失败", "InfoEntryViewModel", ex);
                ErrorHandler.HandleError(ex, "处理待处理项时发生错误", "InfoEntryViewModel.ProcessPending");
            }
        }

        private void DeletePending(EvaluationEntry? entry)
        {
            if (entry == null) return;
            
            try
            {
                // 从待处理列表中移除
                PendingItems.Remove(entry);
                
                // 从数据库中删除
                var store = new EvaluationStore(_baseDir);
                var allEvaluations = store.Load();
                allEvaluations.RemoveAll(e => e.Id == entry.Id);
                store.Save(allEvaluations);
                
                Logger.LogInfo($"删除待处理项: {entry.Remark}", "InfoEntryViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("删除待处理项失败", "InfoEntryViewModel", ex);
                ErrorHandler.HandleError(ex, "删除待处理项时发生错误", "InfoEntryViewModel.DeletePending");
            }
        }
    }
}

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
using ScoreManagerForSchool.Core.Tools;

namespace ScoreManagerForSchool.UI.ViewModels
{
    public class InfoEntryViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly string _baseDir;
        private readonly List<Student> _students;
        private readonly EnhancedEvaluationService _evaluationService;
    private List<Student>? _multiChosen;
    private bool _inAutoFill;

        public InfoEntryViewModel(string? baseDir = null)
        {
            try
            {
                Logger.LogInfo("InfoEntryViewModel initialization started", "InfoEntryViewModel");
                
                _baseDir = string.IsNullOrEmpty(baseDir) ? Path.Combine(Directory.GetCurrentDirectory(), "base") : baseDir;
                _students = new StudentStore(_baseDir).Load();
                _evaluationService = new EnhancedEvaluationService(_baseDir);
                
                ClassOptions = new ObservableCollection<string>();
                foreach (var c in _students.Select(s => (s.Class ?? string.Empty).Trim()).Where(s => s.Length > 0).Distinct())
                    ClassOptions.Add(c);

                // 设置默认积分为2分
                ScoreInput = "2";

                NextCommand = new RelayCommand(async _ => await NextAsync());
                SkipCommand = new RelayCommand(_ => { ClearAllInputs(); Advance(); AutoFillCurrent(); });
                AddToPendingCommand = new RelayCommand(_ => AddToPending());
                ProcessPendingCommand = new RelayCommand(p => ProcessPending(p as EvaluationEntry));
                DeletePendingCommand = new RelayCommand(p => DeletePending(p as EvaluationEntry));
                
                LoadPendingItems();
                
                Logger.LogInfo($"InfoEntryViewModel initialization completed, loaded {_students.Count} students", "InfoEntryViewModel");
            }
            catch (Exception ex)
            {
                Logger.LogError("InfoEntryViewModel initialization failed", "InfoEntryViewModel", ex);
                ErrorHandler.HandleError(ex, "信息录入模块初始化失败", "InfoEntryViewModel.Constructor");
                throw;
            }
        }

        // Callback set by View to allow showing a selection dialog when multiple candidates
    public Func<List<Student>, string, Task<Student?>>? SelectStudentAsync { get; set; }
    public Func<List<Student>, string, Task<List<Student>?>>? SelectMultiAsync { get; set; }
    public Func<List<Teacher>, string, Task<Teacher?>>? SelectTeacherAsync { get; set; }

        private string? _pasteText;
        public string? PasteText
        {
            get => _pasteText; 
            set 
            { 
                if (_pasteText != value)
                {
                    _pasteText = value; 
                    ParseLines(); 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PasteText))); 
                }
            }
        }

        private List<string> _lines = new();
        private int _index = 0;
        public string? CurrentLineText => (_index >= 0 && _index < _lines.Count) ? _lines[_index] : null;

        private string? _classInput;
    public string? ClassInput 
    { 
        get => _classInput; 
        set 
        { 
            if (_classInput != value)
            {
                _classInput = value; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClassInput))); 
                // 当班级变化后，尝试按“科目+班级”自动匹配任课教师
                TryResolveTeacherBySubjectWithClass();
                // 若当前还未确定学生，则基于班级重新尝试自动匹配本行（不在自动填充过程中时）
                if (!_inAutoFill && string.IsNullOrWhiteSpace(NameInput) && string.IsNullOrWhiteSpace(StudentIdInput))
                {
                    AutoFillCurrent();
                }
            }
        } 
    }

    private string? _studentIdInput;
    public string? StudentIdInput 
    { 
        get => _studentIdInput; 
        set 
        { 
            if (_studentIdInput != value)
            {
                _studentIdInput = value; 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StudentIdInput))); 
                // 不在这里触发匹配，匹配逻辑仅基于当前处理行
            }
        } 
    }
        private string? _nameInput;
    public string? NameInput 
    { 
        get => _nameInput; 
        set 
        { 
            if (_nameInput != value)
            {
                _nameInput = value; 
                UpdateClassOptions(); 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameInput))); 
                // 不在这里触发匹配，匹配逻辑仅基于当前处理行
            }
        } 
    }

        private string? _reasonInput;
        public string? ReasonInput 
        { 
            get => _reasonInput; 
            set 
            { 
                if (_reasonInput != value)
                {
                    _reasonInput = value; 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReasonInput))); 
                }
            } 
        }

        private string? _scoreInput;
        public string? ScoreInput 
        { 
            get => _scoreInput; 
            set 
            { 
                if (_scoreInput != value)
                {
                    _scoreInput = value; 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScoreInput))); 
                    ValidateScore();
                }
            } 
        }

        // 积分错误消息
        private string? _scoreErrorMessage;
        public string? ScoreErrorMessage 
        { 
            get => _scoreErrorMessage; 
            set 
            { 
                if (_scoreErrorMessage != value)
                {
                    _scoreErrorMessage = value; 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScoreErrorMessage))); 
                }
            } 
        }

        /// <summary>
        /// 验证积分输入
        /// </summary>
        private void ValidateScore()
        {
            if (string.IsNullOrWhiteSpace(ScoreInput))
            {
                ScoreErrorMessage = "积分不能为空";
                return;
            }

            if (!double.TryParse(ScoreInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                ScoreErrorMessage = "积分格式不正确，请输入数字";
                return;
            }

            ScoreErrorMessage = null; // 验证通过，清空错误消息
        }

        // 新增教师相关属性
        private string? _teacherInput;
        public string? TeacherInput 
        { 
            get => _teacherInput; 
            set 
            { 
                if (_teacherInput != value)
                {
                    _teacherInput = value; 
                    OnTeacherInputChanged(); 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TeacherInput))); 
                }
            } 
        }

        private string? _matchedTeacherInfo;
        public string? MatchedTeacherInfo 
        { 
            get => _matchedTeacherInfo; 
            set 
            { 
                if (_matchedTeacherInfo != value)
                {
                    _matchedTeacherInfo = value; 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MatchedTeacherInfo))); 
                }
            } 
        }

        // 当班级变更后，若之前已识别出科目但未能匹配教师，尝试用当前班级再匹配一次
        private void TryResolveTeacherBySubjectWithClass()
        {
            var line = CurrentLineText;
            if (string.IsNullOrWhiteSpace(line)) return;
            var subject = DetectSubjectFromInput(line);
            if (string.IsNullOrWhiteSpace(subject))
            {
                var py = PinyinUtil.Full(line);
                subject = DetectSubjectFromInput(py);
            }
            if (string.IsNullOrWhiteSpace(subject)) return;

            if (!string.IsNullOrWhiteSpace(ClassInput))
            {
                var t = _evaluationService.MatchTeacherBySubjectAndClass(subject!, ClassInput);
                if (t != null)
                {
                    // 根据用户要求，只填教师名字，不包括科目
                    TeacherInput = t.Name;
                    MatchedTeacherInfo = $"✓ 找到教师：{t.Name} ({t.Subject} - {t.SubjectGroup})（按科目+班级）";
                }
                else
                {
                    // 明确提示用户班级未能唯一匹配
                    MatchedTeacherInfo = $"已识别科目：{subject}，未匹配到该班级任课教师";
                }
            }
            else
            {
                MatchedTeacherInfo = $"已识别科目：{subject}，请选择班级";
            }
        }

        // 学生匹配信息显示
        private string? _matchedStudentInfo;
        public string? MatchedStudentInfo 
        { 
            get => _matchedStudentInfo; 
            set 
            { 
                if (_matchedStudentInfo != value)
                {
                    _matchedStudentInfo = value; 
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MatchedStudentInfo))); 
                }
            } 
        }

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

        /// <summary>
        /// 清空所有输入框（跳过时使用，不包括待处理和正在处理的信息）
        /// </summary>
        private void ClearAllInputs()
        {
            ClassInput = string.Empty;
            NameInput = string.Empty;
            StudentIdInput = string.Empty;
            TeacherInput = string.Empty;
            ReasonInput = string.Empty;
            // 保留ScoreInput，用户可能希望为多个学生录入相同分数
            
            // 清空匹配信息显示
            MatchedStudentInfo = null;
            MatchedTeacherInfo = null;
            
            // 清空多选状态
            _multiChosen?.Clear();
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

            // 先检测输入中是否包含科目关键词
            var detectedSubject = DetectSubjectFromInput(TeacherInput);
            var searchClass = !string.IsNullOrWhiteSpace(ClassInput) ? ClassInput : null;

            Teacher? teacher = null;

            if (!string.IsNullOrWhiteSpace(detectedSubject))
            {
                // 如果检测到科目，优先按科目+班级匹配教师
                teacher = _evaluationService.MatchTeacherBySubjectAndClass(detectedSubject, searchClass);
                if (teacher != null)
                {
                    MatchedTeacherInfo = $"🎯 {teacher.Name} ({teacher.Subject} - {teacher.SubjectGroup}) [科目匹配]";
                    Logger.LogInfo($"Teacher matched by subject: {teacher.Name}, Subject: {detectedSubject}, Class: {searchClass}", "InfoEntryViewModel");
                    return;
                }
            }

            // 如果科目匹配失败，回退到原有的教师姓名匹配
            teacher = _evaluationService.MatchTeacher(TeacherInput, searchClass);
            if (teacher != null)
            {
                MatchedTeacherInfo = $"👤 {teacher.Name} ({teacher.Subject} - {teacher.SubjectGroup}) [姓名匹配]";
            }
            else
            {
                MatchedTeacherInfo = "未找到匹配的教师";
            }
        }

        /// <summary>
        /// 从输入文本中检测科目关键词
        /// </summary>
        private string? DetectSubjectFromInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            // 科目关键词映射表
            var subjectKeywords = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["语文"] = new List<string> { "语文", "chinese", "yw", "yuwen" },
                ["数学"] = new List<string> { "数学", "math", "mathematics", "sx", "shuxue" },
                ["英语"] = new List<string> { "英语", "english", "yy", "yingyu" },
                ["物理"] = new List<string> { "物理", "physics", "wl", "wuli" },
                ["化学"] = new List<string> { "化学", "chemistry", "hx", "huaxue" },
                ["生物"] = new List<string> { "生物", "biology", "sw", "shengwu" },
                ["政治"] = new List<string> { "政治", "politics", "zz", "zhengzhi" },
                ["历史"] = new List<string> { "历史", "history", "ls", "lishi" },
                ["地理"] = new List<string> { "地理", "geography", "dl", "dili" },
                ["体育"] = new List<string> { "体育", "pe", "sports", "ty", "tiyu" },
                ["音乐"] = new List<string> { "音乐", "music", "yl", "yinyue" },
                ["美术"] = new List<string> { "美术", "art", "ms", "meishu" },
                ["信息技术"] = new List<string> { "信息技术", "计算机", "computer", "it", "信息", "xxjs", "jisuan" },
                ["科学"] = new List<string> { "科学", "science", "kx", "kexue" }
            };

            // 检查每个科目的关键词
            foreach (var subject in subjectKeywords)
            {
                foreach (var keyword in subject.Value)
                {
                    if (input.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogInfo($"Subject keyword detected: {keyword} -> {subject.Key}", "InfoEntryViewModel");
                        return subject.Key;
                    }
                }
            }

            return null;
        }

        private Student? MatchStudentByIdOrName(string? id, string? name)
        {
            // 优先按学号精确匹配
            if (!string.IsNullOrWhiteSpace(id))
            {
                var st = _students.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
                if (st != null) return st;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                // 如果有班级信息，优先在班级内匹配
                var classStudents = string.IsNullOrWhiteSpace(ClassInput) 
                    ? _students 
                    : _students.Where(s => string.Equals(s.Class?.Trim(), ClassInput?.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

                // 1. 精确姓名匹配（在班级内）
                var exact = classStudents.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;

                // 2. 转换用户输入为拼音进行匹配
                var inputPinyin = PinyinUtil.Full(name);  // 用户输入转为拼音
                var inputInitials = PinyinUtil.Initials(name);  // 用户输入转为拼音首字母

                // 检测输入是否为纯字母（用户可能直接输入拼音）
                var asciiLetters = PinyinUtil.LettersOnlyLower(name);
                bool inputIsAscii = asciiLetters.Length > 0 && asciiLetters.Length == name.Count(ch => char.IsLetter(ch));

                if (inputIsAscii)
                {
                    // 用户输入的是拼音，直接匹配存储的拼音字段
                    var inputFull = asciiLetters; // 用户输入的拼音
                    var byFull = classStudents.FirstOrDefault(s => 
                        !string.IsNullOrEmpty(s.NamePinyin) && s.NamePinyin.Equals(inputFull, StringComparison.OrdinalIgnoreCase));
                    if (byFull != null) return byFull;

                    // 匹配拼音首字母
                    var byInit = classStudents.FirstOrDefault(s => 
                        !string.IsNullOrEmpty(s.NamePinyinInitials) && s.NamePinyinInitials.Equals(inputFull, StringComparison.OrdinalIgnoreCase));
                    if (byInit != null) return byInit;
                }
                else
                {
                    // 用户输入的是中文，用转换后的拼音匹配
                    // 全拼音匹配
                    var byFullPinyin = classStudents.FirstOrDefault(s => 
                        !string.IsNullOrEmpty(s.NamePinyin) && s.NamePinyin.Equals(inputPinyin, StringComparison.OrdinalIgnoreCase));
                    if (byFullPinyin != null) return byFullPinyin;

                    // 拼音首字母匹配
                    var byInitials = classStudents.FirstOrDefault(s => 
                        !string.IsNullOrEmpty(s.NamePinyinInitials) && s.NamePinyinInitials.Equals(inputInitials, StringComparison.OrdinalIgnoreCase));
                    if (byInitials != null) return byInitials;
                }

                // 3. 如果班级内没找到，扩展到全校搜索（但优先级较低）
                if (classStudents.Count != _students.Count)
                {
                    Logger.LogInfo($"No match found in class, expanding to school-wide search: {name}", "InfoEntryViewModel");
                    if (inputIsAscii)
                    {
                        var globalByFull = _students.FirstOrDefault(s => 
                            !string.IsNullOrEmpty(s.NamePinyin) && s.NamePinyin.Equals(asciiLetters, StringComparison.OrdinalIgnoreCase));
                        if (globalByFull != null) return globalByFull;

                        var globalByInit = _students.FirstOrDefault(s => 
                            !string.IsNullOrEmpty(s.NamePinyinInitials) && s.NamePinyinInitials.Equals(asciiLetters, StringComparison.OrdinalIgnoreCase));
                        if (globalByInit != null) return globalByInit;
                    }
                    else
                    {
                        var globalByFullPinyin = _students.FirstOrDefault(s => 
                            !string.IsNullOrEmpty(s.NamePinyin) && s.NamePinyin.Equals(inputPinyin, StringComparison.OrdinalIgnoreCase));
                        if (globalByFullPinyin != null) return globalByFullPinyin;

                        var globalByInitials = _students.FirstOrDefault(s => 
                            !string.IsNullOrEmpty(s.NamePinyinInitials) && s.NamePinyinInitials.Equals(inputInitials, StringComparison.OrdinalIgnoreCase));
                        if (globalByInitials != null) return globalByInitials;
                    }
                }
            }
            return null;
        }

        private void OnIdOrNameChanged()
        {
            // 清空之前的匹配信息
            MatchedStudentInfo = null;

            if (string.IsNullOrWhiteSpace(StudentIdInput) && string.IsNullOrWhiteSpace(NameInput))
            {
                return;
            }

            var st = MatchStudentByIdOrName(StudentIdInput, NameInput);
            if (st != null)
            {
                // 显示匹配信息
                var matchType = "";
                if (!string.IsNullOrEmpty(StudentIdInput) && string.Equals(StudentIdInput, st.Id, StringComparison.OrdinalIgnoreCase))
                {
                    matchType = "学号匹配";
                }
                else if (!string.IsNullOrEmpty(NameInput))
                {
                    if (string.Equals(NameInput, st.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        matchType = "姓名精确匹配";
                    }
                    else
                    {
                        // 检查是否是拼音匹配
                        var inputPinyin = PinyinUtil.Full(NameInput);
                        var inputInitials = PinyinUtil.Initials(NameInput);
                        var asciiLetters = PinyinUtil.LettersOnlyLower(NameInput);
                        bool inputIsAscii = asciiLetters.Length > 0 && asciiLetters.Length == NameInput.Count(ch => char.IsLetter(ch));

                        if (inputIsAscii)
                        {
                            if (!string.IsNullOrEmpty(st.NamePinyin) && st.NamePinyin.Equals(asciiLetters, StringComparison.OrdinalIgnoreCase))
                            {
                                matchType = "拼音全拼匹配";
                            }
                            else if (!string.IsNullOrEmpty(st.NamePinyinInitials) && st.NamePinyinInitials.Equals(asciiLetters, StringComparison.OrdinalIgnoreCase))
                            {
                                matchType = "拼音首字母匹配";
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(st.NamePinyin) && st.NamePinyin.Equals(inputPinyin, StringComparison.OrdinalIgnoreCase))
                            {
                                matchType = "中文转拼音匹配";
                            }
                            else if (!string.IsNullOrEmpty(st.NamePinyinInitials) && st.NamePinyinInitials.Equals(inputInitials, StringComparison.OrdinalIgnoreCase))
                            {
                                matchType = "中文转拼音首字母匹配";
                            }
                        }
                    }
                }

                var classInfo = !string.IsNullOrWhiteSpace(ClassInput) ? "班级内" : "全校";
                MatchedStudentInfo = $"✓ 找到学生：{st.Name} ({st.Class}) - {classInfo}{matchType}";

                // 自动填充信息
                if (!string.Equals(NameInput, st.Name, StringComparison.OrdinalIgnoreCase)) NameInput = st.Name;
                if (!string.Equals(StudentIdInput, st.Id, StringComparison.OrdinalIgnoreCase)) StudentIdInput = st.Id;
                if (!string.Equals(ClassInput, st.Class, StringComparison.OrdinalIgnoreCase)) ClassInput = st.Class;
            }
            else
            {
                // 未找到匹配的学生
                if (!string.IsNullOrWhiteSpace(NameInput) || !string.IsNullOrWhiteSpace(StudentIdInput))
                {
                    var searchScope = !string.IsNullOrWhiteSpace(ClassInput) ? $"在{ClassInput}班级内" : "在全校范围内";
                    var searchTerm = !string.IsNullOrWhiteSpace(NameInput) ? $"姓名/拼音'{NameInput}'" : $"学号'{StudentIdInput}'";
                    MatchedStudentInfo = $"⚠ {searchScope}未找到匹配{searchTerm}的学生";
                }
            }
        }

        private string? ExtractClassFromText(string line)
        {
            // 移除所有空格，便于匹配
            var cleanLine = Regex.Replace(line, @"\s+", "");
            
            // 轮询所有已知班级，检查哪个班级在当前行中出现
            var allClasses = _students.Where(s => !string.IsNullOrWhiteSpace(s.Class))
                                    .Select(s => s.Class!.Trim())
                                    .Distinct()
                                    .OrderByDescending(c => c.Length) // 优先匹配更长的班级名
                                    .ToList();
                                    
            foreach (var className in allClasses)
            {
                var cleanClassName = Regex.Replace(className, @"\s+", "");
                var escapedClassName = Regex.Escape(cleanClassName);
                
                // 使用正则匹配，允许班级名出现在行中任何位置，但要求边界正确
                var classPattern = $@"(?<![0-9一-鿿]){escapedClassName}(?![0-9一-鿿])";
                if (Regex.IsMatch(cleanLine, classPattern, RegexOptions.IgnoreCase))
                {
                    return className;
                }
                
                // 同时检查 "班级名+班" 的模式
                var classWithBanPattern = $@"(?<![0-9一-鿿]){escapedClassName}班(?![0-9一-鿿])";
                if (Regex.IsMatch(cleanLine, classWithBanPattern, RegexOptions.IgnoreCase))
                {
                    return className;
                }
            }
            
            // 如果轮询失败，回退到原来的正则匹配
            // Arabic digits followed by 班 (must have 班)
            var m1 = Regex.Match(line, @"(?<n>\d{1,3})[\s,，;；·\.、\-]{0,2}班");
            if (m1.Success) return m1.Groups["n"].Value;
            
            // Chinese numerals followed by 班
            var m2 = Regex.Match(line, @"(?<c>[一二三四五六七八九十]{1,3})[\s,，;；·\.、\-]{0,2}班");
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
            // Prepare a pinyin-only version of the entire line for robust matching
            var linePinyin = PinyinUtil.Full(line); // letters-only lower of whole line

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

            // Name regex candidates（中文名使用正则匹配，支持更精确的边界检测）
            var hits = new List<(Student st, int idx)>();
            // 同时准备一个无空格版本的行，用于处理空格干扰
            var cleanLine = Regex.Replace(line, @"\s+", "");
            
            foreach (var st in _students)
            {
                if (string.IsNullOrWhiteSpace(st.Name)) continue;
                
                // 首先尝试精确匹配（放宽边界检测，允许中间位置匹配）
                var escapedName = Regex.Escape(st.Name!);
                // 改进：只要求姓名不与其他中文字符直接相连即可，允许与符号、数字、空格相邻
                var pattern = $@"(?<![一-鿿]){escapedName}(?![一-鿿])";
                var m = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    hits.Add((st, m.Index));
                    continue;
                }
                
                // 如果精确匹配失败，尝试在无空格版本中匹配
                var cleanName = Regex.Replace(st.Name!, @"\s+", "");
                var cleanNameEscaped = Regex.Escape(cleanName);
                var cleanPattern = $@"(?<![一-鿿]){cleanNameEscaped}(?![一-鿿])";
                var cleanMatch = Regex.Match(cleanLine, cleanPattern, RegexOptions.IgnoreCase);
                if (cleanMatch.Success && cleanName.Length >= 2)
                {
                    // 找到在原行中的大概位置
                    var approxIndex = line.IndexOf(st.Name![0], StringComparison.OrdinalIgnoreCase);
                    hits.Add((st, approxIndex >= 0 ? approxIndex : 0));
                }
            }
            if (hits.Count == 1)
                return (hits[0].st, hits[0].idx, hits[0].st.Name ?? string.Empty, new List<Student> { hits[0].st });
            if (hits.Count > 1)
            {
                // try filter by class extracted from text, fallback to current ClassInput
                var clsHint = ExtractClassFromText(line);
                var ctxClass = !string.IsNullOrWhiteSpace(clsHint) ? clsHint : (string.IsNullOrWhiteSpace(ClassInput) ? null : ClassInput);
                var ordered = hits.OrderBy(h => h.idx).Select(h => h.st).ToList();
                if (!string.IsNullOrWhiteSpace(ctxClass))
                {
                    var narrowed = ordered.Where(s => string.Equals((s.Class ?? string.Empty).Trim(), ctxClass, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (narrowed.Count == 1)
                        return (narrowed[0], -1, narrowed[0].Name ?? string.Empty, new List<Student> { narrowed[0] });
                    if (narrowed.Count > 1)
                        return (null, -1, string.Empty, narrowed);
                }
                return (null, -1, string.Empty, ordered);
            }

            // Pinyin-based matching on current line（以姓名拼音为目标轮询待处理行）
            {
                var clsHint = ExtractClassFromText(line);
                var ctxClass = !string.IsNullOrWhiteSpace(clsHint) ? clsHint : (string.IsNullOrWhiteSpace(ClassInput) ? null : ClassInput);
                var scope = string.IsNullOrWhiteSpace(ctxClass)
                    ? _students
                    : _students.Where(s => string.Equals((s.Class ?? string.Empty).Trim(), ctxClass, StringComparison.OrdinalIgnoreCase)).ToList();

                // 以每个学生的姓名拼音为目标，轮询检查当前行是否包含该拼音
                var fullMatches = new List<Student>();
                var initMatches = new List<Student>();
                
                foreach (var student in scope)
                {
                    // 检查学生全拼是否在当前行中出现（使用更精确的边界检测）
                    if (!string.IsNullOrEmpty(student.NamePinyin) && !string.IsNullOrEmpty(linePinyin))
                    {
                        var escapedPinyin = Regex.Escape(student.NamePinyin);
                        var pinyinPattern = $@"(?<![a-zA-Z]){escapedPinyin}(?![a-zA-Z])";
                        if (Regex.IsMatch(linePinyin, pinyinPattern, RegexOptions.IgnoreCase))
                        {
                            fullMatches.Add(student);
                            continue; // 全拼匹配优先，该学生跳过首字母检查
                        }
                    }
                    
                    // 如果全拼未匹配，检查学生首字母是否在当前行中出现（使用更精确的边界检测）
                    if (!string.IsNullOrEmpty(student.NamePinyinInitials) && !string.IsNullOrEmpty(linePinyin))
                    {
                        var escapedInitials = Regex.Escape(student.NamePinyinInitials);
                        var initialsPattern = $@"(?<![a-zA-Z]){escapedInitials}(?![a-zA-Z])";
                        if (Regex.IsMatch(linePinyin, initialsPattern, RegexOptions.IgnoreCase))
                        {
                            initMatches.Add(student);
                        }
                    }
                }
                
                // 优先返回全拼匹配结果
                if (fullMatches.Count == 1)
                    return (fullMatches[0], -1, fullMatches[0].Name ?? string.Empty, new List<Student> { fullMatches[0] });
                if (initMatches.Count == 1 && fullMatches.Count == 0)
                    return (initMatches[0], -1, initMatches[0].Name ?? string.Empty, new List<Student> { initMatches[0] });

                // 如果有多个匹配，返回候选列表供用户选择
                if (fullMatches.Count > 1)
                    return (null, -1, string.Empty, fullMatches);
                if (initMatches.Count > 1)
                    return (null, -1, string.Empty, initMatches);
            }

            return (null, -1, string.Empty, new List<Student>());
        }

        // 从行中匹配教师与科目（支持中文与拼音）
        private (Teacher? teacher, string? subject, string? info, List<Teacher> candidates) FindTeacherFromLine(string line)
        {
            var candidates = new List<Teacher>();
            string? info = null;

            // 班级提示
            var clsHint = ExtractClassFromText(line);

            // 先检测科目（中文或拼音关键字）
            string? subject = DetectSubjectFromInput(line);
            if (string.IsNullOrWhiteSpace(subject))
            {
                var pinyinLine = PinyinUtil.Full(line); // letters-only lower
                subject = DetectSubjectFromInput(pinyinLine);
            }

            // 如果有科目，优先按“班级+科目”直接匹配任课教师
            if (!string.IsNullOrWhiteSpace(subject))
            {
                var classForMatch = !string.IsNullOrWhiteSpace(clsHint) ? clsHint : (string.IsNullOrWhiteSpace(ClassInput) ? null : ClassInput);
                if (!string.IsNullOrWhiteSpace(classForMatch))
                {
                    var t0 = _evaluationService.MatchTeacherBySubjectAndClass(subject!, classForMatch);
                    if (t0 != null)
                    {
                        info = $"✓ 找到教师：{t0.Name} ({t0.Subject} - {t0.SubjectGroup})（按科目+班级）";
                        return (t0, subject, info, new List<Teacher> { t0 });
                    }
                }
            }

            // 用原始行搜索教师（可匹配中文姓名）
            var list1 = _evaluationService.SearchTeachers(line) ?? new List<Teacher>();
            // 再用拼音字符串搜索（可匹配姓名拼音/首字母）
            var pinyinQuery = PinyinUtil.Full(line);
            var list2 = string.IsNullOrWhiteSpace(pinyinQuery) ? new List<Teacher>() : _evaluationService.SearchTeachers(pinyinQuery);

            // 进一步：用“整行拼音正则匹配”
            var linePy = pinyinQuery;
            var list3 = new List<Teacher>();
            if (!string.IsNullOrWhiteSpace(linePy))
            {
                var allTeachers = _evaluationService.SearchTeachers("") ?? new List<Teacher>();
                foreach (var t in allTeachers)
                {
                    if (t == null) continue;
                    var teacherFullPinyin = t.NamePinyin ?? string.Empty;
                    var teacherInitials = t.NamePinyinInitials ?? string.Empty;
                    
                    // 检查教师全拼是否在当前行中出现
                    if (!string.IsNullOrEmpty(teacherFullPinyin))
                    {
                        if (linePy.Contains(teacherFullPinyin, StringComparison.OrdinalIgnoreCase))
                        {
                            list3.Add(t);
                            continue; // 全拼匹配优先，该教师跳过首字母检查
                        }
                    }
                    
                    // 如果全拼未匹配，检查教师首字母是否在当前行中出现
                    if (!string.IsNullOrEmpty(teacherInitials))
                    {
                        if (linePy.Contains(teacherInitials, StringComparison.OrdinalIgnoreCase))
                        {
                            list3.Add(t);
                        }
                    }
                }
            }

            // 合并去重
            var all = new List<Teacher>();
            void addRange(IEnumerable<Teacher> src)
            {
                foreach (var t in src)
                {
                    if (!all.Any(x => string.Equals(x.Name, t.Name, StringComparison.OrdinalIgnoreCase)))
                        all.Add(t);
                }
            }
            addRange(list1);
            addRange(list2);
            addRange(list3);

            // 若有班级提示，优先该班任教
            IEnumerable<Teacher> scoped = all;
            if (!string.IsNullOrWhiteSpace(clsHint))
            {
                var clsFiltered = all.Where(t => t.Classes.Any(c => string.Equals(c?.Trim(), clsHint?.Trim(), StringComparison.OrdinalIgnoreCase)) ).ToList();
                if (clsFiltered.Count > 0) scoped = clsFiltered;
            }

            // 若有科目，进一步按科目/科目组过滤优先
            if (!string.IsNullOrWhiteSpace(subject))
            {
                var bySubject = scoped.Where(t =>
                    (!string.IsNullOrEmpty(t.Subject) && t.Subject.Contains(subject, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(t.SubjectGroup) && t.SubjectGroup.Contains(subject, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                if (bySubject.Count > 0) scoped = bySubject;
            }

            candidates = scoped.ToList();
            if (candidates.Count == 1)
            {
                var t = candidates[0];
                info = $"✓ 找到教师：{t.Name} ({t.Subject} - {t.SubjectGroup})" + (string.IsNullOrWhiteSpace(subject) ? string.Empty : $"，科目：{subject}");
                return (t, subject, info, new List<Teacher> { t });
            }
            else if (candidates.Count > 1)
            {
                info = $"找到 {candidates.Count} 位候选教师" + (string.IsNullOrWhiteSpace(subject) ? string.Empty : $"（科目：{subject}）");
                return (null, subject, info, candidates);
            }

            // 无教师匹配，仅返回科目
            if (!string.IsNullOrWhiteSpace(subject))
            {
                info = string.IsNullOrWhiteSpace(clsHint)
                    ? $"检测到科目：{subject}，请选择班级"
                    : $"检测到科目：{subject}";
            }
            return (null, subject, info, new List<Teacher>());
        }

        // 按“去除已识别片段”的方式提取原因
        private string ExtractReason(string line, Student? student, string? studentMatchedText, Teacher? teacher, string? subject)
        {
            var result = line;
            var resultPy = PinyinUtil.Full(line);

            // 去掉学号/姓名命中（使用更严格的正则匹配）
            if (student != null)
            {
                if (!string.IsNullOrWhiteSpace(studentMatchedText))
                {
                    var escapedText = Regex.Escape(studentMatchedText);
                    result = Regex.Replace(result, escapedText, string.Empty, RegexOptions.IgnoreCase);
                }

                if (!string.IsNullOrWhiteSpace(student.Id))
                {
                    var escapedId = Regex.Escape(student.Id);
                    result = Regex.Replace(result, escapedId, string.Empty, RegexOptions.IgnoreCase);
                }

                if (!string.IsNullOrWhiteSpace(student.Name))
                {
                    // 使用边界检测的正则匹配删除学生姓名
                    var escapedName = Regex.Escape(student.Name);
                    var namePattern = $@"(?<![一-鿿a-zA-Z0-9]){escapedName}(?![一-鿿a-zA-Z0-9])";
                    result = Regex.Replace(result, namePattern, string.Empty, RegexOptions.IgnoreCase);
                    
                    // 也移除学生姓名的拼音/首字母
                    var snp = PinyinUtil.Full(student.Name);
                    var sni = PinyinUtil.Initials(student.Name);
                    if (!string.IsNullOrWhiteSpace(snp))
                    {
                        var escapedPinyin = Regex.Escape(snp);
                        var pinyinPattern = $@"(?<![a-zA-Z]){escapedPinyin}(?![a-zA-Z])";
                        resultPy = Regex.Replace(resultPy, pinyinPattern, string.Empty, RegexOptions.IgnoreCase);
                        result = Regex.Replace(result, pinyinPattern, string.Empty, RegexOptions.IgnoreCase);
                    }
                    if (!string.IsNullOrWhiteSpace(sni))
                    {
                        var escapedInitials = Regex.Escape(sni);
                        var initialsPattern = $@"(?<![a-zA-Z]){escapedInitials}(?![a-zA-Z])";
                        resultPy = Regex.Replace(resultPy, initialsPattern, string.Empty, RegexOptions.IgnoreCase);
                        result = Regex.Replace(result, initialsPattern, string.Empty, RegexOptions.IgnoreCase);
                    }
                }
            }

            // 仅移除紧邻班号（阿拉伯/中文）且在2个字符内相邻的“班”字；不删除其他位置的“班”
            // 数字班号，如“3班”“10 班”“2-班”均删除整个片段（数字 + 可容忍的间隔 + 班）
            result = Regex.Replace(result, @"(\d{1,3})[\s,，;；·\.、\-]{0,2}班", "$1", RegexOptions.IgnoreCase);
            // 中文数词班号，如“三班”“十 班”
            result = Regex.Replace(result, @"([一二三四五六七八九十两]{1,3})[\s,，;；·\.、\-]{0,2}班", "$1", RegexOptions.IgnoreCase);

            // 去掉教师姓名（中文命中，使用边界检测）
            if (teacher != null && !string.IsNullOrWhiteSpace(teacher.Name))
            {
                var escapedTeacherName = Regex.Escape(teacher.Name);
                var teacherNamePattern = $@"(?<![一-鿿a-zA-Z0-9]){escapedTeacherName}(?![一-鿿a-zA-Z0-9])";
                result = Regex.Replace(result, teacherNamePattern, string.Empty, RegexOptions.IgnoreCase);
                
                // 同时移除教师姓名拼音
                var tnp = PinyinUtil.Full(teacher.Name);
                var tni = PinyinUtil.Initials(teacher.Name);
                if (!string.IsNullOrWhiteSpace(tnp))
                {
                    var escapedTeacherPinyin = Regex.Escape(tnp);
                    var teacherPinyinPattern = $@"(?<![a-zA-Z]){escapedTeacherPinyin}(?![a-zA-Z])";
                    resultPy = Regex.Replace(resultPy, teacherPinyinPattern, string.Empty, RegexOptions.IgnoreCase);
                    result = Regex.Replace(result, teacherPinyinPattern, string.Empty, RegexOptions.IgnoreCase);
                }
                if (!string.IsNullOrWhiteSpace(tni))
                {
                    var escapedTeacherInitials = Regex.Escape(tni);
                    var teacherInitialsPattern = $@"(?<![a-zA-Z]){escapedTeacherInitials}(?![a-zA-Z])";
                    resultPy = Regex.Replace(resultPy, teacherInitialsPattern, string.Empty, RegexOptions.IgnoreCase);
                    result = Regex.Replace(result, teacherInitialsPattern, string.Empty, RegexOptions.IgnoreCase);
                }
            }

            // 去掉科目关键词（使用边界检测，避免误删）
            if (!string.IsNullOrWhiteSpace(subject))
            {
                // 与 DetectSubjectFromInput 同步的一些关键词
                var subjectTokens = new[] { "语文","数学","英语","物理","化学","生物","政治","历史","地理","体育","音乐","美术","信息技术","计算机","信息","科学",
                    "chinese","yw","yuwen","math","mathematics","sx","shuxue","english","yy","yingyu","physics","wl","wuli","chemistry","hx","huaxue","biology","sw","shengwu","politics","zz","zhengzhi","history","ls","lishi","geography","dl","dili","pe","sports","ty","tiyu","music","yl","yinyue","art","ms","meishu","xxjs","jisuan","science","kx","kexue" };
                foreach (var tok in subjectTokens)
                {
                    bool isAscii = tok.All(ch => ch < 128);
                    if (isAscii)
                    {
                        // 英文科目词使用单词边界
                        var asciiPattern = $@"\b{Regex.Escape(tok)}\b";
                        result = Regex.Replace(result, asciiPattern, string.Empty, RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        // 中文科目词使用中文边界检测
                        var escapedTok = Regex.Escape(tok);
                        var chinesePattern = $@"(?<![一-鿿a-zA-Z0-9]){escapedTok}(?![一-鿿a-zA-Z0-9])";
                        result = Regex.Replace(result, chinesePattern, string.Empty, RegexOptions.IgnoreCase);
                    }
                }
            }

            // 清理多余空白与分隔符
            result = result.Replace("，", " ").Replace(",", " ").Replace("；", " ").Replace(";", " ");
            result = Regex.Replace(result, "\n|\r", " ");
            result = Regex.Replace(result, @"\s+", " ").Trim();
            return result;
        }

    private async void AutoFillCurrent()
        {
            _inAutoFill = true;
            try
            {
            var line = CurrentLineText;
            if (string.IsNullOrWhiteSpace(line)) return;

            var (student, idx, matched, candidates) = FindBestMatch(line);
            var classHint = ExtractClassFromText(line);
            // 先行解析教师/科目（即便学生未命中也可解析）
            var (teacher, subject, teacherInfo, teacherCandidates) = FindTeacherFromLine(line);
            // 不弹窗：若教师候选较多，仅提示信息，由用户选择班级后再自动匹配
            if (student != null)
            {
                NameInput = student.Name;
                ClassInput = string.IsNullOrWhiteSpace(classHint) ? student.Class : classHint;
                StudentIdInput = student.Id;
                // 使用整行文字作为原因，不去除已识别的信息
                ReasonInput = line;
                MatchedStudentInfo = $"✓ 找到学生：{student.Name} ({ClassInput}) - 来自当前处理行";

                // 教师/科目填充（如果已识别）
                if (teacher != null || !string.IsNullOrWhiteSpace(subject))
                {
                    if (teacher != null && !string.IsNullOrWhiteSpace(subject))
                        TeacherInput = teacher.Name; // 只填老师名字
                    else if (teacher != null)
                        TeacherInput = teacher.Name;
                    // 只有科目时，不填 TeacherInput

                    MatchedTeacherInfo = teacherInfo;
                }
                return;
            }

            if (candidates.Count > 1)
            {
                // 仅在已知班级时弹窗选择；否则提示“请选择班级”
                var ctxClass = !string.IsNullOrWhiteSpace(classHint) ? classHint : (string.IsNullOrWhiteSpace(ClassInput) ? null : ClassInput);
                if (string.IsNullOrWhiteSpace(ctxClass))
                {
                    MatchedStudentInfo = "已识别姓名拼音，请选择班级";
                    return;
                }

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
                        MatchedStudentInfo = $"✓ 找到学生：{chosen.Name} ({ClassInput}) - 来自当前处理行";
                    }
                }
            }
            else
            {
                // 学生未匹配时，仍可设置教师/科目，并把其余作为原因
                if (teacher != null || !string.IsNullOrWhiteSpace(subject))
                {
                    if (teacher != null && !string.IsNullOrWhiteSpace(subject))
                        TeacherInput = teacher.Name; // 只填老师名字
                    else if (teacher != null)
                        TeacherInput = teacher.Name;
                    // 只有科目时不填 TeacherInput
                    MatchedTeacherInfo = teacherInfo;
                }

                ReasonInput = line; // 使用整行文字作为原因，不去除已识别的信息
                // 没有匹配到学生
                MatchedStudentInfo = $"⚠ 未在当前处理行中匹配到学生";
            }
        }
            finally
            {
                _inAutoFill = false;
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

            var remark = string.IsNullOrWhiteSpace(ReasonInput) ? line : ReasonInput; // 使用整行文字作为备注
            
            // 添加到待处理列表（暂不分配给特定学生）
            var store = new EvaluationStore(_baseDir);
            var all = store.Load();
                all.Add(new EvaluationEntry
            {
                Class = ClassInput,
                Name = NameInput, // 可能为空，等待后续处理
                Remark = remark,
                Score = score,
                    Date = DateTime.Now, // 使用本地当前时间
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
                Logger.LogDebug("NextAsync started", "InfoEntryViewModel");
                
                var line = CurrentLineText;
                if (string.IsNullOrWhiteSpace(line)) 
                { 
                    Advance(); 
                    AutoFillCurrent(); 
                    Logger.LogDebug("Current line is empty, skipping", "InfoEntryViewModel");
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

                var remark = string.IsNullOrWhiteSpace(ReasonInput) ? line : ReasonInput; // 使用整行文字作为备注

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

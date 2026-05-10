using System.Text;
using ShortestPathFinder.Algorithms;
using ShortestPathFinder.Controls;
using ShortestPathFinder.Models;

namespace ShortestPathFinder.Forms
{

    public class MainForm : Form
    {

        private GraphCanvas _canvas;
        private MenuStrip _menu;
        private ToolStrip _toolbar;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private Panel _sidePanel;
        private Label _lblStart;
        private Label _lblEnd;
        private CheckBox _chkDirected;
        private ComboBox _cmbAlgorithm;
        private Button _btnRun;
        private Button _btnRunAll;
        private TextBox _txtLog;

        private Graph _graph = new Graph();
        private List<AlgorithmResult> _lastResults = new List<AlgorithmResult>();

        public MainForm()
        {
            Text = "Пошук найкоротшого шляху – Курсова робота, варіант 12";
            Width = 1280;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 650);

            BuildMenu();
            BuildToolbar();
            BuildSidePanel();
            BuildStatusBar();
            BuildCanvas();

            UpdateStartEndLabels();
        }

        private void BuildMenu()
        {
            _menu = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("&Файл");
            fileMenu.DropDownItems.Add("&Новий граф", null, (s, e) => NewGraph());
            fileMenu.DropDownItems.Add("&Завантажити граф...", null, (s, e) => LoadGraph());
            fileMenu.DropDownItems.Add("&Зберегти граф...", null, (s, e) => SaveGraph());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("&Експорт результатів...", null, (s, e) => ExportResults());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("&Вихід", null, (s, e) => Close());

            var graphMenu = new ToolStripMenuItem("&Граф");
            graphMenu.DropDownItems.Add("Створити &випадковий граф...", null, (s, e) => GenerateRandom());
            graphMenu.DropDownItems.Add("Завантажити &приклад", null, (s, e) => LoadExample());
            graphMenu.DropDownItems.Add(new ToolStripSeparator());
            graphMenu.DropDownItems.Add("О&чистити підсвічування", null, (s, e) =>
            {
                _canvas.ClearHighlight();
                Log("Підсвічування очищено.");
            });

            var algoMenu = new ToolStripMenuItem("&Алгоритми");
            algoMenu.DropDownItems.Add("Запустити обраний", null, (s, e) => RunSelected());
            algoMenu.DropDownItems.Add("Запустити &всі та порівняти", null, (s, e) => RunAllAndCompare());
            algoMenu.DropDownItems.Add(new ToolStripSeparator());
            algoMenu.DropDownItems.Add("Показати останні результати", null, (s, e) => ShowResults());

            var helpMenu = new ToolStripMenuItem("&Допомога");
            helpMenu.DropDownItems.Add("&Інструкція користувача", null, (s, e) => ShowHelp());
            helpMenu.DropDownItems.Add("&Про програму", null, (s, e) => ShowAbout());

            _menu.Items.AddRange(new ToolStripItem[] { fileMenu, graphMenu, algoMenu, helpMenu });
            MainMenuStrip = _menu;
            Controls.Add(_menu);
        }

        private void BuildToolbar()
        {
            _toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };

            void AddModeButton(string text, CanvasMode mode)
            {
                var btn = new ToolStripButton(text)
                {
                    DisplayStyle = ToolStripItemDisplayStyle.Text,
                    Tag = mode
                };
                btn.Click += (s, e) =>
                {
                    _canvas.Mode = mode;
                    foreach (var item in _toolbar.Items)
                        if (item is ToolStripButton b && b.Tag is CanvasMode)
                            b.Checked = b == btn;
                    SetStatus($"Режим: {text}");
                };
                _toolbar.Items.Add(btn);
            }

            AddModeButton("➕ Додати вершину", CanvasMode.AddVertex);
            AddModeButton("🔗 Додати ребро", CanvasMode.AddEdge);
            AddModeButton("🗑 Видалити", CanvasMode.Delete);
            AddModeButton("✋ Перемістити", CanvasMode.Move);
            _toolbar.Items.Add(new ToolStripSeparator());
            AddModeButton("🟢 Стартова", CanvasMode.SelectStart);
            AddModeButton("🔴 Кінцева", CanvasMode.SelectEnd);
            _toolbar.Items.Add(new ToolStripSeparator());

            var btnClear = new ToolStripButton("🧹 Очистити підсвічування")
            { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnClear.Click += (s, e) => _canvas.ClearHighlight();
            _toolbar.Items.Add(btnClear);

            ((ToolStripButton)_toolbar.Items[0]).Checked = true;

            Controls.Add(_toolbar);
        }

        private void BuildSidePanel()
        {
            _sidePanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 320,
                BackColor = SystemColors.Control,
                Padding = new Padding(10)
            };

            int y = 10;

            void AddHeader(string text)
            {
                var lbl = new Label
                {
                    Text = text,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Left = 10,
                    Top = y,
                    Width = 290
                };
                _sidePanel.Controls.Add(lbl);
                y += 26;
            }

            AddHeader("Налаштування графа");
            _chkDirected = new CheckBox
            {
                Text = "Орієнтовані ребра (для нових)",
                Left = 10,
                Top = y,
                Width = 290,
                Checked = false
            };
            _chkDirected.CheckedChanged += (s, e) => _canvas.DirectedMode = _chkDirected.Checked;
            _sidePanel.Controls.Add(_chkDirected);
            y += 30;

            AddHeader("Стартова та кінцева вершини");
            _lblStart = new Label
            {
                Text = "🟢 Стартова: (не вибрано)",
                Left = 10, Top = y, Width = 290
            };
            _sidePanel.Controls.Add(_lblStart); y += 22;
            _lblEnd = new Label
            {
                Text = "🔴 Кінцева: (не вибрано)",
                Left = 10, Top = y, Width = 290
            };
            _sidePanel.Controls.Add(_lblEnd); y += 30;

            AddHeader("Алгоритм пошуку");
            _cmbAlgorithm = new ComboBox
            {
                Left = 10, Top = y, Width = 290,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbAlgorithm.Items.AddRange(new object[] { "Дейкстра", "Беллман-Форд", "A*" });
            _cmbAlgorithm.SelectedIndex = 0;
            _sidePanel.Controls.Add(_cmbAlgorithm);
            y += 30;

            _btnRun = new Button
            {
                Text = "▶ Запустити обраний",
                Left = 10, Top = y, Width = 290, Height = 30,
                BackColor = Color.LightSteelBlue
            };
            _btnRun.Click += (s, e) => RunSelected();
            _sidePanel.Controls.Add(_btnRun);
            y += 36;

            _btnRunAll = new Button
            {
                Text = "📊 Запустити всі та порівняти",
                Left = 10, Top = y, Width = 290, Height = 30,
                BackColor = Color.LightGreen
            };
            _btnRunAll.Click += (s, e) => RunAllAndCompare();
            _sidePanel.Controls.Add(_btnRunAll);
            y += 36;

            AddHeader("Журнал подій");
            _txtLog = new TextBox
            {
                Left = 10, Top = y,
                Width = 290,
                Height = _sidePanel.Height - y - 20,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Consolas", 9f),
                BackColor = Color.White
            };
            _sidePanel.Controls.Add(_txtLog);

            Controls.Add(_sidePanel);
        }

        private void BuildStatusBar()
        {
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Готово");
            _statusStrip.Items.Add(_statusLabel);
            Controls.Add(_statusStrip);
        }

        private void BuildCanvas()
        {
            _canvas = new GraphCanvas
            {
                Dock = DockStyle.Fill,
                Graph = _graph
            };
            _canvas.GraphChanged += (s, e) =>
                SetStatus($"Граф: {_graph.Vertices.Count} вершин, {_graph.Edges.Count} ребер");
            _canvas.StartEndChanged += (s, e) => UpdateStartEndLabels();
            Controls.Add(_canvas);
            _canvas.BringToFront();
        }

        private void UpdateStartEndLabels()
        {
            _lblStart.Text = $"🟢 Стартова: {(_canvas?.StartVertex?.Label ?? "(не вибрано)")}";
            _lblEnd.Text = $"🔴 Кінцева: {(_canvas?.EndVertex?.Label ?? "(не вибрано)")}";
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null) _statusLabel.Text = text;
        }

        private void Log(string text)
        {
            _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\r\n");
        }

        private void NewGraph()
        {
            if (_graph.Vertices.Count > 0)
            {
                var ok = MessageBox.Show("Створити новий граф? Поточний граф буде втрачено.",
                                         "Підтвердження", MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question);
                if (ok != DialogResult.Yes) return;
            }
            _graph.Clear();
            _canvas.ClearStartEnd();
            _canvas.ClearHighlight();
            _canvas.Invalidate();
            Log("Створено новий граф.");
            SetStatus("Готово");
        }

        private void SaveGraph()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Файли графа (*.graph)|*.graph|Текстові файли (*.txt)|*.txt",
                FileName = "graph.graph"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# Shortest Path Finder graph file");
                sb.AppendLine($"VERTICES {_graph.Vertices.Count}");
                foreach (var v in _graph.Vertices)
                {
                    sb.AppendLine($"V {v.Id} {v.Label} {v.X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {v.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                }
                sb.AppendLine($"EDGES {_graph.Edges.Count}");
                foreach (var e in _graph.Edges)
                {
                    sb.AppendLine($"E {e.FromId} {e.ToId} {e.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture)} {(e.IsDirected ? 1 : 0)}");
                }
                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                Log($"Граф збережено: {sfd.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadGraph()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Файли графа (*.graph)|*.graph|Текстові файли (*.txt)|*.txt|Усі файли|*.*"
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var lines = File.ReadAllLines(ofd.FileName);
                _graph.Clear();
                _canvas.ClearStartEnd();

                int maxId = -1;
                var ci = System.Globalization.CultureInfo.InvariantCulture;

                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.StartsWith("#") || line.Length == 0) continue;
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts[0] == "V" && parts.Length >= 5)
                    {
                        int id = int.Parse(parts[1]);
                        string label = parts[2];
                        float x = float.Parse(parts[3], ci);
                        float y = float.Parse(parts[4], ci);
                        var v = new Vertex(id, label, x, y);
                        _graph.Vertices.Add(v);
                        if (id > maxId) maxId = id;
                    }
                    else if (parts[0] == "E" && parts.Length >= 5)
                    {
                        int from = int.Parse(parts[1]);
                        int to = int.Parse(parts[2]);
                        double w = double.Parse(parts[3], ci);
                        bool dir = parts[4] == "1";
                        _graph.Edges.Add(new Edge(from, to, w, dir));
                    }
                }

                EnsureIdCounter(maxId + 1);

                _canvas.Invalidate();
                Log($"Граф завантажено: {ofd.FileName} ({_graph.Vertices.Count} вершин, {_graph.Edges.Count} ребер)");
                SetStatus("Граф завантажено");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження: {ex.Message}", "Помилка",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnsureIdCounter(int desired)
        {

            while (true)
            {
                var temp = _graph.AddVertex(-1000, -1000);
                if (temp.Id >= desired)
                {
                    _graph.RemoveVertex(temp);
                    break;
                }
                _graph.RemoveVertex(temp);
            }
        }

        private void ExportResults()
        {
            if (_lastResults.Count == 0)
            {
                MessageBox.Show("Спочатку запустіть алгоритми.", "Немає результатів",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var sfd = new SaveFileDialog
            {
                Filter = "Текстові файли (*.txt)|*.txt",
                FileName = "results.txt"
            };
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var sb = new StringBuilder();
            sb.AppendLine("Результати пошуку найкоротшого шляху");
            sb.AppendLine($"Дата: {DateTime.Now}");
            sb.AppendLine($"Граф: {_graph.Vertices.Count} вершин, {_graph.Edges.Count} ребер");
            sb.AppendLine();
            foreach (var r in _lastResults)
            {
                sb.AppendLine($"=== {r.AlgorithmName} ===");
                sb.AppendLine($"Статус: {r.Message}");
                if (r.Success)
                {
                    var labels = r.Path.Select(id => _graph.GetVertexById(id)?.Label ?? "?");
                    sb.AppendLine($"Шлях: {string.Join(" -> ", labels)}");
                    sb.AppendLine($"Довжина: {r.TotalDistance:F4}");
                }
                sb.AppendLine($"Відвідано вершин: {r.VisitedVertices.Count}");
                sb.AppendLine($"Операцій: {r.OperationCount}");
                sb.AppendLine($"Час: {r.ElapsedMilliseconds:F4} мс");
                sb.AppendLine();
            }
            try
            {
                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                Log($"Результати експортовано: {sfd.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerateRandom()
        {
            using var dlg = new Form
            {
                Text = "Випадковий граф",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(280, 170),
                MaximizeBox = false,
                MinimizeBox = false
            };
            var lblV = new Label { Text = "Вершин (3-30):", Left = 12, Top = 12, Width = 120 };
            var numV = new NumericUpDown { Left = 140, Top = 10, Width = 120, Minimum = 3, Maximum = 30, Value = 8 };
            var lblE = new Label { Text = "Ребер:", Left = 12, Top = 42, Width = 120 };
            var numE = new NumericUpDown { Left = 140, Top = 40, Width = 120, Minimum = 2, Maximum = 200, Value = 12 };
            var lblW = new Label { Text = "Макс. вага:", Left = 12, Top = 72, Width = 120 };
            var numW = new NumericUpDown { Left = 140, Top = 70, Width = 120, Minimum = 1, Maximum = 1000, Value = 20 };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 100, Top = 110, Width = 75 };
            var cancel = new Button { Text = "Скасувати", DialogResult = DialogResult.Cancel, Left = 185, Top = 110, Width = 80 };
            dlg.Controls.AddRange(new Control[] { lblV, numV, lblE, numE, lblW, numW, ok, cancel });
            dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            if (dlg.ShowDialog() != DialogResult.OK) return;

            int vCount = (int)numV.Value;
            int eCount = (int)numE.Value;
            int maxW = (int)numW.Value;

            _graph.Clear();
            _canvas.ClearStartEnd();
            _canvas.ClearHighlight();

            float cx = _canvas.Width / 2f;
            float cy = _canvas.Height / 2f;
            float r = Math.Min(cx, cy) - 60;
            var rnd = new Random();

            for (int i = 0; i < vCount; i++)
            {
                double angle = 2 * Math.PI * i / vCount;
                float x = cx + (float)(r * Math.Cos(angle));
                float y = cy + (float)(r * Math.Sin(angle));
                _graph.AddVertex(x, y);
            }

            var indices = Enumerable.Range(0, vCount).OrderBy(_ => rnd.Next()).ToList();
            for (int i = 1; i < indices.Count; i++)
            {
                int a = indices[i];
                int b = indices[rnd.Next(i)];
                double w = rnd.Next(1, maxW + 1);
                _graph.AddOrUpdateEdge(_graph.Vertices[a], _graph.Vertices[b], w, false);
            }

            int safety = 0;
            while (_graph.Edges.Count < eCount && safety++ < eCount * 10)
            {
                int a = rnd.Next(vCount);
                int b = rnd.Next(vCount);
                if (a == b) continue;
                double w = rnd.Next(1, maxW + 1);
                _graph.AddOrUpdateEdge(_graph.Vertices[a], _graph.Vertices[b], w, false);
            }

            _canvas.Invalidate();
            Log($"Згенеровано випадковий граф: {vCount} вершин, {_graph.Edges.Count} ребер");
            SetStatus($"Випадковий граф: {vCount} вершин");
        }

        private void LoadExample()
        {
            _graph.Clear();
            _canvas.ClearStartEnd();
            _canvas.ClearHighlight();

            float cx = _canvas.Width / 2f;
            float cy = _canvas.Height / 2f;
            float r = 200;
            var positions = new (float, float)[]
            {
                (cx - r, cy - r/2),
                (cx,     cy - r),
                (cx + r, cy - r/2),
                (cx + r, cy + r/2),
                (cx,     cy + r),
                (cx - r, cy + r/2)
            };
            foreach (var (x, y) in positions)
                _graph.AddVertex(x, y);

            var v = _graph.Vertices;

            _graph.AddOrUpdateEdge(v[0], v[1], 4, false);
            _graph.AddOrUpdateEdge(v[0], v[5], 8, false);
            _graph.AddOrUpdateEdge(v[1], v[2], 8, false);
            _graph.AddOrUpdateEdge(v[1], v[5], 11, false);
            _graph.AddOrUpdateEdge(v[2], v[3], 7, false);
            _graph.AddOrUpdateEdge(v[2], v[4], 4, false);
            _graph.AddOrUpdateEdge(v[3], v[4], 9, false);
            _graph.AddOrUpdateEdge(v[3], v[5], 14, false);
            _graph.AddOrUpdateEdge(v[4], v[5], 10, false);

            _canvas.Invalidate();
            Log("Завантажено демонстраційний приклад (6 вершин).");
            SetStatus("Демонстраційний граф завантажено");
        }

        private void RunSelected()
        {
            if (!ValidateRun()) return;

            IShortestPathAlgorithm algo = _cmbAlgorithm.SelectedIndex switch
            {
                0 => new DijkstraAlgorithm(),
                1 => new BellmanFordAlgorithm(),
                2 => new AStarAlgorithm(),
                _ => new DijkstraAlgorithm()
            };

            var result = algo.FindPath(_graph, _canvas.StartVertex.Id, _canvas.EndVertex.Id);
            _lastResults = new List<AlgorithmResult> { result };
            ApplyResultToCanvas(result);
            Log($"{algo.Name}: {result.Message}");
            if (result.Success)
            {
                var labels = result.Path.Select(id => _graph.GetVertexById(id)?.Label ?? "?");
                Log($"   Шлях: {string.Join(" → ", labels)}");
                Log($"   Час: {result.ElapsedMilliseconds:F4} мс, операцій: {result.OperationCount}");
            }
            SetStatus($"{algo.Name}: {result.Message}");
        }

        private void RunAllAndCompare()
        {
            if (!ValidateRun()) return;

            var algos = new IShortestPathAlgorithm[]
            {
                new DijkstraAlgorithm(),
                new BellmanFordAlgorithm(),
                new AStarAlgorithm()
            };

            var results = new List<AlgorithmResult>();
            foreach (var a in algos)
            {
                var r = a.FindPath(_graph, _canvas.StartVertex.Id, _canvas.EndVertex.Id);
                results.Add(r);
                Log($"{a.Name}: {r.Message} ({r.ElapsedMilliseconds:F4} мс)");
            }

            _lastResults = results;

            var firstOk = results.FirstOrDefault(r => r.Success);
            if (firstOk != null) ApplyResultToCanvas(firstOk);

            using var form = new ResultsForm(results, _graph);
            form.ShowDialog(this);
        }

        private void ShowResults()
        {
            if (_lastResults.Count == 0)
            {
                MessageBox.Show("Спочатку запустіть алгоритми.", "Інформація",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var form = new ResultsForm(_lastResults, _graph);
            form.ShowDialog(this);
        }

        private bool ValidateRun()
        {
            if (_graph.Vertices.Count == 0)
            {
                MessageBox.Show("Граф порожній. Додайте вершини та ребра.", "Помилка",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (_canvas.StartVertex == null || _canvas.EndVertex == null)
            {
                MessageBox.Show("Виберіть стартову та кінцеву вершини (кнопки 🟢/🔴 на панелі).",
                                "Не вибрано вершини", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (_canvas.StartVertex == _canvas.EndVertex)
            {
                MessageBox.Show("Стартова і кінцева вершини збігаються.", "Помилка",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void ApplyResultToCanvas(AlgorithmResult result)
        {
            _canvas.HighlightedPath = result.Path ?? new List<int>();
            _canvas.VisitedVertices = result.VisitedVertices ?? new HashSet<int>();
            _canvas.Invalidate();
        }

        private void ShowHelp()
        {
            string help =
@"ІНСТРУКЦІЯ КОРИСТУВАЧА

1. Створення графа:
   • '➕ Додати вершину' — клік по вільному місцю на полотні.
   • '🔗 Додати ребро' — клік по двом вершинам, у діалозі ввести вагу.
   • '🗑 Видалити' — клік по вершині або ребру.
   • '✋ Перемістити' — перетягування вершин мишкою.
   • Подвійний клік по ребру — змінити його вагу.

2. Вибір вершин для пошуку:
   • '🟢 Стартова' — клік по обраній вершині.
   • '🔴 Кінцева' — клік по обраній вершині.

3. Запуск алгоритмів:
   • Виберіть алгоритм у списку та натисніть '▶ Запустити обраний'.
   • Або натисніть '📊 Запустити всі та порівняти' — це викличе всі
     три алгоритми та покаже діаграми порівняння часу та операцій.

4. Меню 'Граф' містить функції генерації випадкового графа та
   завантаження демонстраційного прикладу.

5. Меню 'Файл' дозволяє зберегти/завантажити граф (.graph) та
   експортувати результати у .txt.

Налаштування 'Орієнтовані ребра' застосовується до НОВИХ ребер.";
            MessageBox.Show(help, "Інструкція користувача",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowAbout()
        {
            string text =
@"Пошук найкоротшого шляху у графі
Курсова робота з дисципліни 'Основи програмування'
Варіант 12

Реалізовані алгоритми:
  • Алгоритм Дейкстри
  • Алгоритм Беллмана-Форда
  • Алгоритм A* (евклідова евристика)

Технологія: C# / .NET 6 / WinForms
Київ, 2026";
            MessageBox.Show(text, "Про програму",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

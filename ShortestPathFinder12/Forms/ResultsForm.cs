using ShortestPathFinder.Models;

namespace ShortestPathFinder.Forms
{

    public class ResultsForm : Form
    {
        private readonly List<AlgorithmResult> _results;
        private readonly Graph _graph;

        public ResultsForm(List<AlgorithmResult> results, Graph graph)
        {
            _results = results ?? new List<AlgorithmResult>();
            _graph = graph;

            Text = "Порівняльний аналіз алгоритмів";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(700, 500);

            BuildUI();
        }

        private void BuildUI()
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 240
            };
            Controls.Add(split);

            var txt = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 10f),
                Text = BuildReportText()
            };
            split.Panel1.Controls.Add(txt);

            var bottomPanel = new Panel { Dock = DockStyle.Fill };

            var chartPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            chartPanel.Paint += ChartPanel_Paint;
            bottomPanel.Controls.Add(chartPanel);

            var btnSave = new Button
            {
                Text = "Зберегти у файл...",
                Dock = DockStyle.Bottom,
                Height = 32
            };
            btnSave.Click += (s, e) => SaveToFile(txt.Text);
            bottomPanel.Controls.Add(btnSave);

            split.Panel2.Controls.Add(bottomPanel);
        }

        private string BuildReportText()
        {
            using var sw = new StringWriter();
            sw.WriteLine("====== РЕЗУЛЬТАТИ ПОШУКУ НАЙКОРОТШОГО ШЛЯХУ ======");
            sw.WriteLine();
            sw.WriteLine($"Кількість вершин у графі: {_graph.Vertices.Count}");
            sw.WriteLine($"Кількість ребер у графі:  {_graph.Edges.Count}");
            sw.WriteLine();

            foreach (var r in _results)
            {
                sw.WriteLine($"--- {r.AlgorithmName} ---");
                sw.WriteLine($"  Статус:               {r.Message}");
                if (r.Success)
                {
                    var labels = r.Path.Select(id => _graph.GetVertexById(id)?.Label ?? "?").ToList();
                    sw.WriteLine($"  Шлях:                 {string.Join(" -> ", labels)}");
                    sw.WriteLine($"  Загальна вага шляху:  {r.TotalDistance:F4}");
                    sw.WriteLine($"  Кількість вершин у шляху: {r.Path.Count}");
                }
                sw.WriteLine($"  Відвідано вершин:     {r.VisitedVertices.Count}");
                sw.WriteLine($"  Кількість операцій:   {r.OperationCount}");
                sw.WriteLine($"  Час виконання:        {r.ElapsedMilliseconds:F4} мс");
                sw.WriteLine();
            }

            sw.WriteLine("====== ПОРІВНЯЛЬНИЙ АНАЛІЗ ======");
            var successful = _results.Where(r => r.Success).ToList();
            if (successful.Count > 1)
            {
                var fastestTime = successful.OrderBy(r => r.ElapsedMilliseconds).First();
                var fewestOps = successful.OrderBy(r => r.OperationCount).First();
                sw.WriteLine($"Найшвидший за часом: {fastestTime.AlgorithmName} ({fastestTime.ElapsedMilliseconds:F4} мс)");
                sw.WriteLine($"Найменше операцій:   {fewestOps.AlgorithmName} ({fewestOps.OperationCount})");

                var distances = successful.Select(r => r.TotalDistance).Distinct().ToList();
                if (distances.Count == 1)
                    sw.WriteLine("Усі алгоритми знайшли однакову довжину шляху ✓");
                else
                    sw.WriteLine("УВАГА: алгоритми знайшли РІЗНІ довжини шляхів!");
            }

            return sw.ToString();
        }

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            var panel = (Panel)sender;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            if (_results.Count == 0) return;

            int half = panel.Width / 2;
            DrawBarChart(g, new Rectangle(0, 0, half, panel.Height),
                         "Час виконання (мс)",
                         _results.Select(r => (r.AlgorithmName, r.ElapsedMilliseconds)).ToList(),
                         Color.SteelBlue);
            DrawBarChart(g, new Rectangle(half, 0, half, panel.Height),
                         "Кількість операцій",
                         _results.Select(r => (r.AlgorithmName, (double)r.OperationCount)).ToList(),
                         Color.MediumSeaGreen);
        }

        private void DrawBarChart(Graphics g, Rectangle area, string title,
                                  List<(string label, double value)> data, Color barColor)
        {
            using var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var labelFont = new Font("Segoe UI", 9f);
            using var valueFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var blackBrush = new SolidBrush(Color.Black);
            using var barBrush = new SolidBrush(barColor);
            using var axisPen = new Pen(Color.DimGray, 1.5f);

            var titleSize = g.MeasureString(title, titleFont);
            g.DrawString(title, titleFont, blackBrush,
                         area.Left + (area.Width - titleSize.Width) / 2, area.Top + 8);

            int chartTop = area.Top + 36;
            int chartBottom = area.Bottom - 60;
            int chartLeft = area.Left + 50;
            int chartRight = area.Right - 20;

            g.DrawLine(axisPen, chartLeft, chartTop, chartLeft, chartBottom);
            g.DrawLine(axisPen, chartLeft, chartBottom, chartRight, chartBottom);

            if (data.Count == 0) return;
            double maxVal = data.Max(d => d.value);
            if (maxVal <= 0) maxVal = 1;

            int chartWidth = chartRight - chartLeft;
            int chartHeight = chartBottom - chartTop;
            int barWidth = (int)(chartWidth / (double)data.Count * 0.65);
            int gap = (chartWidth - barWidth * data.Count) / (data.Count + 1);

            int x = chartLeft + gap;
            foreach (var (label, value) in data)
            {
                int h = (int)(chartHeight * (value / maxVal));
                if (h < 1) h = 1;
                var barRect = new Rectangle(x, chartBottom - h, barWidth, h);
                g.FillRectangle(barBrush, barRect);
                g.DrawRectangle(Pens.Black, barRect);

                string valStr = value < 0.01 ? value.ToString("0.####") : value.ToString("0.###");
                var valSize = g.MeasureString(valStr, valueFont);
                g.DrawString(valStr, valueFont, blackBrush,
                             x + (barWidth - valSize.Width) / 2, chartBottom - h - 16);

                var labSize = g.MeasureString(label, labelFont);
                g.DrawString(label, labelFont, blackBrush,
                             x + (barWidth - labSize.Width) / 2, chartBottom + 6);

                x += barWidth + gap;
            }
        }

        private void SaveToFile(string content)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Текстові файли (*.txt)|*.txt|Усі файли (*.*)|*.*",
                FileName = "shortest_path_results.txt",
                Title = "Збереження результатів"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, content, System.Text.Encoding.UTF8);
                    MessageBox.Show($"Результати збережено у:\n{sfd.FileName}", "Готово",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не вдалося зберегти файл:\n{ex.Message}", "Помилка",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

using ShortestPathFinder.Models;

namespace ShortestPathFinder.Controls
{

    public enum CanvasMode
    {

        AddVertex,

        AddEdge,

        Delete,

        Move,

        SelectStart,

        SelectEnd
    }

    public class GraphCanvas : UserControl
    {

        private Graph _graph;
        private CanvasMode _mode = CanvasMode.AddVertex;

        public Graph Graph
        {
            get => _graph;
            set { _graph = value; Invalidate(); }
        }

        public CanvasMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                _firstSelected = null;
                Cursor = (value == CanvasMode.Move) ? Cursors.SizeAll : Cursors.Cross;
            }
        }

        public Vertex StartVertex { get; private set; }

        public Vertex EndVertex { get; private set; }

        public List<int> HighlightedPath { get; set; } = new List<int>();

        public HashSet<int> VisitedVertices { get; set; } = new HashSet<int>();

        public bool DirectedMode { get; set; } = false;

        public event EventHandler GraphChanged;

        public event EventHandler StartEndChanged;

        private Vertex _firstSelected;
        private Vertex _draggedVertex;
        private bool _isDragging;
        private float _lastMouseX, _lastMouseY;

        public GraphCanvas()
        {

            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
            _graph = new Graph();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_graph == null) return;

            var vertex = _graph.FindVertexAt(e.X, e.Y);

            switch (_mode)
            {
                case CanvasMode.AddVertex:
                    if (vertex == null)
                    {
                        _graph.AddVertex(e.X, e.Y);
                        GraphChanged?.Invoke(this, EventArgs.Empty);
                        Invalidate();
                    }
                    else if (e.Button == MouseButtons.Left)
                    {

                        _draggedVertex = vertex;
                        _isDragging = true;
                        _lastMouseX = e.X;
                        _lastMouseY = e.Y;
                    }
                    break;

                case CanvasMode.AddEdge:
                    if (vertex != null)
                    {
                        if (_firstSelected == null)
                        {
                            _firstSelected = vertex;
                        }
                        else if (_firstSelected.Id != vertex.Id)
                        {

                            double weight = AskForWeight();
                            if (!double.IsNaN(weight))
                            {
                                _graph.AddOrUpdateEdge(_firstSelected, vertex, weight, DirectedMode);
                                GraphChanged?.Invoke(this, EventArgs.Empty);
                            }
                            _firstSelected = null;
                        }
                        Invalidate();
                    }
                    break;

                case CanvasMode.Delete:
                    if (vertex != null)
                    {
                        if (StartVertex == vertex) StartVertex = null;
                        if (EndVertex == vertex) EndVertex = null;
                        _graph.RemoveVertex(vertex);
                        GraphChanged?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        var edge = _graph.FindEdgeAt(e.X, e.Y);
                        if (edge != null)
                        {
                            _graph.RemoveEdge(edge);
                            GraphChanged?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    Invalidate();
                    break;

                case CanvasMode.Move:
                    if (vertex != null)
                    {
                        _draggedVertex = vertex;
                        _isDragging = true;
                        _lastMouseX = e.X;
                        _lastMouseY = e.Y;
                    }
                    break;

                case CanvasMode.SelectStart:
                    if (vertex != null)
                    {
                        StartVertex = vertex;
                        StartEndChanged?.Invoke(this, EventArgs.Empty);
                        Invalidate();
                    }
                    break;

                case CanvasMode.SelectEnd:
                    if (vertex != null)
                    {
                        EndVertex = vertex;
                        StartEndChanged?.Invoke(this, EventArgs.Empty);
                        Invalidate();
                    }
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging && _draggedVertex != null)
            {
                _draggedVertex.X = e.X;
                _draggedVertex.Y = e.Y;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
            _draggedVertex = null;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (_graph == null) return;

            var edge = _graph.FindEdgeAt(e.X, e.Y);
            if (edge != null && _graph.FindVertexAt(e.X, e.Y) == null)
            {
                double w = AskForWeight(edge.Weight);
                if (!double.IsNaN(w))
                {
                    edge.Weight = w;
                    GraphChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_graph == null) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var pathEdges = new HashSet<(int, int)>();
            for (int i = 0; HighlightedPath != null && i < HighlightedPath.Count - 1; i++)
            {
                pathEdges.Add((HighlightedPath[i], HighlightedPath[i + 1]));
                pathEdges.Add((HighlightedPath[i + 1], HighlightedPath[i]));
            }

            foreach (var edge in _graph.Edges)
            {
                var v1 = _graph.GetVertexById(edge.FromId);
                var v2 = _graph.GetVertexById(edge.ToId);
                if (v1 == null || v2 == null) continue;

                bool inPath = pathEdges.Contains((edge.FromId, edge.ToId));
                Color color = inPath ? Color.OrangeRed : Color.DimGray;
                float width = inPath ? 3.5f : 1.8f;

                using var pen = new Pen(color, width);
                if (edge.IsDirected)
                {
                    pen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(6, 6, true);
                }

                var (x1, y1, x2, y2) = TrimToCircles(v1.X, v1.Y, v2.X, v2.Y, Vertex.Radius);
                g.DrawLine(pen, x1, y1, x2, y2);

                float mx = (v1.X + v2.X) / 2f;
                float my = (v1.Y + v2.Y) / 2f;
                string weightText = edge.Weight.ToString("0.##");
                using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                var size = g.MeasureString(weightText, font);
                using var bg = new SolidBrush(Color.FromArgb(220, 255, 255, 200));
                g.FillRectangle(bg, mx - size.Width / 2 - 2, my - size.Height / 2 - 1,
                                    size.Width + 4, size.Height + 2);
                using var brush = new SolidBrush(inPath ? Color.DarkRed : Color.Black);
                g.DrawString(weightText, font, brush, mx - size.Width / 2, my - size.Height / 2);
            }

            foreach (var v in _graph.Vertices)
            {
                Color fill;
                if (StartVertex == v) fill = Color.LimeGreen;
                else if (EndVertex == v) fill = Color.Tomato;
                else if (HighlightedPath != null && HighlightedPath.Contains(v.Id)) fill = Color.Gold;
                else if (VisitedVertices != null && VisitedVertices.Contains(v.Id)) fill = Color.LightSkyBlue;
                else if (_firstSelected == v) fill = Color.Khaki;
                else fill = Color.WhiteSmoke;

                using var brush = new SolidBrush(fill);
                using var pen = new Pen(Color.Black, 2f);
                var rect = new RectangleF(v.X - Vertex.Radius, v.Y - Vertex.Radius,
                                          Vertex.Radius * 2, Vertex.Radius * 2);
                g.FillEllipse(brush, rect);
                g.DrawEllipse(pen, rect);

                using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
                var size = g.MeasureString(v.Label, font);
                using var textBrush = new SolidBrush(Color.Black);
                g.DrawString(v.Label, font, textBrush,
                             v.X - size.Width / 2, v.Y - size.Height / 2);
            }

            using var hintFont = new Font("Segoe UI", 9f, FontStyle.Italic);
            using var hintBrush = new SolidBrush(Color.Gray);
            string hint = GetModeHint();
            g.DrawString(hint, hintFont, hintBrush, 8, 8);
        }

        private static (float, float, float, float) TrimToCircles(float x1, float y1,
                                                                   float x2, float y2, float r)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-3f) return (x1, y1, x2, y2);
            float ux = dx / len;
            float uy = dy / len;
            return (x1 + ux * r, y1 + uy * r, x2 - ux * r, y2 - uy * r);
        }

        private string GetModeHint()
        {
            return _mode switch
            {
                CanvasMode.AddVertex => "Режим: додавання вершини. Клік по вільному місцю — нова вершина.",
                CanvasMode.AddEdge => _firstSelected == null
                    ? "Режим: додавання ребра. Виберіть першу вершину."
                    : $"Режим: додавання ребра. Виберіть другу вершину (перша: {_firstSelected.Label}).",
                CanvasMode.Delete => "Режим: видалення. Клік по вершині або ребру для видалення.",
                CanvasMode.Move => "Режим: переміщення. Перетягуйте вершини мишкою.",
                CanvasMode.SelectStart => "Режим: вибір стартової вершини. Клік по вершині.",
                CanvasMode.SelectEnd => "Режим: вибір кінцевої вершини. Клік по вершині.",
                _ => ""
            };
        }

        public static double AskForWeight(double defaultValue = 1.0)
        {
            using var dlg = new Form
            {
                Text = "Вага ребра",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(280, 110)
            };
            var lbl = new Label { Text = "Введіть вагу ребра:", Left = 12, Top = 12, Width = 240 };
            var txt = new TextBox { Left = 12, Top = 36, Width = 250, Text = defaultValue.ToString("0.##") };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 100, Top = 70, Width = 75 };
            var cancel = new Button { Text = "Скасувати", DialogResult = DialogResult.Cancel, Left = 185, Top = 70, Width = 80 };
            dlg.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
            dlg.AcceptButton = ok;
            dlg.CancelButton = cancel;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (double.TryParse(txt.Text.Replace(',', '.'),
                                    System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out double w))
                    return w;
            }
            return double.NaN;
        }

        public void ClearHighlight()
        {
            HighlightedPath = new List<int>();
            VisitedVertices = new HashSet<int>();
            Invalidate();
        }

        public void ClearStartEnd()
        {
            StartVertex = null;
            EndVertex = null;
            StartEndChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }
}

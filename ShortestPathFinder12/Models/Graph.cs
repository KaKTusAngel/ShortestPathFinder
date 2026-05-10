namespace ShortestPathFinder.Models
{

    public class Graph
    {

        public List<Vertex> Vertices { get; private set; } = new List<Vertex>();

        public List<Edge> Edges { get; private set; } = new List<Edge>();

        private int _nextVertexId = 0;

        public Vertex AddVertex(float x, float y)
        {
            int id = _nextVertexId++;
            string label = GenerateLabel(id);
            var vertex = new Vertex(id, label, x, y);
            Vertices.Add(vertex);
            return vertex;
        }

        public void RemoveVertex(Vertex vertex)
        {
            if (vertex == null) return;
            Edges.RemoveAll(e => e.FromId == vertex.Id || e.ToId == vertex.Id);
            Vertices.Remove(vertex);
        }

        public Edge AddOrUpdateEdge(Vertex from, Vertex to, double weight, bool isDirected)
        {

            Edge existing = null;
            foreach (var e in Edges)
            {
                if (isDirected)
                {
                    if (e.IsDirected && e.FromId == from.Id && e.ToId == to.Id)
                    {
                        existing = e;
                        break;
                    }
                }
                else
                {
                    if (!e.IsDirected && e.ConnectsSameVertices(from.Id, to.Id))
                    {
                        existing = e;
                        break;
                    }
                }
            }

            if (existing != null)
            {
                existing.Weight = weight;
                return existing;
            }

            var edge = new Edge(from.Id, to.Id, weight, isDirected);
            Edges.Add(edge);
            return edge;
        }

        public void RemoveEdge(Edge edge)
        {
            if (edge != null) Edges.Remove(edge);
        }

        public Vertex FindVertexAt(float x, float y)
        {
            foreach (var v in Vertices)
            {
                if (v.ContainsPoint(x, y)) return v;
            }
            return null;
        }

        public Edge FindEdgeAt(float x, float y, float threshold = 6f)
        {
            Edge best = null;
            float bestDist = threshold;
            foreach (var e in Edges)
            {
                var v1 = GetVertexById(e.FromId);
                var v2 = GetVertexById(e.ToId);
                if (v1 == null || v2 == null) continue;
                float d = DistanceToSegment(x, y, v1.X, v1.Y, v2.X, v2.Y);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = e;
                }
            }
            return best;
        }

        public Vertex GetVertexById(int id)
        {
            foreach (var v in Vertices)
                if (v.Id == id) return v;
            return null;
        }

        public Dictionary<int, List<(int neighborId, double weight)>> BuildAdjacencyList()
        {
            var adj = new Dictionary<int, List<(int, double)>>();
            foreach (var v in Vertices)
                adj[v.Id] = new List<(int, double)>();

            foreach (var e in Edges)
            {
                if (!adj.ContainsKey(e.FromId) || !adj.ContainsKey(e.ToId)) continue;
                adj[e.FromId].Add((e.ToId, e.Weight));
                if (!e.IsDirected)
                {
                    adj[e.ToId].Add((e.FromId, e.Weight));
                }
            }
            return adj;
        }

        public void Clear()
        {
            Vertices.Clear();
            Edges.Clear();
            _nextVertexId = 0;
        }

        private static string GenerateLabel(int index)
        {
            string label = "";
            int n = index;
            do
            {
                label = (char)('A' + (n % 26)) + label;
                n = n / 26 - 1;
            } while (n >= 0);
            return label;
        }

        private static float DistanceToSegment(float px, float py,
                                               float x1, float y1,
                                               float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 1e-6f) return (float)Math.Sqrt((px - x1) * (px - x1) + (py - y1) * (py - y1));
            float t = ((px - x1) * dx + (py - y1) * dy) / lenSq;
            t = Math.Max(0f, Math.Min(1f, t));
            float projX = x1 + t * dx;
            float projY = y1 + t * dy;
            float ddx = px - projX;
            float ddy = py - projY;
            return (float)Math.Sqrt(ddx * ddx + ddy * ddy);
        }
    }
}

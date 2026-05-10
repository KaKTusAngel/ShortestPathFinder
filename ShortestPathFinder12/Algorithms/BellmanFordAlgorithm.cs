using System.Diagnostics;
using ShortestPathFinder.Models;

namespace ShortestPathFinder.Algorithms
{

    public class BellmanFordAlgorithm : IShortestPathAlgorithm
    {
        public string Name => "Беллман-Форд";

        public AlgorithmResult FindPath(Graph graph, int sourceId, int targetId)
        {
            var result = new AlgorithmResult { AlgorithmName = Name };
            var stopwatch = Stopwatch.StartNew();

            var dist = new Dictionary<int, double>();

            var prev = new Dictionary<int, int>();

            foreach (var v in graph.Vertices)
            {
                dist[v.Id] = double.PositiveInfinity;
                prev[v.Id] = -1;
            }

            if (!dist.ContainsKey(sourceId) || !dist.ContainsKey(targetId))
            {
                stopwatch.Stop();
                result.Message = "Стартова або кінцева вершина не існує в графі.";
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                return result;
            }

            dist[sourceId] = 0;
            int n = graph.Vertices.Count;

            var edgeList = new List<(int from, int to, double w)>();
            foreach (var e in graph.Edges)
            {
                edgeList.Add((e.FromId, e.ToId, e.Weight));
                if (!e.IsDirected)
                    edgeList.Add((e.ToId, e.FromId, e.Weight));
            }

            for (int i = 0; i < n - 1; i++)
            {
                bool updated = false;
                foreach (var (from, to, w) in edgeList)
                {
                    result.OperationCount++;
                    if (double.IsPositiveInfinity(dist[from])) continue;
                    double newDist = dist[from] + w;
                    if (newDist < dist[to])
                    {
                        dist[to] = newDist;
                        prev[to] = from;
                        result.VisitedVertices.Add(to);
                        updated = true;
                    }
                }
                if (!updated) break;
            }

            foreach (var (from, to, w) in edgeList)
            {
                if (double.IsPositiveInfinity(dist[from])) continue;
                if (dist[from] + w < dist[to])
                {
                    stopwatch.Stop();
                    result.Message = "Виявлено цикл від'ємної ваги. Найкоротший шлях не визначений.";
                    result.TotalDistance = double.NegativeInfinity;
                    result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                    return result;
                }
            }

            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            if (double.IsPositiveInfinity(dist[targetId]))
            {
                result.Message = "Шлях між заданими вершинами не існує.";
                result.TotalDistance = double.PositiveInfinity;
                return result;
            }

            result.TotalDistance = dist[targetId];
            result.Path = ReconstructPath(prev, sourceId, targetId);
            result.Message = $"Шлях знайдено. Довжина = {result.TotalDistance:F2}";
            return result;
        }

        private static List<int> ReconstructPath(Dictionary<int, int> prev, int source, int target)
        {
            var path = new List<int>();
            int current = target;
            int safety = 0;

            while (current != -1 && safety++ < 100000)
            {
                path.Add(current);
                if (current == source) break;
                current = prev[current];
            }
            path.Reverse();
            return path;
        }
    }
}

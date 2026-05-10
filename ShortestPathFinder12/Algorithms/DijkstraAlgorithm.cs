using System.Diagnostics;
using ShortestPathFinder.Models;

namespace ShortestPathFinder.Algorithms
{

    public class DijkstraAlgorithm : IShortestPathAlgorithm
    {
        public string Name => "Дейкстра";

        public AlgorithmResult FindPath(Graph graph, int sourceId, int targetId)
        {
            var result = new AlgorithmResult { AlgorithmName = Name };
            var stopwatch = Stopwatch.StartNew();

            foreach (var e in graph.Edges)
            {
                if (e.Weight < 0)
                {
                    stopwatch.Stop();
                    result.Message = "Алгоритм Дейкстри не підтримує від'ємні ваги ребер.";
                    result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                    return result;
                }
            }

            var adjacency = graph.BuildAdjacencyList();

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

            var queue = new PriorityQueue<int, double>();
            queue.Enqueue(sourceId, 0);

            while (queue.Count > 0)
            {
                queue.TryDequeue(out int u, out double currentDist);
                result.OperationCount++;

                if (currentDist > dist[u]) continue;

                result.VisitedVertices.Add(u);

                if (u == targetId) break;

                if (!adjacency.ContainsKey(u)) continue;

                foreach (var (neighborId, weight) in adjacency[u])
                {
                    double newDist = dist[u] + weight;
                    if (newDist < dist[neighborId])
                    {

                        dist[neighborId] = newDist;
                        prev[neighborId] = u;
                        queue.Enqueue(neighborId, newDist);
                    }
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
            while (current != -1)
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

using System.Diagnostics;
using ShortestPathFinder.Models;

namespace ShortestPathFinder.Algorithms
{

    public class AStarAlgorithm : IShortestPathAlgorithm
    {
        public string Name => "A*";

        public double HeuristicScale { get; set; } = 1.0;

        public AlgorithmResult FindPath(Graph graph, int sourceId, int targetId)
        {
            var result = new AlgorithmResult { AlgorithmName = Name };
            var stopwatch = Stopwatch.StartNew();

            foreach (var e in graph.Edges)
            {
                if (e.Weight < 0)
                {
                    stopwatch.Stop();
                    result.Message = "Алгоритм A* не підтримує від'ємні ваги ребер.";
                    result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                    return result;
                }
            }

            var adjacency = graph.BuildAdjacencyList();
            var target = graph.GetVertexById(targetId);
            var source = graph.GetVertexById(sourceId);

            if (target == null || source == null)
            {
                stopwatch.Stop();
                result.Message = "Стартова або кінцева вершина не існує в графі.";
                result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                return result;
            }

            var gScore = new Dictionary<int, double>();

            var prev = new Dictionary<int, int>();

            var closedSet = new HashSet<int>();

            foreach (var v in graph.Vertices)
            {
                gScore[v.Id] = double.PositiveInfinity;
                prev[v.Id] = -1;
            }
            gScore[sourceId] = 0;

            var openSet = new PriorityQueue<int, double>();
            openSet.Enqueue(sourceId, Heuristic(source, target));

            while (openSet.Count > 0)
            {
                openSet.TryDequeue(out int currentId, out double _);
                result.OperationCount++;

                if (closedSet.Contains(currentId)) continue;
                closedSet.Add(currentId);
                result.VisitedVertices.Add(currentId);

                if (currentId == targetId) break;

                if (!adjacency.ContainsKey(currentId)) continue;

                foreach (var (neighborId, weight) in adjacency[currentId])
                {
                    if (closedSet.Contains(neighborId)) continue;

                    double tentativeG = gScore[currentId] + weight;
                    if (tentativeG < gScore[neighborId])
                    {
                        gScore[neighborId] = tentativeG;
                        prev[neighborId] = currentId;
                        var neighborVertex = graph.GetVertexById(neighborId);
                        double f = tentativeG + Heuristic(neighborVertex, target);
                        openSet.Enqueue(neighborId, f);
                    }
                }
            }

            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            if (double.IsPositiveInfinity(gScore[targetId]))
            {
                result.Message = "Шлях між заданими вершинами не існує.";
                result.TotalDistance = double.PositiveInfinity;
                return result;
            }

            result.TotalDistance = gScore[targetId];
            result.Path = ReconstructPath(prev, sourceId, targetId);
            result.Message = $"Шлях знайдено. Довжина = {result.TotalDistance:F2}";
            return result;
        }

        private double Heuristic(Vertex from, Vertex to)
        {
            return from.EuclideanDistanceTo(to) * HeuristicScale;
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

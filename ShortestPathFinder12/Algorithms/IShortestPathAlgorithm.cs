using ShortestPathFinder.Models;

namespace ShortestPathFinder.Algorithms
{

    public interface IShortestPathAlgorithm
    {

        string Name { get; }

        AlgorithmResult FindPath(Graph graph, int sourceId, int targetId);
    }
}

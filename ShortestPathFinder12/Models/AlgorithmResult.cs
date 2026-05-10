namespace ShortestPathFinder.Models
{

    public class AlgorithmResult
    {

        public string AlgorithmName { get; set; }

        public List<int> Path { get; set; } = new List<int>();

        public double TotalDistance { get; set; } = double.PositiveInfinity;

        public double ElapsedMilliseconds { get; set; }

        public long OperationCount { get; set; }

        public string Message { get; set; }

        public HashSet<int> VisitedVertices { get; set; } = new HashSet<int>();

        public bool Success => Path != null && Path.Count > 0 && !double.IsPositiveInfinity(TotalDistance);
    }
}

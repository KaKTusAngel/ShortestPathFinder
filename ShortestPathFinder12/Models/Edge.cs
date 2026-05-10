namespace ShortestPathFinder.Models
{

    public class Edge
    {

        public int FromId { get; set; }

        public int ToId { get; set; }

        public double Weight { get; set; }

        public bool IsDirected { get; set; }

        public Edge(int fromId, int toId, double weight, bool isDirected = false)
        {
            FromId = fromId;
            ToId = toId;
            Weight = weight;
            IsDirected = isDirected;
        }

        public bool ConnectsSameVertices(int aId, int bId)
        {
            return (FromId == aId && ToId == bId) || (FromId == bId && ToId == aId);
        }
    }
}

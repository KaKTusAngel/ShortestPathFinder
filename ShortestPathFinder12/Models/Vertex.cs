namespace ShortestPathFinder.Models
{

    public class Vertex
    {

        public int Id { get; set; }

        public string Label { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public const float Radius = 22f;

        public Vertex(int id, string label, float x, float y)
        {
            Id = id;
            Label = label;
            X = x;
            Y = y;
        }

        public bool ContainsPoint(float px, float py)
        {
            float dx = px - X;
            float dy = py - Y;
            return dx * dx + dy * dy <= Radius * Radius;
        }

        public float EuclideanDistanceTo(Vertex other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() => Label;
    }
}

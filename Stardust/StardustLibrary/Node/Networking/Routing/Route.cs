namespace StardustLibrary.Node.Networking.Routing;

public class Route
{
    public Route(Node target, Node nextHop, double metric)
    {
        Target = target;
        NextHop = nextHop;
        Metric = metric;
    }
    public Node Target { get; set; }
    public Node NextHop { get; set; }
    public double Metric { get; set; }

}

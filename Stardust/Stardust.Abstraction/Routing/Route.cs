namespace Stardust.Abstraction.Routing;

public class Route
{
    public Route(Node.Node target, Node.Node nextHop, double metric)
    {
        Target = target;
        NextHop = nextHop;
        Metric = metric;
    }
    public Node.Node Target { get; set; }
    public Node.Node NextHop { get; set; }
    public double Metric { get; set; }

}

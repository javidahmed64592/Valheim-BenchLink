using Xunit;

namespace BenchLink.Tests;

public class StationGraphTests
{
    private const int Workbench = 0;
    private const int Forge = 1;
    private const int Stonecutter = 2;

    private static StationNode Station(float x, float range, int type)
    {
        return new StationNode(x, 0f, range, type);
    }

    private static void AssertNear(double expected, double actual, double tolerance)
    {
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
    }

    [Fact]
    public void LensAndUnionAreasMatchTheFortyMetreExample()
    {
        // Level 5 workbench (36 m) and level 5 forge (38 m), 40 m apart.
        AssertNear(1491, StationGraph.LensArea(36, 38, 40), 0.5);
        AssertNear(7117, StationGraph.UnionArea(36, 38, 40), 0.5);
    }

    [Fact]
    public void SampledAreasMatchTheFortyMetreExample()
    {
        var graph = new StationGraph([Station(0, 36, Workbench), Station(40, 38, Forge)], 2.5f);

        (double vanilla, double linked) = graph.EstimateAreas(0, 1_000_000);

        AssertNear(1491, vanilla, 20);
        AssertNear(7117, linked, 40);
    }

    [Fact]
    public void LinkNeedsDistanceUnderTheLargerRangePlusSlack()
    {
        StationNode workbench = Station(0, 36, Workbench);
        StationNode forge = Station(40, 38, Forge);

        Assert.False(StationGraph.AreLinked(workbench, forge, 0f));
        Assert.False(StationGraph.AreLinked(workbench, forge, 2f));
        Assert.True(StationGraph.AreLinked(workbench, forge, 2.5f));
        Assert.True(StationGraph.AreLinked(workbench, Station(37, 38, Forge), 0f));
    }

    [Fact]
    public void DistanceIgnoresHeight()
    {
        StationNode a = new(0f, 0f, 20f, Workbench);
        StationNode b = new(3f, 4f, 20f, Forge);

        Assert.Equal(5f, StationGraph.Distance(a, b), 3);
    }

    [Fact]
    public void FarApartStationsAreSeparateClusters()
    {
        var graph = new StationGraph([Station(0, 36, Workbench), Station(80, 38, Forge)], 0f);

        Assert.Equal(2, graph.ClusterCount);
        Assert.NotEqual(graph.ClusterOf(0), graph.ClusterOf(1));
    }

    [Fact]
    public void LinkingIsTransitive()
    {
        // A-B and B-C are linked, A-C (60 m) is not.
        var graph = new StationGraph(
            [Station(0, 36, Workbench), Station(30, 36, Forge), Station(60, 36, Stonecutter)], 0f);

        Assert.Equal(1, graph.ClusterCount);
        Assert.Equal(graph.ClusterOf(0), graph.ClusterOf(2));
    }

    [Fact]
    public void FindLinkedServesPointsCoveredByAnyMemberOfTheCluster()
    {
        var graph = new StationGraph([Station(0, 36, Workbench), Station(30, 38, Forge)], 0f);

        // 60 m out is only inside the forge's circle, yet a workbench serves it through the link.
        Assert.Equal(0, graph.FindLinked(60f, 0f, Workbench));
        Assert.Equal(1, graph.FindLinked(60f, 0f, Forge));
    }

    [Fact]
    public void FindLinkedReturnsTheNearestStationOfTheType()
    {
        var graph = new StationGraph(
            [Station(0, 36, Workbench), Station(30, 38, Forge), Station(60, 36, Workbench)], 0f);

        Assert.Equal(2, graph.FindLinked(55f, 0f, Workbench));
        Assert.Equal(0, graph.FindLinked(-10f, 0f, Workbench));
    }

    [Fact]
    public void FindLinkedFailsOutsideEveryCircleOrWithoutTheType()
    {
        var graph = new StationGraph([Station(0, 36, Workbench), Station(30, 38, Forge)], 0f);

        Assert.Equal(-1, graph.FindLinked(200f, 0f, Workbench));
        Assert.Equal(-1, graph.FindLinked(10f, 0f, Stonecutter));
    }

    [Fact]
    public void FindLinkedIgnoresStationsInOtherClusters()
    {
        // The point is in the workbench's circle, but the only forge belongs to a different cluster.
        var graph = new StationGraph([Station(0, 36, Workbench), Station(200, 38, Forge)], 0f);

        Assert.Equal(-1, graph.FindLinked(10f, 0f, Forge));
    }
}

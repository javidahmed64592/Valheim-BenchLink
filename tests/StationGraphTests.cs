using Xunit;

namespace BenchLink.Tests;

public class StationGraphTests
{
    private const int Workbench = 0;
    private const int Forge = 1;
    private const int Stonecutter = 2;

    private static StationNode Station(float x, float range, int type, long order = 0)
    {
        return new StationNode(x, 0f, range, type, order);
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

    [Fact]
    public void FindLinkedReturnsTheNearestStationOfTheType()
    {
        // Two separate clusters, each with a workbench; the point is covered by both forges' clusters.
        var graph = new StationGraph(
            [Station(0, 36, Workbench), Station(30, 38, Forge), Station(100, 36, Workbench), Station(70, 38, Forge)], 0f);

        Assert.Equal(2, graph.FindLinked(85f, 0f, Workbench));
        Assert.Equal(0, graph.FindLinked(-10f, 0f, Workbench));
    }

    [Fact]
    public void SecondStationOfATypeDoesNotJoinTheCluster()
    {
        // A second workbench at the edge of the first cluster would otherwise pull it 30 m further out.
        var graph = new StationGraph(
            [Station(0, 36, Workbench), Station(30, 38, Forge), Station(60, 36, Workbench)], 0f);

        Assert.False(graph.IsDuplicate(0));
        Assert.True(graph.IsDuplicate(2));
        Assert.Equal(graph.ClusterOf(0), graph.ClusterOf(1));
        Assert.NotEqual(graph.ClusterOf(0), graph.ClusterOf(2));
        Assert.Equal(1, graph.ClusterSize(graph.ClusterOf(2)));
    }

    [Fact]
    public void DuplicateStillWorksAsVanilla()
    {
        var graph = new StationGraph(
            [Station(0, 36, Workbench), Station(30, 38, Forge), Station(60, 36, Workbench)], 0f);

        // 90 m out is only in the duplicate workbench's own circle: it serves workbench pieces, not forge pieces.
        Assert.Equal(2, graph.FindLinked(90f, 0f, Workbench));
        Assert.Equal(-1, graph.FindLinked(90f, 0f, Forge));
    }

    [Fact]
    public void ChainOfDuplicatesCannotExtendACluster()
    {
        var graph = new StationGraph(
            [
                Station(0, 36, Workbench),
                Station(30, 38, Forge),
                Station(60, 36, Workbench),
                Station(90, 36, Workbench),
                Station(120, 36, Workbench),
            ],
            0f);

        // 100 m out is only reached by the far workbenches, which stand alone instead of lending the forge's reach.
        Assert.Equal(-1, graph.FindLinked(100f, 0f, Forge));
        Assert.Equal(2, graph.ClusterSize(graph.ClusterOf(0)));
    }

    [Fact]
    public void HigherRangeDuplicateTakesTheSlot()
    {
        // The upgraded workbench (40 m) outranks the 36 m one, so it is the linked one.
        var graph = new StationGraph([Station(0, 36, Workbench), Station(30, 38, Forge), Station(60, 40, Workbench)], 0f);

        Assert.True(graph.IsDuplicate(0));
        Assert.False(graph.IsDuplicate(2));
        Assert.Equal(graph.ClusterOf(1), graph.ClusterOf(2));
    }

    [Fact]
    public void StationLinkingTwoClustersWithTheSameTypeJoinsTheNearest()
    {
        // Two workbench + forge clusters, with a stonecutter in range of both forges but nearer the first.
        var graph = new StationGraph(
            [
                Station(0, 36, Workbench),
                Station(30, 38, Forge),
                Station(90, 38, Forge),
                Station(120, 36, Workbench),
                Station(55, 37, Stonecutter),
            ],
            0f);

        Assert.False(graph.IsDuplicate(4));
        Assert.Equal(2, graph.ClusterCount);
        Assert.Equal(graph.ClusterOf(1), graph.ClusterOf(4));
        Assert.NotEqual(graph.ClusterOf(0), graph.ClusterOf(3));
    }

    [Fact]
    public void LaterStationOfAnExistingTypeIsADuplicate()
    {
        var graph = new StationGraph(
            [Station(0, 36, Workbench), Station(30, 38, Forge), Station(60, 36, Workbench, 5)], 0f);

        Assert.True(graph.IsDuplicate(2));
    }

    [Fact]
    public void EarlierBuiltStationKeepsTheSlotOnEqualRange()
    {
        // The new workbench is further left, which used to let it win the tie.
        var graph = new StationGraph(
            [Station(-10, 36, Workbench, 9), Station(0, 36, Workbench, 1), Station(20, 38, Forge, 2)], 0f);

        Assert.True(graph.IsDuplicate(0));
        Assert.False(graph.IsDuplicate(1));
        Assert.Equal(graph.ClusterOf(1), graph.ClusterOf(2));
    }

    [Fact]
    public void FindPrimaryCoveringSkipsDuplicates()
    {
        var graph = new StationGraph(
            [Station(0, 36, Workbench), Station(30, 38, Forge), Station(60, 40, Workbench)], 0f);

        Assert.True(graph.HasDuplicates);
        Assert.Equal(2, graph.FindPrimaryCovering(30f, 0f, Workbench));
        Assert.Equal(-1, graph.FindPrimaryCovering(-30f, 0f, Workbench));
    }

    [Fact]
    public void ResultDoesNotDependOnStationOrder()
    {
        StationNode[] stations = [Station(0, 36, Workbench), Station(30, 38, Forge), Station(60, 36, Workbench)];
        StationNode[] reversed = [stations[2], stations[1], stations[0]];

        var forward = new StationGraph(stations, 0f);
        var backward = new StationGraph(reversed, 0f);

        Assert.Equal(forward.IsDuplicate(2), backward.IsDuplicate(0));
        Assert.Equal(forward.IsDuplicate(0), backward.IsDuplicate(2));
    }
}

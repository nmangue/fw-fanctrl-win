using NFluent;

namespace FrameworkFanControl.Tests;

public class LinearFanControlCurveTest
{
    [Fact]
    public void Get_ReturnsFirstPoint_WhenTempBelowFirstPoint()
    {
        Dictionary<float, Percentage>points = new()
        {
            { 50f, 20 }
        };

        var profile = new LinearFanControlCurve(points);
        var state = new ComputerState(40f, 0f, []);

        var result = profile.Get(state);

        Check.That(result.Value).IsEqualTo(20);
    }

    [Fact]
    public void Get_ReturnsInterpolatedValue_BetweenPoints()
    {
        // points: 40 -> 20%, 80 -> 80%
        // temp 60 => ratio = (60-40)/(80-40) = 0.5
        // interpolated = 20% + 0.5 * (80% - 20%) = 50%
        var points = new Dictionary<float, Percentage>
        {
            { 40f, 20 },
            { 80f, 80 }
        };

        var profile = new LinearFanControlCurve(points);
        var state = new ComputerState(60f, 0f, []);

        var result = profile.Get(state);

        Check.That(result.Value).IsEqualTo(50);
    }

    [Theory]
    [InlineData(80f)]
    [InlineData(90f)]
    public void Get_ReturnsDefault_WhenTempAtOrAboveLastPoint(float temp)
    {
        var points = new Dictionary<float, Percentage>
        {
            { 40f, new Percentage(20) },
            { 80f, new Percentage(80) }
        };

        var profile = new LinearFanControlCurve(points);
        var state = new ComputerState(temp, 0f, []);

        var result = profile.Get(state);

        Check.That(result.Value).IsEqualTo(100);
    }

    [Fact]
    public void Constructor_SortsPointsAndAllowsInterpolation_EvenIfInputUnordered()
    {
        // input is unordered; behavior must be identical to ordered input
        var points = new Dictionary<float, Percentage>
        {
            { 80f, new Percentage(80) },
            { 40f, new Percentage(20) }
        };

        var profile = new LinearFanControlCurve(points);
        var state = new ComputerState(60f, 0f, System.Array.Empty<float>());

        var result = profile.Get(state);

        Check.That(result.Value).IsEqualTo(50);
    }
}
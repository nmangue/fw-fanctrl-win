using FrameworkFanControl.Domain;
using NFluent;

namespace FrameworkFanControl.Tests.Domain;

public class ComputerStateTest
{
	private const double Epsilon = 1e-6;

	[Fact]
	public void Average_MultipleStates_SameCoreCount_ComputesCorrectAverages()
	{
		// Arrange
		var states = new List<ComputerState>
		{
			new(80f, 60f, [50f, 70f]),
			new(90f, 65f, [55f, 75f]),
			new(70f, 55f, [45f, 65f]),
		};

		var expectedMaxAvg = 80f;
		var expectedAvgAvg = 60f;
		var expectedCore0 = 50f;
		var expectedCore1 = 70f;

		// Act
		var result = ComputerStateExtension.Average(states);

		// Assert
		Check.That(result.CoreMaxTemp).IsCloseTo(expectedMaxAvg, Epsilon);
		Check.That(result.CoreAvgTemp).IsCloseTo(expectedAvgAvg, Epsilon);
		Check.That(result.CoreTemps.Count).IsEqualTo(2);
		Check.That(result.CoreTemps[0]).IsCloseTo(expectedCore0, Epsilon);
		Check.That(result.CoreTemps[1]).IsCloseTo(expectedCore1, Epsilon);
	}

	[Fact]
	public void Average_DifferentCoreCounts_UsesMinimumCoreCount()
	{
		// Arrange
		var stateA = new ComputerState(60f, 50f, [10f, 20f, 30f]);
		var stateB = new ComputerState(80f, 70f, [20f, 40f]);
		var states = new List<ComputerState> { stateA, stateB };

		var expectedCore0 = 15f;
		var expectedCore1 = 30f;
		var expectedMaxAvg = 70f;
		var expectedAvgAvg = 60f;

		// Act
		var result = ComputerStateExtension.Average(states);

		// Assert
		Check.That(result.CoreMaxTemp).IsCloseTo(expectedMaxAvg, Epsilon);
		Check.That(result.CoreAvgTemp).IsCloseTo(expectedAvgAvg, Epsilon);
		Check.That(result.CoreTemps.Count).IsEqualTo(2);
		Check.That(result.CoreTemps[0]).IsCloseTo(expectedCore0, Epsilon);
		Check.That(result.CoreTemps[1]).IsCloseTo(expectedCore1, Epsilon);
	}

	[Fact]
	public void Average_SingleState_ReturnsIdenticalValues()
	{
		// Arrange
		var coreTemps = new List<float> { 12.5f, 15.5f, 18.0f };
		var state = new ComputerState(45f, 30f, coreTemps);
		var states = new List<ComputerState> { state };

		// Act
		var result = ComputerStateExtension.Average(states);

		// Assert
		Check.That(result.CoreMaxTemp).IsCloseTo(45f, Epsilon);
		Check.That(result.CoreAvgTemp).IsCloseTo(30f, Epsilon);
		Check.That(result.CoreTemps.Count).IsEqualTo(coreTemps.Count);
		for (int i = 0; i < coreTemps.Count; i++)
		{
			Check.That(result.CoreTemps[i]).IsCloseTo(coreTemps[i], Epsilon);
		}
	}

	[Fact]
	public void Average_EmptyCollection_ThrowsInvalidOperationException()
	{
		// Arrange
		var states = new List<ComputerState>();

		// Act & Assert
		Check
			.ThatCode(() => ComputerStateExtension.Average(states))
			.Throws<InvalidOperationException>();
	}
}

namespace FrameworkFanControl.Domain;

public record ComputerState(float CoreMaxTemp, float CoreAvgTemp, IReadOnlyList<float> CoreTemps);

public static class ComputerStateExtension
{
	public static ComputerState Average(this IReadOnlyCollection<ComputerState> states)
	{
		var maxAvgTemp = states.Select(s => s.CoreMaxTemp).Average();
		var avgAvgTemp = states.Select(s => s.CoreAvgTemp).Average();

		var nbCores = states.Min(s => s.CoreTemps.Count);

		var coreAvgTemps = new List<float>(nbCores);
		for (int i = 0; i < nbCores; i++)
		{
			coreAvgTemps.Add(states.Select(s => s.CoreTemps[i]).Average());
		}

		return new ComputerState(maxAvgTemp, avgAvgTemp, coreAvgTemps);
	}
}

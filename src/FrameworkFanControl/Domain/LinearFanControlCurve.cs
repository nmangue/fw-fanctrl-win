public class LinearFanControlCurve : IFanControlProfile
{
	private static readonly Percentage DefaultPercentage = new(100);
	private readonly SortedDictionary<float, Percentage> _points;

	public LinearFanControlCurve(IDictionary<float, Percentage> points)
	{
		_points = new(points);
	}

	public Percentage Get(ComputerState state)
	{
		var currentTemp = state.CoreMaxTemp;

		for (var i = 0; i < _points.Count; i++)
		{
			var (temp, percent) = _points.ElementAt(i);

			if (currentTemp < temp)
			{
				if (i == 0)
				{
					return percent;
				}
				else
				{
					var (prevTemp, prevPercent) = _points.ElementAt(i - 1);
					float ratio = (currentTemp - prevTemp) / (temp - prevTemp);
					var interpolatedValue = (int)(prevPercent + ratio * (percent - prevPercent));
					return new Percentage(interpolatedValue);
				}
			}
		}

		return DefaultPercentage;
	}
}

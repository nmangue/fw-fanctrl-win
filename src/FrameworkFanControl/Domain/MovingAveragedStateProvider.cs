using Microsoft.Win32;

namespace FrameworkFanControl.Domain;

public class MovingAveragedStateProvider : IStateProvider
{
	private readonly IStateProvider _baseProvider;
	private readonly int _width;
	private readonly Queue<ComputerState> _states;
	private readonly ILogger<MovingAveragedStateProvider>? _logger;

	public MovingAveragedStateProvider(
		IStateProvider baseProvider,
		int width,
		ILogger<MovingAveragedStateProvider>? logger
	)
	{
		_baseProvider = baseProvider;
		_width = Math.Max(1, width);
		_states = new Queue<ComputerState>(width);
		_logger = logger;

		SystemEvents.PowerModeChanged += OnPowerChange;
	}

	public ComputerState ReadState()
	{
		var state = _baseProvider.ReadState();
		while (_states.Count >= _width)
		{
			_states.Dequeue();
		}
		_states.Enqueue(state);

		var result = _states.Average();

		_logger?.LogInformation(
			"Moving average CPU temperatures: Core Max = {CoreMax}°C, Core Avg = {CoreAvg}°C",
			result.CoreMaxTemp,
			result.CoreAvgTemp
		);

		return result;
	}

	private void OnPowerChange(object s, PowerModeChangedEventArgs e)
	{
		switch (e.Mode)
		{
			case PowerModes.Resume:
				_logger?.LogInformation("Resetting moving average state");
				_states.Clear();
				break;
			case PowerModes.Suspend:
				// Nothing to do yet
				break;
		}
	}

	public void Dispose()
	{
		SystemEvents.PowerModeChanged -= OnPowerChange;
	}
}

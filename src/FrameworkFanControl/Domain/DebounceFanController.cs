using Microsoft.Win32;

namespace FrameworkFanControl.Domain;

public sealed class DebounceFanController : IFanController
{
	private readonly IFanController baseImplementation;
	private readonly ILogger<DebounceFanController> logger;
	private readonly int threshold;
	private Percentage? _lastValue = null;

	public DebounceFanController(
		IFanController baseImplementation,
		ILogger<DebounceFanController> logger,
		int threshold = 5
	)
	{
		this.baseImplementation = baseImplementation;
		this.logger = logger;
		this.threshold = threshold;

		SystemEvents.PowerModeChanged += OnPowerChange;
	}

	public void ActivateAutoFanContrl() => baseImplementation.ActivateAutoFanContrl();

	public void SetFanDuty(Percentage speed)
	{
		if (_lastValue is null || Math.Abs(speed.Value - _lastValue.Value) >= threshold)
		{
			baseImplementation.SetFanDuty(speed);
			_lastValue = speed;
		}
		else
		{
			logger.LogInformation(
				"Ignoring {ReevaluatedSpeed}. Keeping the fan at the previous value of {CurrentSpeed}",
				speed,
				_lastValue
			);
		}
	}

	private void OnPowerChange(object s, PowerModeChangedEventArgs e)
	{
		switch (e.Mode)
		{
			case PowerModes.Resume:
				logger?.LogInformation("Resetting debounce");
				_lastValue = null;
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

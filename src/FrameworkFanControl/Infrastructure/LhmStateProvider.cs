using System.Collections.Immutable;
using System.Text.RegularExpressions;
using FrameworkFanControl.Domain;
using LibreHardwareMonitor.Hardware;

namespace FrameworkFanControl.Infrastructure;

public class LhmStateProvider : IStateProvider
{
	private readonly Computer _computer;
	private readonly IHardware _cpu;
	private readonly ISensor? _coreMaxTempSensor;
	private readonly ISensor? _coreAvgTempSensor;
	private readonly IList<ISensor> _coreTempSensors;
	private readonly ILogger<LhmStateProvider>? _logger;

	public LhmStateProvider(ILogger<LhmStateProvider>? logger = null)
	{
		_computer = new Computer { IsCpuEnabled = true };
		_computer.Open();

		_cpu =
			_computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)
			?? throw new InvalidOperationException("No CPU hardware found.");

		_cpu.Update();

		_coreTempSensors = [];
		var coreTempSensorRegex = new Regex("^CPU Core #(\\d+)$", RegexOptions.CultureInvariant);
		foreach (var sensor in _cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature))
		{
			if (sensor.Name.Equals("Core Max"))
			{
				_coreMaxTempSensor = sensor;
			}
			else if (sensor.Name.Equals("Core Average"))
			{
				_coreAvgTempSensor = sensor;
			}
			else if (coreTempSensorRegex.IsMatch(sensor.Name))
			{
				_coreTempSensors.Add(sensor);
			}
		}

		_logger = logger;
	}

	public ComputerState ReadState()
	{
		_cpu.Update();
		float coreMaxTemp = _coreMaxTempSensor?.Value ?? float.NaN;
		float coreAvgTemp = _coreAvgTempSensor?.Value ?? float.NaN;
		var coreTemps = ImmutableList<float>.Empty.AddRange(
			_coreTempSensors.Select(s => s.Value ?? float.NaN)
		);

		_logger?.LogInformation(
			"Real-time CPU temperatures: Core Max = {CoreMax}°C, Core Avg = {CoreAvg}°C",
			coreMaxTemp,
			coreAvgTemp
		);

		return new ComputerState(coreMaxTemp, coreAvgTemp, coreTemps);
	}

	public void Dispose()
	{
		_computer.Close();
	}
}

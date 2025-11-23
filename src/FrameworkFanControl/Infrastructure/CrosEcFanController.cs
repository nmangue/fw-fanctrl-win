using FrameworkFanControl.Domain;

namespace FrameworkFanControl.Infrastructure;

public sealed class CrosEcFanController : IFanController
{
	private readonly CrosEcClient _client;
	private readonly ILogger<CrosEcFanController> _logger;

	public CrosEcFanController(ILogger<CrosEcFanController> logger)
	{
		_client = CrosEcClient.Open();
		_logger = logger;
	}

	public void SetFanDuty(Percentage speed)
	{
		_logger.LogInformation("Setting fan duty to {Speed}", speed);
		_client.SendCommand(CrosEcConstants.EC_CMD_PWM_SET_FAN_DUTY, (uint)speed.Value);
	}

	public void Dispose()
	{
		_client.Dispose();
	}

	public void ActivateAutoFanContrl()
	{
		// Restore fan 0x00 to automatic thermal control
		_logger.LogInformation("Restoring fan to automatic thermal control");
		_client.SendCommand(CrosEcConstants.EC_CMD_THERMAL_AUTO_FAN_CTRL, false);
	}
}

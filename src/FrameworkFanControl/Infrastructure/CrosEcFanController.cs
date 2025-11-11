namespace FrameworkFanControl.Infrastructure;

public class CrosEcFanController : IFanController
{
	private readonly CrosEcClient _client;

	public CrosEcFanController()
	{
		_client = CrosEcClient.Open();
	}

	public void SetFanDuty(Percentage speed)
	{
		_client.SendCommand(CrosEcConstants.EC_CMD_PWM_SET_FAN_DUTY, (uint)speed.Value);
	}

	public void Dispose()
	{
		_client.Dispose();
	}

	public void ActivateAutoFanContrl()
	{
		// Restore fan 0x00 to automatic thermal control
		_client.SendCommand(CrosEcConstants.EC_CMD_THERMAL_AUTO_FAN_CTRL, false);
	}
}

public interface IFanController : IDisposable
{
	void SetFanDuty(Percentage speed);

	void ActivateAutoFanContrl();
}

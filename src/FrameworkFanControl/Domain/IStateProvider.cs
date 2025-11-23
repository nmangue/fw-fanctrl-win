namespace FrameworkFanControl.Domain;

public interface IStateProvider : IDisposable
{
	ComputerState ReadState();
}

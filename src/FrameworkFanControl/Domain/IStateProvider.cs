public interface IStateProvider : IDisposable
{
	ComputerState ReadState();
}

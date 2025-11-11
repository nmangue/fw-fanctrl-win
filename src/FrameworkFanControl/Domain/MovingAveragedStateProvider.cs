public class MovingAveragedStateProvider : IStateProvider
{
    private readonly IStateProvider _baseProvider;
    private readonly int _size;
    private readonly Queue<ComputerState> _states;

    public MovingAveragedStateProvider(IStateProvider baseProvider, int size)
    {
        _baseProvider = baseProvider;
        _size = size;
        _states = new Queue<ComputerState>(size);
    }

    public void Dispose()
    {
        _baseProvider.Dispose();
    }

    public ComputerState ReadState()
    {
        var state = _baseProvider.ReadState();
        while (_states.Count >= _size)
        {
            _states.Dequeue();
        }
        _states.Enqueue(state);

        return _states.Average();
    }
}

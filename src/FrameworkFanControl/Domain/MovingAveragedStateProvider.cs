public class MovingAveragedStateProvider : IStateProvider
{
    private readonly IStateProvider _baseProvider;
    private readonly int _width;
    private readonly Queue<ComputerState> _states;

    public MovingAveragedStateProvider(IStateProvider baseProvider, int width)
    {
        _baseProvider = baseProvider;
        _width = Math.Max(1, width);
        _states = new Queue<ComputerState>(width);
    }

    public void Dispose()
    {
        _baseProvider.Dispose();
    }

    public ComputerState ReadState()
    {
        var state = _baseProvider.ReadState();
        while (_states.Count >= _width)
        {
            _states.Dequeue();
        }
        _states.Enqueue(state);

        return _states.Average();
    }
}

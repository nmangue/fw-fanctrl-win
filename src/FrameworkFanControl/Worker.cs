using FrameworkFanControl.Infrastructure;

namespace FrameworkFanControl;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IStateProvider _stateProvider = new MovingAveragedStateProvider(
            new LhmStateProvider(), 
            4);
        IFanControlProfile _fanProfile = new LinearFanControlCurve(new Dictionary<float, Percentage>
        {
            { 50f, 00 },
            { 60f, 50 },
            { 80f, 80 }
        });
        using IFanController _fanController = new CrosEcFanController();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                var state = _stateProvider.ReadState();
                logger.LogInformation("CPU Max at {temp}°C", state.CoreMaxTemp);
                var fanSpeed = _fanProfile.Get(state);
                logger.LogInformation("Setting speed to {speed}", fanSpeed);
                _fanController.SetFanDuty(fanSpeed);

                await Task.Delay(10_000, stoppingToken);
            }
        }
        finally
        {
            logger.LogInformation("Restoring automatic fan control");
            _fanController.ActivateAutoFanContrl();
        }

    }
}

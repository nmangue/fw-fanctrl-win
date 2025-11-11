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
            { 45f, 00 },
            { 55f, 10 },
            { 65f, 30 },
            { 70f, 40 },
            { 75f, 80 },
            { 85f, 100 }
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
        catch (TaskCanceledException)
        {
            // Stop without error
        }
        catch (OperationCanceledException)
        {
            // Stop without error
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
        finally
        {
            logger.LogInformation("Restoring automatic fan control");
            _fanController.ActivateAutoFanContrl();
        }

    }
}

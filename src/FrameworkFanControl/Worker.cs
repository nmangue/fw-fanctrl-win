namespace FrameworkFanControl;

public class Worker(IServiceScopeFactory serviceScopeFactory, ILogger<Worker> logger)
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				using var scope = serviceScopeFactory.CreateScope();

				var _stateProvider = scope.ServiceProvider.GetRequiredService<IStateProvider>();
				var _fanProfile = scope.ServiceProvider.GetRequiredService<IFanControlProfile>();
				var _fanController = scope.ServiceProvider.GetRequiredService<IFanController>();

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
			using var scope = serviceScopeFactory.CreateScope();
			var _fanController = scope.ServiceProvider.GetRequiredService<IFanController>();
			_fanController.ActivateAutoFanContrl();
		}
	}
}

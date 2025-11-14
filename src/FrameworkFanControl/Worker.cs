using Microsoft.Extensions.Options;

namespace FrameworkFanControl;

public class Worker(IServiceScopeFactory serviceScopeFactory, ILogger<Worker> logger)
	: BackgroundService
{
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var exitCode = 0;

		try
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				using var scope = serviceScopeFactory.CreateScope();

				var _stateProvider = scope.ServiceProvider.GetRequiredService<IStateProvider>();
				var _fanProfile = scope.ServiceProvider.GetRequiredService<IFanControlProfile>();
				var _fanController = scope.ServiceProvider.GetRequiredService<IFanController>();

				var state = _stateProvider.ReadState();

				var fanSpeed = _fanProfile.Get(state);

				_fanController.SetFanDuty(fanSpeed);

				var settings = scope
					.ServiceProvider.GetRequiredService<IOptionsSnapshot<FanControlSettings>>()
					.Value;
				await Task.Delay(settings.UpdateInterval, stoppingToken);
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
			exitCode = 1;
		}
		finally
		{
			using var scope = serviceScopeFactory.CreateScope();
			var _fanController = scope.ServiceProvider.GetRequiredService<IFanController>();
			_fanController.ActivateAutoFanContrl();
		}

		if (exitCode != 0)
		{
			Environment.Exit(exitCode);
		}
	}
}

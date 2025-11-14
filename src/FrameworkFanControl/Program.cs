using FrameworkFanControl;
using FrameworkFanControl.Infrastructure;
using Microsoft.Extensions.Options;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder
	.Services.AddOptions<FanControlSettings>()
	.Bind(builder.Configuration.GetSection(FanControlSettings.SectionName))
	.ValidateDataAnnotations()
	.ValidateOnStart();

builder.Services.AddSerilog(config =>
{
	config.ReadFrom.Configuration(builder.Configuration);
});

builder.Services.AddWindowsService(options =>
{
	options.ServiceName = "Framework Fan Control";
});

builder.Services.AddScoped<IFanControlProfile>(sp =>
{
	var settings = sp.GetRequiredService<IOptionsSnapshot<FanControlSettings>>().Value;
	var points = settings.ControlCurve.ToDictionary(kv => kv.Temp, kv => new Percentage(kv.Speed));
	return new LinearFanControlCurve(points);
});

builder.Services.AddSingleton<LhmStateProvider>();
builder.Services.AddSingleton<IStateProvider>(sp =>
{
	var settings = sp.GetRequiredService<IOptions<FanControlSettings>>().Value;
	var baseImplementation = sp.GetRequiredService<LhmStateProvider>();
	var logger = sp.GetRequiredService<ILogger<MovingAveragedStateProvider>>();
	return new MovingAveragedStateProvider(baseImplementation, settings.MovingAverageWidth, logger);
});

builder.Services.AddSingleton<IFanController, CrosEcFanController>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

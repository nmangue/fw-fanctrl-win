using FrameworkFanControl;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Framework Fan Control";
});
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

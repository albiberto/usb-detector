using UsbDetector.Worker;
using UsbDetector.Worker.Abstract;
using Detector = UsbDetector.Worker.Detectors.UsbDetector;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddScoped<IUsbDetector, Detector>();
        services.AddSignalR();
    })
    .Build();

await host.RunAsync();

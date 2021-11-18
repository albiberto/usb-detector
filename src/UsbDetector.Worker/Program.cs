using UsbDetector.Worker;
using UsbDetector.Worker.Abstract;
using UsbDetector.Worker.Hubs;
using Detector = UsbDetector.Worker.Detectors.UsbDetector;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddScoped<IUsbDetector, Detector>();
        services.AddSignalR();
    })
    .ConfigureWebHostDefaults(webBuilder => webBuilder.Configure(app =>
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<UsbHub>("/usbhub");
        });
    }))
    .Build();

await host.RunAsync();

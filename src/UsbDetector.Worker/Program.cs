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
        services.AddCors();
    })
    .ConfigureWebHostDefaults(webBuilder => webBuilder.Configure(app =>
    {
        app.UseCors(policy =>
        {
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            policy.AllowAnyOrigin();
        });
        
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<UsbHub>("/usbhub");
        });
    }))
    .Build();

await host.RunAsync();

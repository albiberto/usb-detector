using System.Management;

namespace UsbDetector.Worker;

public class Worker : IHostedService
{
    private readonly ILogger<Worker> _logger;

    private const string InsertQuery = @"SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'";
    private readonly ManagementEventWatcher _insertWatcher = new(new WqlEventQuery(InsertQuery));

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        _insertWatcher.EventArrived += (_, __) => Console.WriteLine("Usb Inserted");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _insertWatcher.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _insertWatcher.Stop();
        return Task.CompletedTask;
    }
}

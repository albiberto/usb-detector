using System.Management;
using UsbDetector.Worker.Abstract;

namespace UsbDetector.Worker;

public class Worker : IHostedService, IDisposable
{
    private readonly ILogger<Worker> _logger;

    private const string InsertQuery = @"SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'";
    private readonly ManagementEventWatcher _insertWatcher = new(new WqlEventQuery(InsertQuery));
    
    private const string RemoveQuery = @"SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'";
    private readonly ManagementEventWatcher _removeWatcher = new(new WqlEventQuery(RemoveQuery));


    public Worker(IServiceProvider provider, ILogger<Worker> logger)
    {
        _logger = logger;
        
        _insertWatcher.EventArrived += (sender, args) =>
        {
            using var scope = provider.CreateScope();
            var eventsService = scope
                .ServiceProvider
                .GetRequiredService<IUsbDetector>();

            eventsService.OnInserted(sender, args);
        };
        
        _removeWatcher.EventArrived += (sender, args) =>
        {
            using var scope = provider.CreateScope();
            var eventsService = scope
                .ServiceProvider
                .GetRequiredService<IUsbDetector>();

            eventsService.OnRemoved(sender, args);
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _insertWatcher.Start();
        _removeWatcher.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _insertWatcher.Stop();
        _removeWatcher.Stop();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _insertWatcher.Dispose();
        _removeWatcher.Dispose();
    }
}

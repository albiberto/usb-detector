using System.Management;
using Microsoft.AspNetCore.SignalR;
using UsbDetector.Worker.Abstract;
using UsbDetector.Worker.Hubs;

namespace UsbDetector.Worker.Detectors;

public class UsbDetector : IUsbDetector
{
    private const string SearchQuery = @"SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'";

    private readonly IHubContext<UsbHub, IUsbHub> _usbHub;

    public UsbDetector(IHubContext<UsbHub, IUsbHub> usbHub)
    {
        _usbHub = usbHub;
    }
    
    public async Task OnInserted(object sender, EventArrivedEventArgs e)
    {
        using var searcher = new ManagementObjectSearcher(SearchQuery);
        foreach (var currentObject in searcher.Get())
        {
            var management = new ManagementObject("Win32_PhysicalMedia.Tag='" + currentObject["DeviceID"] + "'");
            var serialNumber = $"{management["SerialNumber"]}";
            
            await _usbHub.Clients.All.Connect(serialNumber);
        }
    }

    public async Task OnRemoved(object sender, EventArrivedEventArgs e)
    {
        await _usbHub.Clients.All.Connect(string.Empty);
    }
}
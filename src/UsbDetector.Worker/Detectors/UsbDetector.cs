using System.Management;
using UsbDetector.Worker.Abstract;

namespace UsbDetector.Worker.Detectors;

public class UsbDetector : IUsbDetector
{
    private const string SearchQuery = @"SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'";

    public void OnInserted(object sender, EventArrivedEventArgs e)
    {
        using var searcher = new ManagementObjectSearcher(SearchQuery);
        foreach (var currentObject in searcher.Get())
        {
            var management = new ManagementObject("Win32_PhysicalMedia.Tag='" + currentObject["DeviceID"] + "'");
            var serialNumber = $"{management["SerialNumber"]}";
            
            Console.WriteLine($"{serialNumber} USB inseted");
        }
    }

    public void OnRemoved(object sender, EventArrivedEventArgs e)
    {
        Console.WriteLine("USB removed");
    }
}
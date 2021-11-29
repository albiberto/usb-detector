using System.Management;

namespace UsbDetector.Worker.Abstract;

public interface IUsbDetector
{
    Task OnInserted(object sender, EventArrivedEventArgs e);
    Task OnRemoved(object sender, EventArrivedEventArgs e);
}
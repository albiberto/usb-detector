using System.Management;

namespace UsbDetector.Worker.Abstract;

public interface IUsbDetector
{
    void OnInserted(object sender, EventArrivedEventArgs e);
    void OnRemoved(object sender, EventArrivedEventArgs e);
}
namespace UsbDetector.Worker.Abstract;

public interface IUsbHub
{
    Task Connect(string serialNumber);
}
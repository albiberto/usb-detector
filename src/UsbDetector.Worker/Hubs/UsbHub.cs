using Microsoft.AspNetCore.SignalR;
using UsbDetector.Worker.Abstract;

namespace UsbDetector.Worker.Hubs;

public class UsbHub : Hub<IUsbHub>
{
}
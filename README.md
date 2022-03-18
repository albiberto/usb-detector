# USB Detector
This demo project shows how to recognize when an USB stick is plugged into a PC. For this purpose it use both a windows service and a WASM Blazor client.

## Blue-Print
This solution aims to make WASM Blazor client responsive when an USB stick is plugged into a PC.

## Wiki
If you are interesting in having more details look at the application, visit our [Wiki](https://github.com/aviezzi/usb-detector/wiki).

## Tutorial
To get information on how replicate the application code, follow [Tutorial](https://github.com/aviezzi/usb-detector/wiki/Tutorial) wiki section.

## Big Picture
The solution consist of two projects a ```Worker Project``` a WindowsService installed on local PC and a ```Client Project``` a WASM Blazor client that runs on local PC browser.

### Client
When an user navigates to ```http://localhost:6001``` the client attempt to connect to the SignalR worker hub.
If the worker is not running yet then the client will retry the connection every 5 seconds and the homepage will display the hub connection status as disconnected.
If the worker is running, then the client will connect to worker's SignalR hub and update home page status as connected and will wait for pushes from the worker.

### Worker
Its purpose is spy windows management system by regularly making WQL (SQL like) queries. When the USB stick is plugged or unplugged it will push to the client through SignalR Hub (on ```http://localhost:5000```).

## Complete Life Cycle

The application conceptual life cycle is described by the following steps:
1. User navigate by browser to ```https://host.my```
2. The web page try to connect to local SignalR hub but it is not preset
3. The web page show the status of hub connection to ```DISCONNECTED``` and the usb status to ```UNPLUGGED```
4. The web page wait 5 seconds and retry to connect to SignalR hub forever
5. The user install the windows service
6. When the service is installed the client connected to it and the hub status is updated to ```CONNECTED```
7. The user plug a USB stick on the PC
8. The service detect USB key and via SignalR push the client
9. The client update the web page to show usb status to ```PLUGGED```
10. The user unplug the USB stick by the PC
11. The service detect USB key is unplugged and via SignalR push the client
12. The web page show USB status to ```UNPLUGGED```

<p align="center">
  <img src="https://github.com/aviezzi/usb-detector/blob/main/img/big_picture.gif" alt="Big Picture"/>
</p>

### Bootstrapped Ecosystem
When ecosystem is bootstrapped the client is connected to worker SignalR hub, and wait for worker push USB stick is plugged or unplugged. The below image shows the mechanism.

![Big Picture](https://github.com/aviezzi/usb-detector/blob/main/img/big_picture_cut.gif)

## Bootstrap Pratical Example

This project don't have a specific starting order, the worst scenario is when client stars first, let us to analyse this case.
Refer to [Bootstrap](https://github.com/aviezzi/usb-detector/wiki/Bootstrap) wiki section to more details on how bootstrap the application. 

### Run client
This step shows how start client and what browser application page shows.

![Client start](https://github.com/aviezzi/usb-detector/blob/main/img/client.gif)

### Run worker
This step shows how start worker and when browser application page is updated.

![Worker start](https://github.com/aviezzi/usb-detector/blob/main/img/server.gif)

### Web Application
This step show how browser application page react when usb stick is plugging/unplugging.

<p align="center">
  <img src="https://github.com/aviezzi/usb-detector/blob/main/img/usb.gif" alt="Plug USB gif"/>
</p>

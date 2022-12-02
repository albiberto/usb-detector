# USB Detector
This project is a demo to show how recognize when a USB stick is plugged on PC by a WASM Blazor client.

## Blue-Print
The scope of the solution is to make WASM Blazor client responsive when USB stick is plugged to PC.

## Actors
 
The application has 2 projects:
1. Worker Project: is a WASM Blazor client, and run on the local PC browser.
2. Client Project: is a WindowsService installed on local PC. 

### Client
When user navigate to ```( http://localhost:6001)```, the client try to connect to SignalR worker hub. 
If the worker not running yet, the client retry to ricconect forever with a deplay policy of 5 seconds and the homepage displays the hub connection status to Disconnected.
If or when worker is bootstrapped, the client connect to worker SignalR hub and update home page status to Connected finaly wait for worker pushes.

### Worker
Its purpose is watching windows management, with WQL (SQL like) queries and push client through SignalR Hub (on ```http://localhost:5000```), when USB stick is plugged or unplugged.

## LifeCycle
The projects don't have a specific bootstrap order, in any case the worst order is when client starting first, so analyse this scenario.

### Start Client
The client bootstrapped and waits for worker bootrapping. The connections status is set to ```Disconnected``` and the USB status is set to ```unplugged``` 
<br />
![alt tag](https://github.com/aviezzi/usb-detector/blob/documentation/img/client.gif)

### Start Worker
When worker bootstrapped, the client established connection to SignalR hub. The connections status is set to ```Connected``` and the USB status is set to ```unplugged```
<br />
![alt tag](https://github.com/aviezzi/usb-detector/blob/documentation/img/server.gif)

### USB Plug/Unplug
When client and worker are bootstrapped.
The client wait USB is plugging when it happen, the home page is refresh, the USB status is update to ```plugged``` and the ```serialnumber``` is show.  
When the USB stick is unplugged the USB status is update to ```unplugged``` 
<br />
![alt tag](https://github.com/aviezzi/usb-detector/blob/documentation/img/usb.gif)

## Tutorial
To reproduce the demo follow the below instructions:

### Create New Worker Project
To create run from terminal the following instructions
 1. ```mkdir usb-detector```
 2. ```cd usb-detector```
 3. ```dotnet new sln -n UsbDetector```
 4. ```mkdir src```
 5. ```cd src```
 6. ```dotnet new worker -n UsbDetector.Worker```
 7. ```cd ..```
 8. ```dotnet sln add .\UsbDetector.sln .\src\UsbDetector.Worker\UsbDetector.Worker.csproj```

### Add Usb Watchers to Worker
To make worker abel to recognize when USB is present add the following code:
1. remove inherit BackgroundService class from Worker class
2. remove virtual method ExecuteAsync
3. implement IHostedService interface
4. implement missing interface members StartAsync and StopAsync, for now they can throw NotImplementedException
5. add watcher that fire when InsertQuery is satisfied
   ``` csharp
   private const string InsertQuery = @"SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'";
   private readonly ManagementEventWatcher _insertWatcher = new(new WqlEventQuery(InsertQuery))
6. In the worker constructor we need to register what we want to do when watcher is fired for now we can log on console.
   ```csharp
   public Worker()
   {
        _insertWatcher.EventArrived += (sender, args) => Console.WriteLine("UsbInserted");
   }
7. Now we have to start and stop the watcher, to do this modify StartAsync and StopAsync worker methods, like follow:
   ```csharp
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
8. And finally we have to dispose watcher when the worker is shutdown, to do this implement IDisposable interface and implement it own method
   ```csharp
   public class Worker : IHostedService, IDisposable
   ```
   
   ```csharp
    public void Dispose() => _insertWatcher.Dispose();
   ```
9. Now if we run the worker and try to plug and unplug USB stick from PC when che see "UsbInserted" message in console.
To run worker, with terminal, go inside worker project folder and type dotnet run.

### Add USB Detector
Now we going to add the class to manage was happen when USB is plugged
1. create a Abstract folder
2. inside the folder add IUsbDetector interface
   ```csharp
   public interface IUsbDetector
   {
       Task OnInserted(object sender, EventArrivedEventArgs e);
   }
3. create Detectors folder
4. inside the folder add new class named UsbDetector and implement IUsbDetectorInterface
   ```csharp
   public class UsbDetector : IUsbDetector
5. add private const searcher WQL query
   ```csharp
   private const string SearchQuery = @"SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'";
6. add the following code in OnInserted method
   ```csharp
   public async Task OnInserted(object sender, EventArrivedEventArgs e)
   {
       using var searcher = new ManagementObjectSearcher(SearchQuery);
       foreach (var currentObject in searcher.Get())
       {
           var management = new ManagementObject("Win32_PhysicalMedia.Tag='" + currentObject["DeviceID"] + "'");
           var serialNumber = $"{management["SerialNumber"]}";              
   
           Console.WriteLine($"{serialNumber} USB inserted");
       }
   }
7. Now we have to register the above service in DI, go to Program class and add the following line under ConfigureServices extension method
   ```csharp
   .ConfigureServices(services =>
   {
       services.AddHostedService<Worker>();
       services.AddScoped<IUsbDetector, Detector>();
   }
   ```
   Warning: use the following using instruction:
   ```charp
   using Detector = UsbDetector.Worker.Detectors.UsbDetector;
   ```
8. At this point we had end the activity on UsbDetector class and we go to update worker class to use new code.

### Update Worker
Now we have to update worker to use above service.
1. Now we have to inject in worker IServiceProvider to retrieve new instance of detector every time an event was fired, in fact the worker 
has a singleton instance life time, so we have to access to ServiceProvider directly.
   ```csharp
   public Worker(IServiceProvider provider)
   {
       ...
   ```
2. Change EventArrived registration inside worker constructor from:
   ```csharp
   _insertWatcher.EventArrived += (sender, args) => Console.WriteLine("UsbInserted");
   ```
   to
   ```csharp
   _insertWatcher.EventArrived += (sender, args) =>
   {
       using var scope = provider.CreateScope();
       var eventsService = scope
           .ServiceProvider
           GetRequiredService<IUsbDetector>();

       eventsService.OnInserted(sender, args);
   };
   ```
3. Now if we run the worker and try to plug and unplug USB stick from PC when che see "{SERIAL_NUMBER} USB inserted" message in console.
    
### Add SignalR Hub
now we have a SignalR hub, when client can connect, to worker. To do this follow the next steps:
1. Install package:
   ```dotnet add package Microsoft.AspNetCore.SignalR --version 1.1.0```
2. Add new interface called IUsbHub inside Abstract folder
   ```csharp
   public interface IUsbHub
   {
       Task Connect(string serialNumber);
   }
   ```
3. create a folder called Hubs and inside it create new class called UsbHub, this class has to inherits from Hub<T> SignalR abstract class
   ```csharp
   public class UsbHub : Hub<IUsbHub>
   {
   }
   ```
4. now we have to expose our hub endpoint, but first to proceed we have to change projects sdk, to do this, edit csproj worker file and change the following line
   ```csharp
   <Project Sdk="Microsoft.NET.Sdk.Worker">
   ```
   with
   ```csharp
   <Project Sdk="Microsoft.NET.Sdk.web">
   ```
5. After that we can go in Program class and add the endpoint to app builder
   ```csharp
   .ConfigureWebHostDefaults(webBuilder => webBuilder.Configure(app =>
   {
       app.UseRouting();
       app.UseEndpoints(endpoints =>
       {
           endpoints.MapHub<UsbHub>("/usbhub");
       });
   }))
   .Build();
   ```
6. For now we end with worker service. at this point it is not runnable configuration. We continue with writing a WASM client

### Create WASM Blazor Client
The next step is to create a new Blazor WASM projects to do this, open terminal, go inside src folder in UsbDetector solution folder, and run fallowing commands:
1. ```dotnet new blazorwasm -n UsbDetector,Client```
2```cd ..```
3```dotnet sln add .\UsbDetector.sln .\src\UsbDetector.Client\UsbDetector.Client.csproj```

### Add SignalR Client
Now we have to add SignalR client to connect with SignalR worker hub, let's start:
1. Install package
   ```dotnet add package Microsoft.AspNetCore.SignalR.Client --version 6.0.0```
2. Add ```@using Microsoft.AspNetCore.SignalR.Client``` in ```_import.razor``` file 
3. Add ```@Code``` directive in ```Index.razor``` page and override ```OnInitializedAsync``` method
   ```csharp
   protected override async Task OnInitializedAsync()
   {
       await StartHubConnection();
   }
   
   private async Task StartHubConnection()
   {
       throw new NotImplementedException();
   }
   ```
4. Inject ```NavigationManager```
   ```csharp
   @code 
   {
      @inject NavigationManager _navigationManager
      ...
   ```
5. Create a nullable directive variable called ```_hubConnection``` of type ```HubConnection```
   ```csharp 
   @code
   {
      private HubConnection? _hubConnection;
   
      ...
   ```
6. add following property:
   ```csharp
   private bool Connected => _hubConnection?.State == HubConnectionState.Connected;
   ```
7. Implement StartHubConnection method
   ```csharp
   private async Task StartHubConnection()
   {
      _hubConnection = new HubConnectionBuilder()
         .WithUrl(_navigationManager.ToAbsoluteUri("http://localhost:5000/usbhub"))
         .WithAutomaticReconnect()
         .Build();
      
      _hubConnection.On<string>("Connect", serialNumber =>
      {
         _serialNumber = serialNumber;
         StateHasChanged();
      });
           
      await _hubConnection.StartAsync();
           
      if (Connected) Console.WriteLine("connection started");
    }
   ```
8. Implement IAsyncDispose interface
   ```csharp
   @implements IAsyncDisposable
   ```
9. Implement missing DisposeAsync method
   ```csharp
   public async ValueTask DisposeAsync()
   {
      if(_hub.connection is not null) await _hubConnection.DisposeAsync;
   }
   ```
11. Change app http url to no have conflicts with worker service, in Properties folder in launchSettings.josn file change port from 5001 to 6001
```json
    "UsbDetector.Client": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "inspectUri": "{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}",
      "applicationUrl": "https://localhost:7263;http://localhost:6001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
```
9. If now we start the app we received CORS error, to manage this we have to enable CORS on worker.

### Enable CORS
Go to program class in worker projects to add CORS support
1. In ```ConfigureServices``` add the following
   ```csharp
   services.AddCors();
   ```
2. in ```ConfigureWebHostDefaults``` add at the start
   ```csharp
   app.UseCors(policy => {
      policy.AllowAnyHeader();      
      policy.AllowAnyMethod();      
      policy.AllowAnyOrigin();
   })
   ```
3. Run the following commands on ```Terminal``` from solution folder
   - ```cd src\UsbDetector.Worker```
   - ```dotnet run```
   - ```cd ..```
   - ```cd UsbDetector.Client```
   - ```dotnet run```
4. The client start and open window browser that show the succeed connection between client and worker. You can also open browser console to see "connection started" message

### React to Worker Push
Now we have to add the piece of code, that manage when worker push us that the USB stick is inserted
1. prepare a variable where store serial number
   ```csharp
   private string? _serialNumber;
   ```
2. In ```StartHubConnection``` in ```Index.razor``` page, we added ```_hubConnection.On``` extension method, change method with following
   ```csharp
   private async Task TryOpenSignalRConnection()
   {
      _hubConnection = new HubConnectionBuilder()
         .WithUrl(_navigationManager.ToAbsoluteUri("http://localhost:5000/usbhub"))
         .WithAutomaticReconnect()
         .Build();

      _hubConnection.On<string>("Connect", serialNumber =>
      {
         _serialNumber = serialNumber;
         StateHasChanged();
      });
        
      await _hubConnection.StartAsync();
        
      if (Connected) Console.WriteLine("connection started");
   }
   ```
3. Update view to show pushed values
   ```csharp
   <PageTitle>USB-Detector</PageTitle>
   
   <h1>USB Detector</h1>
   
   <ul>
       <li>
           <span>Usb Hub is: <strong>@(Connected ? "Connected" : "Disconnected")</strong></span>        
       </li>
       <li>
           @if (string.IsNullOrEmpty(_serialNumber))
           {
               <span>Usb unplugged</span>
           }
           else
           {
               <span>Usb plugged: <strong>@_serialNumber</strong></span>
           }
       </li>
   </ul>
   ```
4. If now start two projects we can see the following state

### Push WASM Client
now we have to add the code to push the client in worker projects
1. Inject ```IHubContext<T1, T2>``` inside ```UsbDetector``` class
   ```csharp
   private readonly IHubConnection<UsbHub, IUsbHub> _usbHub;
   
   public UsbDetector(IHubConnection<UsbHub, IUsbHub> usbHub)
   {
      _usbhub = usbHub;
   }
   ```
2. Refactor interface ```IUsbDetector```
   ```csharp
   public interface IUsbDetector
   {
      Task OnInserted(object sender, EventArrivedEventArgs e);
   }
   ```
3. refactor ```OnInserted``` method in ```UsbDetector``` class
   ```csharp
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
   ```
4. Now we can run the application but first we have to do an observation, if the worker is not running when client bootstrap it go in exception and break.
To prevent this we have to add and retry policy, let's follow the last step before run the solution

### Add retry Policy
1. Install package:
   ```dotnet add package Polly --version 7.2.2```
2. Rename ```StartHubConnection``` method into ```TryOpenSignalRConnection```
3. Recreate ```StartHubConnection``` method and add the following code
```csharp
   private async Task StartHubConnection()
   {
      var retryPolicy = Policy
         .Handle<Exception>()
         .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(5), (exception, timeSpan) => Console.WriteLine($"Connection cannot be established"));
        
      await retryPolicy.ExecuteAsync(async () =>
      {
         Console.WriteLine("Trying to connect to SignalR server");
         await TryOpenSignalRConnection();
      });
   }
```
4. now you can row the two projects in any order and try it by yourself.

![alt tag](https://github.com/aviezzi/usb-detector/blob/documentation/img/client-only.gif)

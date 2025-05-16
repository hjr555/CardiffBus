## Cardiff Bus API lookup

Listening to Cardiff Bus Central Control on 178.25000 MHz can be quite interesting.

I made a fun little app so you can find out more details about the bus they are communicating with, as they use the fleet number, rather than the route number.

### Usage
Requires [dotnet 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
Then, just run the app with the fleet number as the first parameter, e.g.

`dotnet run 123`  
or  
`CardiffBus.exe 123`

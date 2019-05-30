<div align="center">

[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://raw.githubusercontent.com/alandoherty/devicepilot-api-net/master/LICENSE)
[![GitHub issues](https://img.shields.io/github/issues/devicepilot-api-net/protosocket-net.svg?style=flat-square)](https://github.com/alandoherty/devicepilot-api-net/issues)
[![GitHub stars](https://img.shields.io/github/stars/devicepilot-api-net/protosocket-net.svg?style=flat-square)](https://github.com/alandoherty/devicepilot-api-net/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/devicepilot-api-net/protosocket-net.svg?style=flat-square)](https://github.com/alandoherty/devicepilot-api-net/network)
[![GitHub forks](https://img.shields.io/nuget/dt/DevicePilot.Api.svg?style=flat-square)](https://www.nuget.org/packages/DevicePilot.Api/)

</div>

# devicepilot-api

A .NET API client for the Device Pilot IoT analytics platform. Open permissive MIT license and requires a minimum of .NET Standard 1.3.

## Getting Started

[![NuGet Status](https://img.shields.io/nuget/v/DevicePilot.Api.svg?style=flat-square)](https://www.nuget.org/packages/DevicePilot.Api/)

You can install the package using either the CLI:

```
dotnet add package DevicePilot.Api
```

or from the NuGet package manager:

```
Install-Package DevicePilot.Api
```

## Usage

Create an instance of the API client and configure your authentication token, you can find this on the API keys page of the platform.

```csharp
ApiClient client = new ApiClient();
```

### Ingesting

You can start ingesting data immediately after creating the client, you can ingest either a single device update or as many as you want. If you ingest more than one device the batch API will be used, if you pass more than 500 devices the client will split up the request automatically.

```csharp
var devices = new DeviceData[] {
	new DeviceData() {
		Id = "switch1",
		Properties = new Dictionary<string, object>() {
			{ "name", "Switch 1" },
			{ "isOn", true },
			{ "longitude", 53.7005d },
			{ "latitude", 2.3015d }
		}
	},
	new DeviceData() {
		Id = "switch2",
		Properties = new Dictionary<string, object>() {
			{ "name", "Switch 2" },
			{ "isOn", false },
			{ "longitude", 53.7005d },
			{ "latitude", 2.3015d }
		}
	}
};

await client.BulkIngestAsync(devices);
```

### Mapped types

Optionally you can use .NET objects to represent your device data structure. You can use either `client.IngestAsync<T>` or `client.BulkIngestAsync<T>`.

```csharp
class MyDevice 
{
	[DeviceId]
	public string ID { get; set; }

	[DeviceProperty("online")]
	public bool Online { get; set; }

	[DeviceProperty]
	public double Temperature { get; set; }

	[DeviceTimestamp]
	public DateTime? Timestamp { get; set; }
}
```

```csharp
await client.IngestAsync<MyDevice>(new MyDevice() {
	ID = "temp1",
	Online = true,
	Temperature = 16.21d
});
```

## Contributing

Any pull requests or bug reports are welcome, please try and keep to the existing style conventions and comment any additions.
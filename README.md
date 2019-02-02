# UKHO.ConfigurableStub

This package provides a stub for API calls that can be configured to return pre-defined responses based upon HTTP calls made to it during runtime.

## How to use the Stub

The stub is a .net core webapp targeting .net framework 471. The web app is packaged into a nuget package. Currently the stub can only be used if you are using the new csproj format due to the way the libuv.dll dependency is resolved.

### Running up the stub

To start the stub you can use the static methods in the `StubStartup` class. 
```cs
StubStartup.StartWithSpecifiedPorts(44367, 45678);
```
This would start the stub up on port 44367 for https and port 45678 for http using a self-signed certificate that will be valid for 40 minutes. The stub runs using kestrel only to be more light weight and easily configurable.

### Configuring a call

To configure a call to the stub you should post to the stub on the route `stub/{verb}/api/{*resource}"`where verb is the http verb and resource is the path you wish to configure. With this call you should include the `RouteConfiguration` object in the body serialised as json, this will specify the response to the configured call.

### Calling a configured URL

Once a URL has been configured it can be called at the route `api/{*resource}"` this will return the last response that was configured for that resource.

### Getting the calls to URL

If you wish to get the request that have been set to a URL this can be done by calling the stub with a get request on the path `stub/{verb}/api/{*resource}`


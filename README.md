# UKHO.ConfigurableStub

This package provides a stub for API calls that can be configured to return pre-defined responses based upon HTTP calls made to it during runtime.

## How to use the Stub

The stub is a .net core webapp targeting .net framework 471. The web app is packaged into a nuget package. Currently the stub can only be used if you are using the new csproj format due to the way the libuv.dll dependency is resolved.
The stub also has libuv baked into the nuget package as test project, such as Autotest projects also do not bring in libuv even if they are the new Csproj types. This could cause problems with cross compatibilty with versions of windows and linux.

### Running up the stub

There are a few way to start the stub.

#### Using the Stub Manager

You can start the stub usiong the stub manager

```cs
            var stubManager = new StubManager();
            stubManager.Start(Console.Out, 5000, 5001);
```

This would start the stub in a seperate process on ports 5000 and 5001. It would redirect the stub standard out to the console. After the stub has finished starting and is ready to take request it will return. The stub will use a self-signed certificate that will be valid for 40 minutes. 

#### Start using the startup methods

To start the stub you can use the static methods in the `StubStartup` class. 

```cs
StubStartup.StartStub(44367, 45678);
```

This would start the stub up on port 44367 for https and port 45678 for http using a self-signed certificate that will be valid for 40 minutes. This call will hold execution for as long as the stub is running.

### Configuring a call

To configure a call to the stub you should post to the stub on the route `stub/{verb}/api/{*resource}"`where verb is the http verb and resource is the path you wish to configure. With this call you should include the `RouteConfiguration` object in the body serialised as json, this will specify the response to the configured call. You can also use the stub client to do so which will call the stub for you.

```cs
            var client = new StubClient($"https://localhost:{DefaultPortConfiguration.HttpsPort}");
            await client.ConfigureRouteAsync(HttpMethod.Get,
                                       "hi",
                                       new RouteConfiguration()
                                       {
                                           Response = "hi"
                                       });
```

This will configure the stub running on local host with the deafult ports to return the text `hi` on the path `/api/hi`.

### Calling a configured URL

Once a URL has been configured it can be called at the route `api/{*resource}"` this will return the last response that was configured for that resource.

### Getting the calls to URL

If you wish to get the request that have been sent to a URL this can be done by calling the stub with a get request on the path `stub/{verb}/api/{*resource}` which will give you the last request or  `stub/{verb}/history/api/{*resource}` which will give you a list of all the request sent to to `api/{*resource}`. These calls can also be done with the stub client.

```cs
            var client = new StubClient($"https://localhost:{DefaultPortConfiguration.HttpsPort}");
            var lastRequest = await client.GetLastRequestAsync<order>(HttpMethod.Get, "hi")

```

This will return an `option<RequstRecort<order>>` representing the last request sent to `api/hi`. In this case the payload that the stub recived will be serialized to an order object and placed into a request record along with other information about the request. The StubClient returns request records wrapped in an option which is an abstraction to deal with not finding the information or the request otherwise failing.

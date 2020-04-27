# UKHO.ConfigurableStub

This package provides a stub for API calls that can be configured to return pre-defined responses based upon HTTP calls made to it during runtime.

## How to use the Stub

### Including the stub in your project

The stub is a .net core web application which is packaged upon build into a NuGet package. To include the stub in your project, add a reference to the NuGet package.

### Running up the stub

There are a few way to start the stub listed in the sections below.

#### Using the Stub Manager

You can start the stub using the stub manager

```cs
            var stubManager = new StubManager();
            stubManager.Start(Console.Out, 5000, 5001);
```

This snippet starts the stub in a separate process on ports 5000 and 5001. It redirects the stub standard output to the console. After the stub has finished starting up and is ready to accept requests it returns. The stub uses a self-signed certificate that is valid for 40 minutes.

#### Start using the startup methods

You can use start the stub using the static methods in the `StubStartup` class.

```cs
StubStartup.StartStub(44367, 45678);
```

This starts the stub on port 44367 for https and port 45678 for http using a self-signed certificate that is valid for 40 minutes. This call block the executing thread for as long as the stub is running.

### Configuring a call

To configure a call to the stub you should post to the stub on the route `stub/{verb}/api/{*resource}"`where verb is the http verb and resource is the path you wish to configure. With this call you should include the `RouteConfiguration` object in the body serialised as JSON, this will specify the response to the configured call. You can also use the stub client to do so which will call the stub for you.

```cs
            var client = new StubClient($"https://localhost:{DefaultPortConfiguration.HttpsPort}");
            await client.ConfigureRouteAsync(HttpMethod.Get,
                                       "hi",
                                       new RouteConfiguration()
                                       {
                                           Response = "hi"
                                       });
```

The above call configures the stub running on localhost with the default ports to return the text `hi` on the path `/api/hi`.

### Calling a configured URL

Once a URL has been configured it can be called at the route `api/{*resource}"` this returns the last response that was configured for that resource.

### Getting the calls to URL

If you wish to get the requests that have been sent to a URL this can be done by calling the stub with a get request on:

- `stub/{verb}/api/{*resource}` to get the last request
- `stub/{verb}/history/api/{*resource}` to get a list of all the requests

sent to to `api/{*resource}`. These calls can also be made using the stub client.

```cs
            var client = new StubClient($"https://localhost:{DefaultPortConfiguration.HttpsPort}");
            var lastRequest = await client.GetLastRequestAsync<order>(HttpMethod.Get, "hi")

```

This returns an `Option<RequestRecord<order>>` representing the last request sent to `api/hi`. In this case the payload that the stub received will be serialized to an order object and placed into a `RequestRecord` along with other information about the request. The `StubClient` returns `RequestRecord`s wrapped in an `Option` which is an abstraction to deal with not finding the information or the request otherwise failing.

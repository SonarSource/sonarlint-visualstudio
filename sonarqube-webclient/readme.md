# Description

This repository contains a library for accessing the SonarQube Web API from .NET applications. It supports limited APIs that are used by SonarLint for Visual Studio and Scanner for MSBuild.

The library automatically calls the best API for the connected SonarQube version.



# Structure

- Request - SonarQube version-specific implementation of a certain Web API. Each request type consists of an interface and one or more implementations. The request objects' properties are converted to query string parameters.
- RequestFactory - a stateful factory for Request objects that is initialized with the version of the connected SonarQube.
- DefaultConfiguration - registers all supported version-Request pairs with the given RequestFactory.
- SonarQubeService - using a preconfigured RequestFactory, creates, configures and executes Requests.



# Adding new API

### New request

- Create a new interface for the request in `/Api`, containing all needed parameters (could be subset of the supported parameters of the API).
- Inherit from `IRequest<T>` when the response is a single object or non-paged collection, or `IPagedRequest<T>` when the response is a paged collection.
- Create a class in `Models` that represents the return value of the API.
- Continue with a **New version-specific implementation**

### New version-specific implementation

- Create a directory (if it does not exist) for the version you want to support in `/Api`. For example, `V5_40`.
- Create a class in that directory, name it after the interface.
  - Inherit from `RequestBase<T>` for simple requests that return non-paged data or `PagedRequestBase<T>` for requests that return paged data.
  - Override the `Path` property, return the relative path of the API. For example if the actual URL is `http://localhost:9000/api/issues/search`, the value should be: `api/issues/search`.
  - Implement the interface added in the **New Request** step. Decorate the properties with `JsonProperty` attributes to specify the corresponding query string parameter names.
  - Create one or more subclasses representing the JSON objects that are returned from the web API. Use `JsonProperty` attributes.
  - Override one or more methods of the base class to implement the needed functionality. Usually `ParseResponse` should be enough.
- Add a new method to `ISonarQubeService` and implement it in `SonarQubeService`. Usually the implementation should be something like this:

```
public async Task<SonarQubeSomething> GetSomethingAsync(string param1, int param2, CancellationToken token) =>
    await InvokeRequestAsync<IGetSomethingRequest, SonarQubeSomething>(
        request =>
        {
            request.Param1 = param1;
            request.Param2 = param2;
            ...
        },
        token);
```

- Add test class for the new service method. Implement tests for the common cases - API returns valid data, API returns error code, etc.



# Using the SonarQubeService

```
// Create a new instance of the service:
var service = new SonarQubeService(new HttpClientHandler(), "user-agent-string", logger);

// Connect to SonarQube
await service.ConnectAsync(connectionInfo, token);

// Invoke request
var something = await service.GetSomethingAsync(param1, param2, token);

// Disconnect
service.Disconnect();
```

The service will throw `HttpWebException` when it receives a response that cannot be handled. Make sure those exceptions are caught and correctly handled.



# Errors and Error Handling

When an unexpected HTTP status code is returned the service will throw an exception of type `HttpRequestException`, which contains a `WebException` as `InnerException`. When the `WebException` is `HttpWebException`, its `Response` property could be cast to `HttpWebResponse` and examined for various data such as `HttpStatusCode`, etc.

Why exception and not status code? Here is a [nice list](https://stackoverflow.com/questions/4670987/why-is-it-better-to-throw-an-exception-rather-than-return-an-error-code) of pros and cons.

NOTE: Some requests handle HTTP status codes differently and do not always throw exception (for example see `IGetNotificationsRequest`).


# Tips and Tricks

### Ignore a Request property from serialization

By default all `Request` properties will be serialized as query string parameters.

To ignore a non-virtual property use `[JsonIgnore]`. To ignore a virtual property do the following:
```
[JsonProperty("param3", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(null)]
public virtual string Param3
{
    get { return null; }
    set { /* not supported in this implementation */ }
}
```

Note: The `JsonIgnore` attribute cannot be undone.

### Add nonconfigurable query string parameter

Sometimes the Web API would need an argument that should not be user configurable, or it should depend on the value of another parameter. Add it as a normal property just to the implementation class:

```
[JsonProperty("param5")]
public virtual bool? Param5 => string.IsNullOrWhiteSpace(Param3) ? (bool?)true : null;
```

### Dates

Some APIs return or need Java-specific date format. Use the `JavaDateConverter` to serialize such properties:

```
[JsonProperty("from"), JsonConverter(typeof(JavaDateConverter))]
public virtual DateTimeOffset EventsSince { get; set; }
```


### Logging

The SonarQube.Client contains a logging interface - `ILogger`. To plug your own logger in you need to create an adapter.


### Reading non-JSON responses

Some APIs return protobuf. See the following request for a working implementation `V5_10.GetIssuesRequest`.

Some APIs return XML. See the following request for a working implementation `V5_20.GetRoslynExportProfileRequest`.


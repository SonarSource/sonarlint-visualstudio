# Overview

- `ISonarQubeService` is the public entry point to the API. It is the interface that's used in the consuming application, e.g. SonarLint.
- `Models` contains the classes that represent application-specific data returned by the APIs. `ISonarQubeService` returns instances of the classes in `Models`.
- `Api/Requests` contains abstractions and concrete implementations of single API requests. Each implementation represents the API requests and response for specific version of SonarQube. Multiple implementations could exist for the different supported versions of SonarQube.
- `SonarQubeService` is the implementation of `ISonarQubeService`, which uses `RequestFactory` to create instances of the `Api\Requests` classes. The request instances are then configured and executed in each `ISonarQubeService` method implementation.
- `DefaultCofiguration` contains the mappings for SonarQube versions and request implementations.
- `Messages` contains classes that deserialize from XML and protobuf.

# API implementation

An API request implementation consists of:
- data model (one or more classes in `Models`)
- `ISonarQubeService` method and corresponding `SonarQubeService` implementation
- request abstraction (interface in `Api\Requests`)
- one or more versioned request implementations (classes in versioned subfolders of `Api\Requests`) that contain both request and response definitions, and implement the corresponding request abstraction.
- configuration entry (a line in `DefaultConfiguration` that registeres the request implementation with the corresponding SonarQube version)

# Consuming new API

1. Create a new class in `Models`, representing the return value of the API that will be used in the application. This does not need to have the same structure as the returned JSON. For example `SonarQubeBranch`.
2. Add properties to the model.
3. In `Api\Requests` create an interface for the new request, name it after the API. For example `IGetBranchesRequest`.
4. Add properties to the interface for each of the query string arguments in the API. Add `JsonProperty` attributes to specify the exact web API parameter names.
5. `IGetBranchesRequest` should implement `IRequest<T>` or `IPagedRequest<T>` depending on the API return values. For example it could be `IRequest<SonarQubeBranch>` for single value, `IRequest<SonarQubeBranch[]>` for multiple values in one page or `IPagedRequest<SonarQubeBranch>` for multiple values in multiple pages (no array here!).
6. Add method to `ISonarQubeService` that returns `SonarQubeBranch` or `IList<SonarQubeBranch>`, depending on the return values of the service, implement in SonarQubeService. Do not forget to initialize the request parameters.
7. Add concrete implementations of the API

# Adding API implementation

1. Determine what version of SonarQube needs to be supported (let's say 6.7)
2. Create subfolder in `Api\Requests\` named after the version - for example `V6_70`
3. Create a new class in that folder, named after the consumed API interface - for example `GetBranchesRequest`
4. Change the base class to `RequestBase<SonarQubeBranch>` or `PagedRequestBase<SonarQubeBranch>`, depending on whether the API returns single page or multiple pages with values.
5. Add the API interface to the class, e.g. `IGetBranchesRequest`
6. Override `Path` and return the relative url to the API. For example `api/branches`.
7. Override the required methods, e.g. usually it is only `ParseResponse`. In the implementation you need to convert the argument `string response` to SonarQubeBranch instance. Usually the best way to do it is to create a private subclass representing the exact JSON that's returned from the API, then deserialize the string and convert that class to `SonarQubeBranch`. In this case when a new implementation is added SonarQubeBranch will not change.
8. Open `Services\DefaultConfiguration` and register the new class and implementation with the chosen version.


# Handling API change

This is basically the same as Adding concrete implementation.
- if the new API accepts additional parameters, new properties should be added to the request interface. DO NOT remove properties. If a property should not be sent to a particular version of the API, use `JsonIgnore` attribute in the corresponding implementation to hide the property.
- you could copy or derive from an existing implementation. Deriving is probably less code, but could become more complex in the long term, especially if multiple versions of that request are implemented.
- you could use different base classes in the different implementations.
- you could have as many versions as needed.

# Advanced

- Override `InvokeAsync` if special response code handling should be implemented
- Override `ReadResponseAsync` when the returned data is not JSON
- Override `HttpMethod` if the API responds to different HTTP methods









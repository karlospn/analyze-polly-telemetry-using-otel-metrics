# Analyze Polly Telemetry using Prometheus, Grafana and OpenTelemetry Metrics

Starting with version 8, Polly provides Telemetry for all built-in resilience strategies.    
This repository contains an example of how to send this Telemetry to Prometheus and Grafana for a more in-depth analysis using OpenTelemetry Metrics.

# **Content**

This repository contains the following applications:

![polly-metrics-components-diagram](https://raw.githubusercontent.com/karlospn/analyze-polly-telemetry-using-otel-metrics/main/docs/polly-metrics-components-diagram.png)

- A .NET WebAPI that uses Polly to improve resiliency when making HTTP calls to the ``https://jsonplaceholder.typicode.com/`` API.
    - Polly provides Telemetry for all built-in resilience strategies starting with version 8. It’s crucial to emphasize that the .NET WebApi does not work with versions of Polly earlier than version 8. 
- The WebApi uses the OpenTelemetry OTLP exporter package (``OpenTelemetry.Exporter.OpenTelemetryProtocol``) to send the Polly Telemetry to an OpenTelemetry Collector.
- A Prometheus server that fetches the Polly metric data from the OTEL Collector.
- A Grafana server that is preconfigured with a dashboard for visualizing the Polly metrics sent by the WebAPI.


# **Application**

The application is a .NET 7 WebApi that makes calls to the ``jsonplaceholder.typicode.com`` API and returns the result.

The application features 2 endpoints: ``/Comments`` and ``/Users``.

## **/Comments endpoint**

This endpoint  makes a call to the ``https://jsonplaceholder.typicode.com/posts/{commentId}/comments`` endpoint and returns the result. 

To invoke the TypiCode API, the app utilizes an HttpClient with a Polly Retry strategy attached to it.    

- The following code snippet shows how the HttpClient is registered in the Dependency Injection (DI) container and also how the Polly Retry Strategy is attached to it.

```csharp
builder.Services.AddHttpClient("typicode-comments", c =>
{
    c.BaseAddress = new Uri(builder.Configuration.GetValue<string>("TypiCodeBaseUri") ??
                            throw new InvalidOperationException());
    c.DefaultRequestHeaders.Add("accept", "application/json");

}).AddPolicyHandler(PollyResiliencePipelines.CreateRetryStrategy().AsAsyncPolicy());
```

- And this is how the Polly Retry Strategy is created:

```csharp
public static ResiliencePipeline<HttpResponseMessage> CreateRetryStrategy()
{
    var retry = new ResiliencePipelineBuilder<HttpResponseMessage>
    {
        Name = "TypiCodeCommentsRetryPipeline"
    };

    var pipeline = retry.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<Exception>()
            .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError),
        Name = "RetryStrategy",
        MaxRetryAttempts = 5,
        Delay = TimeSpan.FromSeconds(5),
        OnRetry = arg =>
        {
            Console.WriteLine("OnRetry, Attempt: {0}", arg.AttemptNumber);
            return default;
        }
    })
    .AddTimeout(TimeSpan.FromSeconds(5))
    .ConfigureTelemetry(LoggerFactory.Create(bld => bld.AddConsole()))
    .Build();

    return pipeline;
}
```

## **/Users endpoint**

This endpoint makes a call to the ``https://jsonplaceholder.typicode.com/users/{userId}`` endpoint and returns the result.

To invoke the TypiCode API, the app utilizes an HttpClient with a Polly Circuit Breaker strategy attached to it.    

- The following code snippet shows how the HttpClient is registered in the Dependency Injection (DI) container and also how the Polly Circuit Breaker Strategy is attached to it.

```csharp
builder.Services.AddHttpClient("typicode-users", c =>
{
    c.BaseAddress = new Uri(builder.Configuration.GetValue<string>("TypiCodeBaseUri") ?? 
                            throw new InvalidOperationException());
    c.DefaultRequestHeaders.Add("accept", "application/json");

}).AddPolicyHandler(PollyResiliencePipelines.CreateCircuitBreakerStrategy().AsAsyncPolicy());
```

- And here is the creation of the Polly Circuit Breaker strategy:

```csharp
public static ResiliencePipeline<HttpResponseMessage> CreateCircuitBreakerStrategy()
{

    var circuitBreaker = new ResiliencePipelineBuilder<HttpResponseMessage>
    {
        Name = "TypiCodeUsersCircuitBreakerPipeline"
    };

    var pipeline = circuitBreaker.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage> 
    {
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r => r.StatusCode == HttpStatusCode.InternalServerError),
        Name = "CircuitBreakerStrategy",
        BreakDuration = TimeSpan.FromSeconds(15),
        FailureRatio = .3,
        MinimumThroughput = 5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        OnOpened = arg =>
        {
            Console.WriteLine("Circuit Breaker Opened, Duration: {0}", arg.BreakDuration);
            return default;
        },
        OnClosed = _ =>
        {
            Console.WriteLine("Circuit Breaker Closed");
            return default;
        },
        OnHalfOpened = _ =>
        {
            Console.Write("Circuit Breaker Half Opened");
            return default;
        }
    })
    .ConfigureTelemetry(new TelemetryOptions
    {
        MeteringEnrichers = { new CircuitBreakerMetersEnricher() }
    })
    .Build();

    return pipeline;
}
```
## **Send Polly Telemetry to the OTEL Collector using OpenTelemetry Metrics**

To transmit the Telemetry generated by the Polly strategies created above, we utilize OpenTelemetry Metrics.

The metrics are forwarded to an OpenTelemetry Collector using the ``AddOtlpExporter`` extension method.

To capture the metrics generated by Polly, it is necessary to register the "Polly" Meter using the ``AddMeter("Polly")`` extension method.

- The following code snippet shows how OpenTelemetry Metrics is configured.

```csharp
builder.Services.AddOpenTelemetry().WithMetrics(opts => opts
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Pollyv8.WebApi"))
    .AddMeter("Polly")
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri(builder.Configuration.GetValue<string>("OtlpEndpointUri") 
                                    ?? throw new InvalidOperationException());
    }));
```

# **External dependencies**

- OpenTelemetry Collector
- Prometheus
- Grafana

# **How to run the app**

The repository contains a ``docker-compose`` that starts up the .NET WebApi and its external dependencies.   
The external dependencies (OpenTelemetry Collector, Prometheus and Grafana) are already preconfigured, so you don't need to perform any additional setup.

- The OpenTelemetry Collector is already setup to export the metrics to Prometheus.
- Prometheus is configured to receive metric data from the OpenTelemetry Collector.
- Grafana has the Prometheus connector preconfigured and includes a custom dashboard for visualizing several Polly metrics emitted by the .NET WebApi.

Simply run ``docker-compose up``, and your good to go!

# **How to test the app**

To test the app:
- Run ``docker-compose up``.
- Navigate to ``http://localhost:5001/swagger``
- Interact with both endpoints a few times.

There is a **catch** to test this app, that you need to be aware of.     
Take a look at the ``docker-compose``:

```yaml
version: '3.8'

networks:
  polly:
    name: polly-network

services:
  prometheus:
    build: 
      context: ./scripts/prometheus
    ports:
      - 9090:9090
    networks:
      - polly

  grafana:
    build: 
      context: ./scripts/grafana
    depends_on:
      - prometheus
    ports:
      - 3000:3000
    networks:
      - polly
  
  otel-collector:
    image: otel/opentelemetry-collector:0.73.0
    command: ["--config=/etc/otel-collector-config.yaml"]
    volumes:
      - ./scripts/otel-collector/otel-collector-config.yaml:/etc/otel-collector-config.yaml
    ports:
      - "8888:8888" 
      - "8889:8889" 
      - "13133:13133"
      - "4317:4317"
    networks:
      - polly

  app:
    build:
      context: ./src/PollyTelemetryDemo.WebApi
    depends_on:
      - otel-collector
    ports:
      - 5001:8080
    environment:
      TypiCodeBaseUri: https://jsonplceholder.typicode.com/
      OtlpEndpointUri: http://otel-collector:4317
    networks:
      - polly
```

As you can see, the app requires a couple of environment variables to function correctly:
- ``TypiCodeBaseUri``: The URI address of the TypiCode API.
- ``OtlpEndpointUri``: The URI address of the OpenTelemetry Collector

If you examine the value of ``TypiCodeBaseUri``, you'll notice a typo in the address. The correct address should be ``jsonplaceholder.typicode.com``, but there is a missing 'a' in it.

**This error is intentional**, we want to ensure that calls to TypiCode API fail so that the Polly strategies are executed. This way, we can generate an entire set of Polly metrics.   
You can fix the typo and run the ``docker-compose`` if you wish, but you won't see half of the Polly metrics, because some of the Polly strategies, like retries or circuit breaker, are only triggered when something goes wrong.

# **Output**

If you open Grafana after using the .NET WebAPI endpoints a few times, you will encounter a dashboard like this one.

## **Polly metrics dashboard**

> **This dashboard has only a few examples of what we can build using the metrics emitted by Polly, we could do more things with them.**

With this dashboard, we can address questions such as:

- _How many calls to the "https://jsonplaceholder.typicode.com/posts/{commentId}/comments" endpoint were retried within the last hour?_
  
- _How many calls to the "https://jsonplaceholder.typicode.com/posts/{commentId}/comments" endpoint were successful? And how many were retried?_

- _What types of exceptions were raised during the calls to "https://jsonplaceholder.typicode.com/posts/{commentId}/comments" in each retry?_

- _What is the average duration of each retry made to "https://jsonplaceholder.typicode.com/posts/{commentId}/comments"?_

- _How many calls were made to the "https://jsonplaceholder.typicode.com/users/{userId}" endpoint when the circuit was open, half open, and closed, respectively?_

- _What is the average response time for each call made to the "https://jsonplaceholder.typicode.com/users/{userId}" endpoint when the circuit is open, and how does it compare to the response time when an error is thrown and the circuit remains closed?_


![polly-metrics-dashboard](https://raw.githubusercontent.com/karlospn/analyze-polly-telemetry-using-otel-metrics/main/docs/polly-metrics-dashboard.png)




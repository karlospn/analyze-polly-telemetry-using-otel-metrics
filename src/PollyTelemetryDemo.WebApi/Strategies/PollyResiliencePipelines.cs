using System.Net;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Telemetry;

namespace PollyTelemetryDemo.WebApi.Strategies
{
    public static class PollyResiliencePipelines
    {
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
    }
}

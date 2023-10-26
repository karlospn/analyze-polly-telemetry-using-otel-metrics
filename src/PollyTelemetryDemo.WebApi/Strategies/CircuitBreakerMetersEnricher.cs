using Polly.CircuitBreaker;
using Polly.Telemetry;

namespace PollyTelemetryDemo.WebApi.Strategies;

internal class CircuitBreakerMetersEnricher : MeteringEnricher
{
    public override void Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)
    {
        if (context.TelemetryEvent.Arguments is OnCircuitOpenedArguments<TResult> onCircuitOpenedArgs)
        {
            context.Tags.Add(new("circuitbreaker.open.duration", onCircuitOpenedArgs.BreakDuration));
        }
    }
}
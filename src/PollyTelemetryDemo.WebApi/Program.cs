using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Polly;
using PollyTelemetryDemo.WebApi.Strategies;

namespace PollyTelemetryDemo.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHttpClient("typicode-comments", c =>
            {
                c.BaseAddress = new Uri(builder.Configuration.GetValue<string>("TypiCodeBaseUri") ??
                                        throw new InvalidOperationException());
                c.DefaultRequestHeaders.Add("accept", "application/json");

            }).AddPolicyHandler(PollyResiliencePipelines.CreateRetryStrategy().AsAsyncPolicy());

            builder.Services.AddHttpClient("typicode-users", c =>
            {
                c.BaseAddress = new Uri(builder.Configuration.GetValue<string>("TypiCodeBaseUri") ?? 
                                        throw new InvalidOperationException());
                c.DefaultRequestHeaders.Add("accept", "application/json");

            }).AddPolicyHandler(PollyResiliencePipelines.CreateCircuitBreakerStrategy().AsAsyncPolicy());

            builder.Services.AddOpenTelemetry().WithMetrics(opts => opts
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Pollyv8.WebApi"))
                .AddMeter("Polly")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(builder.Configuration.GetValue<string>("OtlpEndpointUri") 
                                               ?? throw new InvalidOperationException());
                }));

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }

}

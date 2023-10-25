# Analyze Polly Telemetry using OpenTelemetry Metrics

Starting with version 8, Polly provides telemetry for all built-in resilience strategies. This repository contains an example of how to send this telemetry to Prometheus for more in-depth analysis using OpenTelemetry Metrics

# **Content**

This repository contains the following applications:


- A .NET WebAPI that uses Polly to improve resiliency when making HTTP calls to ``https://jsonplaceholder.typicode.com/``
  - The WebApi uses the OpenTelemetry OTLP exporter package (``OpenTelemetry.Exporter.OpenTelemetryProtocol``) to send the Polly telemetry to the OpenTelemetry Collector.
- The Prometheus server obtains the metric data from the OpenTelemetry Collector.
- The Grafana server comes preconfigured with a few dashboards to visualize the OpenTelemetry metrics emitted by the BookStore WebApi.



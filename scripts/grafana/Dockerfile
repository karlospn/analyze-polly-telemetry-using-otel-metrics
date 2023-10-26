FROM grafana/grafana:9.4.3
COPY ./prometheus-connector.yml  /etc/grafana/provisioning/datasources/ 
COPY ./prometheus-dashboards.yml  /etc/grafana/provisioning/dashboards/
COPY ./dashboards/polly-metrics-dashboard.json  /var/lib/grafana/dashboards/
# Lab 4 Scaling Setup

This folder contains a horizontal scaling setup for DonetskWaterHope backend:

- `docker-compose.scale.yml` starts PostgreSQL, one migration container, scalable API replicas, Nginx load balancer, and optional Locust.
- `nginx.conf` balances requests between API replicas.
- `locust/locustfile.py` contains read-heavy load tests for login, health, devices, consumption, alerts, and tickets.

## Run Backend With 1 API Instance

```bash
docker compose -f scaling/docker-compose.scale.yml up --build --scale api=1
```

API through load balancer:

```text
http://localhost:8088
```

Health check:

```bash
curl -i http://localhost:8088/health
```

The response contains `X-Backend-Instance`, which can be used to verify which API container handled the request.

## Scale API Horizontally

```bash
docker compose -f scaling/docker-compose.scale.yml up --build --scale api=3
```

To reduce capacity:

```bash
docker compose -f scaling/docker-compose.scale.yml up --scale api=1
```

## Run Locust

```bash
docker compose -f scaling/docker-compose.scale.yml --profile loadtest up --build --scale api=1
```

Open:

```text
http://localhost:8089
```

Recommended test sequence:

1. Run with `--scale api=1`, 50 users, 5 users/s.
2. Run with `--scale api=2`, 100 users, 10 users/s.
3. Run with `--scale api=3`, 150 users, 15 users/s.

Compare requests per second, average response time, p95 response time, and error rate.

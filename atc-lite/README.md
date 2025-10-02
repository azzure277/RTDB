# ATC-Lite: Real-Time Air Traffic Control Demo

## What is this?
A meaningful real-time database example: an air traffic control system that analyzes constantly changing data like aircraft positions, fuel, and weather to make split-second decisions for safe and efficient landings, where delays in data processing can have severe consequences.

This project demonstrates:
- Ingesting high-velocity streams (aircraft telemetry) with sub-second latency
- Maintaining a live, queryable state (Redis) for conflict detection and sequencing
- Pushing instant advisories to a dashboard UI
- End-to-end real-time data flow using .NET, Kafka (Redpanda), Redis, and a minimal web dashboard

## How it works
- **Ingest.Sim** simulates aircraft positions and sends them to Kafka
- **Processor** consumes the stream, updates Redis, and computes landing sequences
- **Tower.Web** displays live traffic and sequencing in a browser dashboard

## Why real-time matters
In air traffic control, delays in data processing can have severe consequences. This system proves:
- Data is processed and visible within milliseconds
- Controllers always see the latest state
- The system can be extended for more complex logic (weather, fuel, wake separation, etc.)

## How to run
See the project instructions in the repo for setup and usage.

---

*This project is for educational/demo purposes and is not for use in real-world ATC operations.*

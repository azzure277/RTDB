# ATC-Lite: Real-Time Air Traffic Control System

## What is this?
A real-time air traffic control system that analyzes live aircraft data to make split-second decisions for safe and efficient air traffic management. The system demonstrates high-velocity data processing where delays can have severe consequences.

This project showcases:
- **Real-world data ingestion** from OpenSky Network (live aircraft positions worldwide)
- **Simulated data generation** for testing and development
- **Stream processing** with sub-second latency using Kafka
- **Real-time state management** with Redis for conflict detection and sequencing
- **Live dashboard** with instant updates via SignalR
- **Microservices architecture** using .NET 8, Docker, and modern cloud-native patterns

## Architecture

### Core Components
- **Ingest.OpenSky**: Fetches real-time aircraft data from OpenSky Network API
- **Ingest.Sim**: Generates simulated aircraft data for testing (SFO approach patterns)
- **Processor**: Consumes data streams, maintains aircraft state, and computes landing sequences
- **Tower.Web**: Web API and dashboard for real-time traffic visualization and control

### Data Flow
1. **Data Sources**: Real OpenSky data OR simulated aircraft
2. **Message Bus**: Kafka streams aircraft position events
3. **Processing**: Real-time separation monitoring and sequence optimization
4. **Storage**: Redis maintains current aircraft state
5. **API**: RESTful endpoints with SignalR for live updates
6. **Dashboard**: Real-time web interface for air traffic controllers

## Features

### ✈️ **Real Aircraft Data**
- Live data from 1000+ aircraft worldwide via OpenSky Network
- Regional filtering (SFO Bay Area, LAX region)
- Automatic data conversion and validation

### 🛡️ **Safety Systems**
- **Separation Monitoring**: 3-6 nautical mile wake turbulence separation
- **Conflict Detection**: Real-time alerts for aircraft too close
- **Landing Sequence**: Automated optimal landing order

### 📊 **Real-Time Analytics**
- Aircraft count and density monitoring
- Fuel level tracking and low-fuel alerts
- Performance metrics and system health

### 🌐 **Modern Tech Stack**
- **.NET 8**: High-performance, cross-platform runtime
- **Apache Kafka**: Scalable event streaming
- **Redis**: Ultra-fast in-memory state store
- **SignalR**: Real-time web communication
- **Docker**: Containerized deployment

## Quick Start

### Option 1: Real Aircraft Data (Recommended)
```bash
# Clone and navigate to project
git clone https://github.com/azzure277/RTDB.git
cd RTDB

# Start all services with real OpenSky data
docker-compose up

# Access dashboard at http://localhost:5000
```

### Option 2: Simulated Data Only
```bash
# Start with simulation profile
docker-compose --profile simulation up
```

## Configuration

### Environment Variables
- `REGION`: Geographic area to monitor (`SFO` or `LAX`)
- `INTERVAL_SECONDS`: Data fetch interval (default: 30s)
- `KAFKA`: Kafka bootstrap servers
- `TOPIC`: Aircraft position topic name

### Regional Coverage
- **SFO**: San Francisco Bay Area (100nm radius)
- **LAX**: Los Angeles Area (100nm radius)

## Why Real-Time Matters

In air traffic control, data freshness is critical:
- **Safety**: Prevent mid-air collisions with real-time separation monitoring
- **Efficiency**: Optimize landing sequences to reduce delays
- **Scalability**: Handle 1000+ aircraft simultaneously
- **Reliability**: Process data with sub-second latency

This system proves real-time data processing capabilities essential for mission-critical applications.

## Documentation

- [OpenSky Integration Guide](docs/OpenSky-Integration.md)
- [Architecture Details](PLAN.md)
- [API Documentation](src/Tower.Web/README.md)

## Development

```bash
# Build solution
dotnet build atc-lite.sln

# Run tests
dotnet test

# Start individual services
dotnet run --project src/Ingest.OpenSky
dotnet run --project src/Processor  
dotnet run --project src/Tower.Web
```

---

*This project demonstrates real-time data processing concepts and is for educational purposes. Not for use in actual air traffic control operations.*

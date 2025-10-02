# OpenSky Network Integration

This service integrates real-time aircraft data from the [OpenSky Network](https://opensky-network.org/) into the ATC system.

## Features

- **Real-time Data**: Fetches live aircraft positions from OpenSky Network API
- **Regional Filtering**: Supports filtering by geographical regions (SFO, LAX areas)
- **Kafka Integration**: Publishes aircraft positions to Kafka for processing
- **Rate Limiting**: Configurable interval to respect OpenSky API limits
- **Error Handling**: Robust error handling and retry logic

## Configuration

Environment variables:

- `KAFKA`: Kafka bootstrap servers (default: `localhost:9092`)
- `TOPIC`: Kafka topic for aircraft positions (default: `aircraft.position`)
- `INTERVAL_SECONDS`: Data fetch interval in seconds (default: `30`)
- `REGION`: Geographic region to monitor (`SFO` or `LAX`, default: `SFO`)

## OpenSky API

The service uses the OpenSky Network's REST API:
- **Endpoint**: `https://opensky-network.org/api/states/all`
- **Rate Limit**: Please be respectful and don't exceed 1 request per 10 seconds
- **Data**: Provides real-time aircraft state vectors including position, altitude, speed, etc.

### Regional Coverage

**SFO Region** (San Francisco Bay Area):
- Latitude: 36.12° to 39.12° N
- Longitude: 123.88° to 120.88° W
- Covers approximately 100nm radius around SFO

**LAX Region** (Los Angeles Area):
- Latitude: 32.44° to 35.44° N  
- Longitude: 119.91° to 116.91° W
- Covers approximately 100nm radius around LAX

## Data Transformation

The service converts OpenSky state vectors to our internal `PositionEvent` format:

- **ICAO24**: Aircraft identifier (converted to uppercase)
- **Callsign**: Flight number or generated identifier
- **Position**: Latitude/Longitude in decimal degrees
- **Altitude**: Converted from meters to feet
- **Speed**: Converted from m/s to knots
- **Heading**: True track in degrees
- **Vertical Speed**: Converted from m/s to feet per minute
- **Fuel**: Estimated based on aircraft characteristics

## Running the Service

### Standalone
```bash
dotnet run --project src/Ingest.OpenSky
```

### Docker
```bash
docker build -f Dockerfile.opensky -t atc-opensky .
docker run -e KAFKA=kafka:9092 -e REGION=SFO atc-opensky
```

### Docker Compose
```bash
# Start with real OpenSky data
docker-compose up

# Start with simulation data only
docker-compose --profile simulation up
```

## Monitoring

The service logs:
- Aircraft count per fetch cycle
- Processing time for each batch
- API errors and connectivity issues
- Individual aircraft position updates (debug level)

## Notes

- **API Limitations**: OpenSky Network is free but has rate limits
- **Data Quality**: Real-world data may have gaps or inconsistencies
- **Coverage**: Aircraft must be equipped with ADS-B transponders
- **Latency**: Data is typically 5-15 seconds behind real-time
- **Authentication**: Anonymous access supported, registered users get higher limits

For production deployments, consider registering for an OpenSky Network account to get higher API limits and more reliable access.
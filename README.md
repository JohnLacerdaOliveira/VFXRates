# VFXRates

**VFXRates** is a .NET 8 Web API application for managing foreign exchange rates. It provides endpoints to retrieve, create, update, and delete FX rates while integrating with an external API (Alpha Vantage) for live rate data. The project demonstrates clean architecture principles by separating concerns across API, Application, Domain, and Infrastructure layers.

## Table of Contents

- [Key Features](#key-features)
- [Architecture & Design](#architecture--design)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running Locally with Visual Studio](#running-locally-with-visual-studio)
  - [Running via Docker Compose](#running-via-docker-compose)
- [Testing](#testing)
- [Environment & Configuration](#environment--configuration)
- [Design Considerations](#design-considerations)
- [License](#license)

## Key Features

- **RESTful Endpoints:**  
  CRUD operations for FX rates via endpoints such as GET, POST, PUT, and DELETE.

- **External API Integration:**  
  Automatically retrieves FX rates from the [Alpha Vantage](https://www.alphavantage.co/) API when a rate is not found in the local database.

- **Entity Framework Core:**  
  Uses EF Core with SQL Server (or In-Memory for integration tests) for data persistence and migration management.

- **Middleware-Based Error Handling:**  
  Global error handling via a custom middleware to ensure a consistent error response format.

- **Logging:**  
  Extensive use of logging across the application to aid in debugging and monitoring key operations.

- **Docker Support:**  
  A Docker Compose configuration is provided to run the application along with a SQL Server container.

## Architecture & Design

- **API Layer:**  
  Contains the controllers that expose REST endpoints.

- **Application Layer:**  
  Contains business logic in services (e.g., `FxRateService`) and Data Transfer Objects (DTOs) for communication between layers.

- **Domain Layer:**  
  Contains core domain entities such as the `FxRate` entity.

- **Infrastructure Layer:**  
  Contains repository implementations (e.g., `FxRateRepository`) and the EF Core `DbContext` (`FxRatesDbContext`).  
  This layer also contains integration with external services (e.g., the `AlphaVantageApiClient`).

- **Configuration & Startup:**  
  Startup configuration has been organized into extension methods to promote separation of concerns. Logging is configured to output to the console and key startup events are logged.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server 2019](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or use the provided Docker container)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (if running with Docker)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (or your preferred IDE)
- (Optional) [DotNetEnv](https://github.com/tonerdo/dotnet-env) for environment variable loading

### Running Locally with Visual Studio

1. **Clone the Repository:**

   ```bash
   git clone https://github.com/yourusername/VFXRates.git
   cd VFXRates```
 
2. **Configure User Secrets / Environment Variables:**
If you wish to override any settings (e.g., connection strings, API keys), use User Secrets or set environment variables in your IDE’s launch settings.

3. **Set Up the Database:**

The application automatically runs EF Core migrations on startup.
In development, the database will be deleted and re-created on each run.

4. **Run the Application:**

Press F5 or use Visual Studio’s run command.
The API will be hosted on the URL specified in your launch settings (e.g., http://localhost:5208).

5. **Testing Endpoints:**

Navigate to /swagger (in development) to explore the API endpoints via Swagger UI.

### Running via Docker Compose

1. **Ensure Docker Desktop is Running.**

2. **Configure the Environment:**

Create a .env file in the root directory with the following variables (update with your own values):
	# .env file
	SA_PASSWORD=YourStrongPassword123
SQLSERVER_CONNECTIONSTRING=Server=host.docker.internal,1433;Database=FxRatesDb;User Id=sa;Password=YourStrongPassword123;TrustServerCertificate=True;
API_KEY=EPSZTUZ68K5RRBAD
	
	# For appsettings.json placeholders
	ConnectionStrings__FxRatesDb=Server=sqlserver,1433;Database=FxRatesDb;User Id=sa;Password=YourStrongPassword123;TrustServerCertificate=True;
	AlphaVantage__ApiKey=YourActualAlphaVantageApiKey

3. **Build and Run the Containers:**

 bash
docker-compose up --build

This command builds the API image and starts two containers:
sqlserver: Running SQL Server 2019.
vfxrates-api: The API application.

4. **Accessing the API:**

The API listens on the ports defined in your docker-compose.yml (e.g., 5208 for HTTP).
If you are in development, you can access Swagger UI at http://localhost:5208/swagger.

5. **Stopping the Containers:**
To stop the containers, press Ctrl+C in the terminal or use Docker Desktop to stop the containers.

## Message Broker (RabbitMQ)
In addition to the core functionality, this application demonstrates a simple integration with a message broker (RabbitMQ) for event publishing. Every time a new FX rate is added, an event is raised. This event can be consumed by a subscriber (for example, a service that logs the event or triggers additional processing).

**How to Test the Message Broker**
Once the RabbitMQ container is running, open your browser and navigate to http://localhost:15672.
The default credentials are usually:

Username: guest
Password: guest

**Testing Event Publishing:**
The application publishes an event to RabbitMQ when a new FX rate is created. You can use the management UI to verify that messages are being queued. Additionally, you can create a simple subscriber service to log the events to the console.

## Testing

**Unit Tests:**
Located under the tests/VFXRates.Application.UnitTests folder.
Use your IDE’s test explorer or run the tests using the following command:

dotnet test tests/VFXRates.Application.UnitTests


**Integration Tests:**
Located under the tests/VFXRates.API.IntegrationTests folder.
These tests use an in-memory database or Docker containers to test the full behavior of the API.

Run them with:
dotnet test tests/VFXRates.API.IntegrationTests

## Environment & Configuration

**App Settings:**
The application configuration is stored in appsettings.json with placeholder values.
Sensitive data (e.g., connection strings, API keys) should be overridden using environment variables or user secrets.

**Logging:**
Logging is configured via the built-in logging provider (console) and additional configuration can be found in appsettings.json.

**HTTPS Configuration:**
The application binds to URLs defined by the ASPNETCORE_URLS environment variable.
In production, ensure that you provide a valid certificate if using HTTPS.

## Design Considerations 

**Separation of Concerns:**
The startup configuration is organized into extension methods to keep the Program class clean.
Each layer (API, Application, Domain, Infrastructure) is responsible for a specific part of the application logic.

**Error Handling:**
Global error handling is implemented via middleware, ensuring that exceptions are caught and a consistent error response is returned.

**Scalability:**
The repository and service patterns used make it easy to extend functionality without impacting other layers.

## Licence
This project is licensed under the MIT License. See the LICENSE file for details.
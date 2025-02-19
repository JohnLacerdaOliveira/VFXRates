# VFXRates

**VFXRates** is a .NET 8 Web API application for managing foreign exchange rates. It provides endpoints to retrieve, create, update, and delete FX rates while integrating with an external API (Alpha Vantage) for live rate data. The project demonstrates clean architecture principles by separating concerns across API, Application, Domain, and Infrastructure layers.

<img src="https://github.com/user-attachments/assets/3c114561-9bc6-49bb-8090-bd573e2470d6" alt="Description" style="width:100%" style="display: block; margin: 0 auto;" />


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
- [License](#licence)

## Key Features

- **RESTful Endpoints:**  
  CRUD operations for FX rates via endpoints such as GET, POST, PUT, and DELETE.

- **External API Integration:**  
  Automatically retrieves FX rates from the [Alpha Vantage](https://www.alphavantage.co/) API and stores them locally when a rate is not found in the local database.

- **Entity Framework Core:**  
  Uses EF Core with SQL Server (or In-Memory for integration tests) for data persistence and migration management.

- **Middleware-Based Error Handling:**  
  Global error handling via a custom middleware to ensure a consistent error response format.

- **Logging:**  
  Extensive use of logging across the application to aid in debugging and monitoring key operations.

- **Docker Support:**  
  A Docker Compose configuration is provided to run the application along with a SQL Server container and RabbitMQ server.

- **Message Publishing:**
  Integrates RabbitMQ to publish events (e.g., when a new FX rate is added), promoting loose coupling and asynchronous processing.

## Architecture & Design

- **API Layer:**  
  Contains the controllers that expose REST endpoints.

- **Application Layer:**  
  Contains business logic in services (e.g., [`FxRateService`](src/VFXRates.Application/Services/FxRatesService.cs)) and Data Transfer Objects (DTOs) for communication between layers.

- **Domain Layer:**  
  Contains core domain entities such as the [`FxRate`](src/VFXRates.Domain/Entities/FxRate.cs) entity.

- **Infrastructure Layer:**  
  Contains repository implementations (e.g., [`FxRateRepository`](src/VFXRates.Infrastructure/Repositories/FxRateRepository.cs)) and the EF Core [`DbContext`](src/VFXRates.Infrastructure/Data/DbContext/FxRatesDbContext.cs).  
  This layer also contains integration with external services (e.g., the [`AlphaVantageApiClient`](src/VFXRates.Application/Services/AlphaVantageApiClient.cs)) and message publishing thru (e.g., [` RabbitMqPublisher`](src/VFXRates.Infrastructure/Messaging/RabbitMqPublisher.cs)).

- **Configuration & Startup:**  
  Startup configuration has been organized into [`extension methods`](src/VFXRates.API/Extensions/) to promote separation of concerns. Logging is configured to output to the console and key startup events are logged.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server 2019](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or use the provided Docker container)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (or your preferred IDE)
- [DotNetEnv](https://github.com/tonerdo/dotnet-env) for environment variable loading

### Running Locally with Visual Studio

1. **Clone the Repository:**

   ```bash 
   git clone https://github.com/yourusername/VFXRates.git
   cd VFXRates
 
2. **Configure User Secrets / Environment Variables:**
If you wish to override any settings (e.g., connection strings, API keys), use User Secrets or set environment variables in your IDE’s launch settings.

3. **Set Up the Database:**

Make sure to start the sqlserver and rabbitMQ containers


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

 `bash 
docker-compose up --build`

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
The project includes comprehensive [`Unit Tests`](tests/VFXRates.Application.UnitTests/FxRateServiceTests.cs) that cover core business logic and repository interactions, ensuring the correctness of functionality and enabling early detection of bugs. These tests can be executed with the following command:

`dotnet test tests/VFXRates.Application.UnitTests`

<img src="https://github.com/user-attachments/assets/4e8ebd39-b8fc-4280-968b-fd4ea55c70ba" alt="Description" style="width:60%;" />


**Integration Tests:**
The project includes [`Integration Tests`](tests/VFXRates.API.IntegrationTests/FxRatesIntegrationTests.cs) that validate the complete end-to-end functionality of the application by simulating real-world scenarios with external dependencies. These tests ensure that the various layers of the application (API, Application, Domain, Infrastructure) work together as expected and help catch issues that might not surface in unit testing. They can be executed with the following command:

Use your IDE’s test explorer or run the tests using the following command:
`dotnet test tests/VFXRates.API.IntegrationTests`

<img src="https://github.com/user-attachments/assets/21c1dde9-c9c5-45a0-a701-ffc5c53494dd" alt="Description" style="width:60%;" />


## Environment & Configuration

**App Settings:**
The application configuration is stored in appsettings.json with placeholder values.
Sensitive data (e.g., connection strings, API keys) should be overridden using environment variables or user secrets thru a .env file added to the project's root folder.

**Logging:**
Logging is configured via the built-in logging provider (console) and additional configuration can be found in appsettings.json.

**HTTPS Configuration:**
The application binds to URLs defined by the ASPNETCORE_URLS environment variable.
In production, ensure that you provide a valid certificate if using HTTPS.

**Container Requirements:**
When running the application, launching from Visual Studio, ensure that the SQL Server and RabbitMQ containers are running, as these services are essential for data persistence and messaging functionality.

`docker-compose up selserver rabbitmq`

## Design Considerations 

**Separation of Concerns:**
The startup configuration is organized into extension methods to keep the Program class clean.
Each layer (API, Application, Domain, Infrastructure) is responsible for a specific part of the application logic.

**Dependency Injection**
The project makes extensive use of dependency injection to manage service lifetimes and reduce tight coupling between components. This approach improves testability, maintainability, and scalability, allowing components such as controllers, services, and repositories to be easily substituted or mocked.

**Error Handling:**
Global error handling is implemented via middleware, ensuring that exceptions are caught and a consistent error response is returned.

**Scalability:**
The repository and service patterns used make it easy to extend functionality without impacting other layers.

## Licence
This project is licensed under the MIT License. See the LICENSE file for details.

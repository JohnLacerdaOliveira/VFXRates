# VFXRates

**VFXRates** is an ASP.NET Core Web API that manages foreign exchange rates. It supports full CRUD operations on currency pairs, maintaining separate bid and ask prices. When a requested currency pair does not exist in the local database, the application fetches fresh FX rates from Alpha Vantage and stores them for future use.  

The application is designed with 100% integration test coverage, ensuring system reliability and correctness. It uses JWT authentication to secure API access and logs application events directly to the database for better monitoring and debugging. With support for multiple environments, including Development, IntegrationTest, and Production, it allows seamless configuration adjustments.  

For deployment, VFXRates provides Docker configuration, simplifying the process of running the application in a production environment. Additionally, it offers optional RabbitMQ integration, enabling event-based messaging whenever a new FX rate is added. The project follows modern best practices in scalable and maintainable ASP.NET Core development.  

<img src="https://github.com/user-attachments/assets/16a39eb0-fe6e-4750-a417-24b28554b94f" alt="Description" style="width:100%" style="display: block; margin: 0 auto;" />

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

- **CRUD  Endpoints:**  
  Create, Read, Update, and Delete currency pairs with bid and ask prices.

- **External API Integration:**  
  If the rate for a given currency pair isn’t found in the database, the application calls [Alpha Vantage](https://www.alphavantage.co/) API to retrieve and store it locally.

- **Entity Framework Core:**  
  Uses EF Core with SQL Server (or In-Memory for integration tests) for data persistence and migration management.
  Uses two diferent databases:
  
  - **FxRatesDev** - Used in Development mode and deleted after each run, ideal for testing and development.
  
  - **FxRatesProd** - Used in Production mode and with Docker, with persistance between sessions for production environment.
  
- **Authentication & Security**
  - Supports JWT-based authentication to secure endpoints.
  - Https support with http redirection.
    
- **Logging:**  
  Includes structured logging, where log entries can be persisted to a database for auditing and troubleshooting.
  
- **Docker Support:**  
  A Docker Compose configuration is provided to deploy the entire application and dependencies with a single command.

- **Event Publishing:**
  Integrates RabbitMQ to publish events (e.g., when a new FX rate is added), promoting loose coupling and asynchronous processing.

- **Tests:**  
  - Integration tests that bypass JWT authentication when running in a test environment.
  - Unit tests for core services to ensure robust functionality.
  
## Architecture & Design

- **API Layer:**  
Contains the controllers [`controllers`](src/VFXRates.API/Controllers/FxRatesController.cs) that expose REST endpoints.

- **Application Layer:**  
Contains business logic in services (e.g., [`FxRateService`](src/VFXRates.Application/Services/FxRatesService.cs)) and [`Transfer Objects (DTOs)`](src/VFXRates.Application/DTOs/) for communication between layers.

- **Domain Layer:**  
Contains core domain entities such as the [`FxRate`](src/VFXRates.Domain/Entities/FxRate.cs) entity.

- **Infrastructure Layer:**  
Contains repository implementations (e.g., [`FxRateRepository`](src/VFXRates.Infrastructure/Repositories/FxRateRepository.cs)) and the EF Core [`DbContext`](src/VFXRates.Infrastructure/Data/DbContext/FxRatesDbContext.cs).  
  This layer also contains integration with external services (e.g., the [`AlphaVantageApiClient`](src/VFXRates.Application/Services/AlphaVantageApiClient.cs)) and message publishing thru (e.g., [`RabbitMqPublisher`](src/VFXRates.Infrastructure/Messaging/RabbitMqPublisher.cs)).

- **Configuration & Startup:**  
  Startup configuration has been organized into [`extension methods`](src/VFXRates.API/Extensions/) to promote separation of concerns. Logging is configured to output to the console and key startup events are logged.

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (or your preferred IDE)
- [.Env secrets file](https://github.com/tonerdo/dotnet-env) for environment variable loading

### Running Locally with Visual Studio

1. **Clone the Repository:**

```cmd 
git clone https://github.com/JohnLacerdaOliveira/VFXRates.git
cd VFXRates
```
 
2. **Provide Environment Variables:**
Store a .Env file in the project's root directory with values for the following keys:

```text 
# .env file

# Docker Compose Environment Variables

SA_PASSWORD=
SQLSERVER_CONNECTIONSTRING=

API_KEY=

RABBITMQ_USER=
RABBITMQ_PASS=


# appsetting.json overrides

ConnectionStrings__FxRatesDevDb=

ConnectionStrings__FxRatesProdDb=

ConnectionStrings__FxRatesDockerDb=

Jwt__Secret=

AlphaVantage__ApiKey=

RabbitMQ__UserName=
RabbitMQ__Password=
RabbitMQ__Exchange=

# appsetting.Production.json overrides

Kestrel__Endpoints__Https__Certificate__Password=
```
3. **Create a HTTPS certificate:**
Run the following command on the projects root folder:

```cmd 
dotnet dev-certs https --trust
```
    
5. **Set Up the Database & RabbitMQ containers:**
   
Make sure to start the sqlserver and rabbitMQ containers by running the following command in the project's root directory:

```cmd 
docker-compose up sqlserver rabbitmq
```

The application automatically runs EF Core migrations on startup.
In Development, the database will be deleted and migrated anew on each run.
In Production, the database is persistent in between runs.

5. **Run the Application:**
Press F5 or use Visual Studio’s run command:

Run in Development for swagger support and utilize the application thru the provided UI at [https://localhost:7291](https://localhost:7291/swagger/index.html)
In Production the application launches in headless mode with no swagger support.

6. **Testing Endpoints:**
When ran in Develpment mode the application can be used thru Swagger's UI that lauches at startup

A complete [`Postman collection`](tests/VFXRates.TestUtilities/PostmanTests/VFXRates.postman_collection.json) of requests is also provided with the project, just import into Postman and use.
Make sure to lauch the application and run the tests in Development mode as to ensure database state for the tests to function corretly.

<img src="https://github.com/user-attachments/assets/b2c924cc-e4f9-4ab2-8dc5-7dd8d65e239c" alt="Description" style="width:100%" style="display: block; margin: 0 auto;" />

### Running via Docker Compose

1. **Ensure Docker Desktop is Running.**

2. **Configure the Environment:**
Create a .Env file in the root directory with the priviously (update keys with your own values):

3. **Create a HTTPS certificate:** 
Run the following command on the projects root folder:

```cmd 
dotnet dev-certs https --trust
```

4. **Build & Run the Containers:** 
Run the following command in the projects root directory:

 ```bash 
docker-compose up --build
```

This command builds the API image and starts all 3 containers: (allow a few minutes for the process to complete)

- **vfxrates-api**: The API application.
- **sqlserver**: Running SQL Server 2019.
- **rabbitmq**: Running the RabbitMQ messaging service (management UI available on http://localhost:15672/).

5. **Testing Endpoints:**
View point 5. of the previous

6. **Shutting down the application:**
To stop the application, press Ctrl+C in the terminal (allow a few seconds to stop the containers gracefully) and run the following command when it completes:

 ```bash 
docker-compose down
```

## Message Broker (RabbitMQ) 
In addition to the core functionality, this application demonstrates a simple integration with a message broker (RabbitMQ) for event publishing. Every time a new FX rate is added, an event is raised. This event can be consumed by a subscriber (for example, a service that logs the event or triggers additional processing).

<img src="https://github.com/user-attachments/assets/3a06d431-351d-468b-a5ad-7c73a9df32b1" alt="Description" style="width:60%;" />  

 
**How to Test the Message Broker**    
Once the RabbitMQ container is running, open your browser and navigate to [[https://localhost:15672](http://localhost:15672)]. 
The login credentials will be the ones set int the .Env file

**Testing Event Publishing**    
The application publishes an event to RabbitMQ when a new FX rate is created. You can use the management UI to verify that messages are being queued. Additionally, you can create a simple subscriber service to log the events to the console.

## Testing

**Unit Tests**  
The project includes comprehensive [`Unit Tests`](tests/VFXRates.Application.UnitTests/FxRateServiceTests.cs) that cover core business logic and repository interactions, ensuring the correctness of functionality and enabling early detection of bugs. These tests can be executed with the following command:

 ```bash 
dotnet test tests/VFXRates.Application.UnitTests
```

<img src="https://github.com/user-attachments/assets/38a2c1c9-2168-4ee3-b7f7-ee1f7894e2ba" alt="Description" style="width:60%;" />   


**Integration Tests**       
The project includes [`Integration Tests`](tests/VFXRates.API.IntegrationTests/FxRatesIntegrationTests.cs) that validate the complete end-to-end functionality of the application by simulating real-world scenarios with external dependencies. These tests ensure that the various layers of the application (API, Application, Domain, Infrastructure) work together as expected and help catch issues that might not surface in unit testing. They can be executed with the following command:

Use your IDE’s test explorer or run the tests using the following command:
`dotnet test tests/VFXRates.API.IntegrationTests`

<img src="https://github.com/user-attachments/assets/ad815483-0b9e-40a3-a0c2-29f30a7c1780" alt="Description" style="width:60%;" />

## Environment & Configuration  

**App Settings:**  
The core configuration is stored in appsettings.json with placeholder values that can be overridden using environment variables or user secrets via a .env file located at the project's root. This setup supports multiple environments, allowing you to tailor settings based on where the application is running.

**Environment-Specific Modes:**  

- ***Development*** (Transient Database, Swagger UI, LogConsole): The app will drop and re-create the database on every run, provides a Swagger UI for API testing and in addition to Db logs it start a console to display all framework's logs.
  
- ***Production*** (Persistent Database & Headless Mode): The app maintains a persistent database without UI and doesn't deploy a console Logging only to the database.
  
- ***Docker*** (Production-like Mode): The application runs with configurations similar to production.

**Container Requirements:**  
When running the application, launching from Visual Studio, ensure that the SQL Server and RabbitMQ containers are running, as these services are essential for data persistence and messaging functionality.

```cmd 
docker-compose up selserver rabbitmq
```  
  
**Logging:**  
Currently, the application is configured to log to the console using the built-in logging provider and also logs to a database table application specific logs only. In future versions, you could integrate more advanced logging mechanisms such as logging to files, a centralized logging system (e.g., ELK Stack, Azure Application Insights, or Seq), or even both, to facilitate improved monitoring and diagnostics in production environments.

<img src="https://github.com/user-attachments/assets/b300bd54-f7cf-42a0-a60c-60f15ca3b853" width="49%"> 
<img src="https://github.com/user-attachments/assets/ff9af0f6-f3d3-49a3-af98-375aa4595097" width="49%">  


**HTTPS Configuration:**  
The application binds to URLs defined by the ASPNETCORE_URLS environment variable.
In production, ensure that you provide a valid certificate if using HTTPS.


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

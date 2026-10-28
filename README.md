# Order Processing – Azure Functions (.NET 10)

A small **Azure Functions (C# / .NET 10, isolated worker)** project built while studying the course
**"Microsoft Azure From Zero to Hero - The Complete Guide"**.

It is the order-processing backend of the course's *ReadIt* sample app (an online bookstore):
when a customer places an order, the order JSON is POSTed to an HTTP-triggered function, which
stores it in Azure Blob Storage (or, in the Cosmos DB variant, logs it and can save it to Cosmos DB).

> The course originally built this on .NET Core 3.1 / Functions v3 (in-process). It has been
> upgraded to **.NET 10 / Functions v4 isolated worker model** – see [Upgrade Notes](#upgrade-notes).

---

## Table of Contents

- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Functions](#functions)
- [Data Model](#data-model)
- [Configuration](#configuration)
- [Running Locally](#running-locally)
- [Testing the Functions](#testing-the-functions)
- [Deploying to Azure](#deploying-to-azure)
- [Upgrade Notes](#upgrade-notes)
- [Azure Concepts Covered](#azure-concepts-covered)

---

## Architecture

```
                 HTTP POST (order JSON)
  Client  ───────────────────────────────►  ProcessOrderStorage  ──►  Blob Storage
 (ReadIt app,                                                        container: "orders"
  Postman, curl)                                                     <guid>.txt
                 HTTP POST (order JSON)
          ───────────────────────────────►  ProcessOrderCosmos   ──►  Logs
                                                                     (Cosmos DB output
                                                                      binding: optional)
```

The functions run in a separate **.NET 10 worker process** (isolated model) that talks to the
Functions v4 host. HTTP requests use **ASP.NET Core integration** (`HttpRequest` / `IActionResult`).

## Tech Stack

| Item                | Version / Value                                                    |
|---------------------|--------------------------------------------------------------------|
| Runtime             | Azure Functions **v4**, isolated worker (`dotnet-isolated`)        |
| Framework           | **.NET 10** (`net10.0`)                                            |
| Language            | C#                                                                 |
| Worker              | `Microsoft.Azure.Functions.Worker` 2.52.0 / `Worker.Sdk` 2.1.0     |
| HTTP                | `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` 2.1.1 |
| Blob Storage        | `Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs` 6.8.2  |
| Cosmos DB           | `Microsoft.Azure.Functions.Worker.Extensions.CosmosDB` 4.16.1      |
| Monitoring          | Application Insights (`Worker.ApplicationInsights` 2.51.0)         |
| JSON                | `Newtonsoft.Json` 13.0.4                                           |

## Project Structure

```
order/
├── Program.cs                # Worker host startup (ASP.NET Core integration, App Insights)
├── Order.cs                  # Order and OrderItem models
├── ProcessOrderStorage.cs    # HTTP trigger → writes order to Blob Storage
├── ProcessOrderCosmos.cs     # HTTP trigger → logs order (optional Cosmos DB output)
├── ordersample.json          # Sample request body for testing
├── host.json                 # Functions host config (logging, Cosmos DB extension)
├── order.csproj              # Project file & NuGet packages
├── order.sln                 # Visual Studio solution
└── .vscode/                  # VS Code tasks, debug & Azure Functions settings
```

`local.settings.json` (connection strings) is **not** committed – see [Configuration](#configuration).

## Functions

### 1. `ProcessOrderStorage`

| Property  | Value                                                                   |
|-----------|-------------------------------------------------------------------------|
| Trigger   | HTTP `POST`, `AuthorizationLevel.Anonymous`                             |
| Route     | `/api/ProcessOrderStorage`                                              |
| Binding   | `BlobContainerClient` for container **`orders`** (connection: `StorageConnectionString`) |
| Response  | `200 OK`                                                                |

**What it does**

1. Reads the raw request body (the order JSON).
2. Generates a unique blob name: `<GUID>.txt`.
3. Logs the blob name and the order.
4. Uploads the body as a blob into the `orders` container.

> The `orders` container must exist in the storage account.

### 2. `ProcessOrderCosmos`

| Property  | Value                                              |
|-----------|----------------------------------------------------|
| Trigger   | HTTP `POST`, `AuthorizationLevel.Anonymous`        |
| Route     | `/api/ProcessOrderCosmos`                          |
| Response  | `200 OK`                                           |

**What it does**

Reads the request body and logs it. The file also defines a `CosmosOrderResponse` class with a
**Cosmos DB output binding** that saves the order to:

- Database: `readit-orders`
- Container: `orders`
- Connection setting: `CosmosDBConnection`

To enable it, change the function's return type to `Task<CosmosOrderResponse>` and return the object
shown in the comment in [ProcessOrderCosmos.cs](ProcessOrderCosmos.cs).

## Data Model

Defined in [Order.cs](Order.cs):

```csharp
public class Order
{
    public string id { get; set; }            // Cosmos DB document id (new GUID by default)
    public int orderId { get; set; }          // random 0–999 by default
    public DateTime orderDate { get; set; }
    public double total { get; set; }
    public int priority { get; set; }         // random 1–3 by default
    public List<OrderItem> items { get; set; }
}

public class OrderItem
{
    public string name { get; set; }
    public int id { get; set; }
    public double price { get; set; }
}
```

Sample request ([ordersample.json](ordersample.json)):

```json
{
  "orderId": "54",
  "orderDate": "01/09/2020 17:34:02",
  "total": 55,
  "priority": 2,
  "items": [
    { "name": "Harry Potter", "id": "443", "price": 55 }
  ]
}
```

## Configuration

Create a `local.settings.json` file in the project root (it is git-ignored):

```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "StorageConnectionString": "<your-storage-account-connection-string>",
    "CosmosDBConnection": "<your-cosmos-db-connection-string>"
  }
}
```

| Setting                   | Used by                 | Where to get it                                      |
|---------------------------|-------------------------|------------------------------------------------------|
| `FUNCTIONS_WORKER_RUNTIME`| Functions host          | Must be `dotnet-isolated`                            |
| `AzureWebJobsStorage`     | Functions host          | Storage account connection string, or Azurite        |
| `StorageConnectionString` | `ProcessOrderStorage`   | Storage account → *Access keys*                      |
| `CosmosDBConnection`      | `ProcessOrderCosmos`    | Cosmos DB account → *Keys* (only if binding enabled) |

`AzureWebJobsStorage` is required by the Functions v4 host. For local work you can use the
[Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) emulator
(`UseDevelopmentStorage=true`).

When deployed, add the same settings under **Function App → Settings → Environment variables**.

> ⚠️ Never commit real connection strings or keys.

## Running Locally

**Prerequisites**

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local) (a version that supports .NET 10)
- An Azure Storage account, or the Azurite emulator
- *(Optional)* VS Code with the Azure Functions and C# Dev Kit extensions

**Run**

```bash
dotnet build
```

```bash
func start
```

Or in VS Code press **F5** (uses the tasks in `.vscode/tasks.json`).

The functions will be available at:

```
http://localhost:7071/api/ProcessOrderStorage
http://localhost:7071/api/ProcessOrderCosmos
```

## Testing the Functions

Using **curl**:

```bash
curl -X POST http://localhost:7071/api/ProcessOrderStorage -H "Content-Type: application/json" -d @ordersample.json
```

Using **Postman CLI** / Postman: send a `POST` request to the URL above with the contents of
`ordersample.json` as the raw JSON body.

Then check the `orders` container in your storage account (Azure Portal or Storage Explorer) – a new
`<guid>.txt` blob should contain the order.

## Deploying to Azure

1. Create a **Function App** – Runtime stack: **.NET**, Version: **10 (isolated worker)**.
   The Flex Consumption plan is recommended for new apps.
2. Add `StorageConnectionString` (and `CosmosDBConnection` if used) to the app settings.
3. Deploy using one of:
   - VS Code → Azure Functions extension → **Deploy to Function App** (runs the `publish (functions)` task first), or
   - Visual Studio → right-click project → **Publish**, or
   - Core Tools:

     ```bash
     func azure functionapp publish <your-function-app-name>
     ```

## Upgrade Notes

Changes made when moving from **.NET Core 3.1 / Functions v3 in-process** to
**.NET 10 / Functions v4 isolated worker**:

| Area              | Before (v3 in-process)                        | After (v4 isolated)                                     |
|-------------------|-----------------------------------------------|---------------------------------------------------------|
| Target framework  | `netcoreapp3.1`                               | `net10.0`, `OutputType=Exe`                             |
| Startup           | none                                          | `Program.cs` with `FunctionsApplication.CreateBuilder`  |
| Function attribute| `[FunctionName]`                              | `[Function]`                                            |
| Classes           | `static` classes, `ILogger` parameter         | instance classes, `ILogger<T>` via constructor injection |
| Blob binding      | `[Blob] CloudBlobContainer` (legacy SDK)      | `[BlobInput] BlobContainerClient` (`Azure.Storage.Blobs`) |
| Cosmos binding    | `[CosmosDB(collectionName: ...)]`             | `[CosmosDBOutput(containerName: ...)]` on a return type |
| Request body      | `.ReadToEndAsync().Result` (blocking)         | `await ... ReadToEndAsync()`                            |
| Worker runtime    | `dotnet`                                      | `dotnet-isolated`                                       |
| `host.json`       | Cosmos `protocol` / `leaseOptions`            | only `connectionMode` (removed options no longer exist) |
| `Order`           | no `id`                                       | `id` property added (Cosmos DB documents need one)       |

## Azure Concepts Covered

- Azure Functions (serverless compute) and the HTTP trigger
- Isolated worker model and dependency injection
- Input/output **bindings** (Blob Storage, Cosmos DB)
- Azure Blob Storage containers and blobs
- Azure Cosmos DB (database / container)
- App settings and connection-string management
- Logging with `ILogger` and Application Insights

---

*Built as part of the course "Microsoft Azure From Zero to Hero - The Complete Guide".*

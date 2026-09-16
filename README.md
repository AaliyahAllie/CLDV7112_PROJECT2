# ABC Retail - CLDV7112 Project 2: Azure Microservices & Cloud Integration

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Azure Functions](https://img.shields.io/badge/Azure_Functions-v4-0089D6?logo=microsoftazure)](https://azure.microsoft.com/services/functions/)
[![Azure Storage](https://img.shields.io/badge/Azure_Storage-Tables_%7C_Blobs_%7C_Queues_%7C_Files-0089D6?logo=microsoftazure)](https://azure.microsoft.com/services/storage/)
[![Build Status](https://img.shields.io/badge/Build-Passing-brightgreen)]()

---

## 📌 Project Overview

**ABC Retail** is a rapidly growing online retailer expanding its enterprise cloud platform. **CLDV7112 Project 2** extends the core e-commerce platform built in Project 1 by integrating **Azure Functions serverless microservices**, real-time event streaming via **Azure Event Hubs**, and enterprise message brokering via **Azure Service Bus**.

The solution is architected around high availability, asynchronous decoupled processing, cost efficiency, and cloud scalability.

---

## 🏛️ System Architecture

```
                             ┌────────────────────────────────────────────────────────┐
                             │               CLDV7112_PROJECT2.sln                    │
                             └───────────────────────────┬────────────────────────────┘
                                                         │
                        ┌────────────────────────────────┴───────────────────────────────┐
                        ▼                                                                ▼
         ┌──────────────────────────────┐                              ┌──────────────────────────────────┐
         │     CLDV7112_PROJECT2        │                              │    CLDV7112_PROJECT2.Functions   │
         │   (ASP.NET Core Web App)     │                              │   (Azure Functions Microservice) │
         └──────────────┬───────────────┘                              └────────────────┬─────────────────┘
                        │                                                               │
                        │  Invokes Azure Functions API Endpoints via HttpClient         │
                        └───────────────────────────────┬───────────────────────────────┘
                                                        │
                                                        ▼
         ┌──────────────────────────────────────────────────────────────────────────────┐
         │                               Azure Portal                                   │
         │  ┌──────────────────────┬────────────────────┬───────────────┬────────────┐  │
         │  │ Azure Tables         │ Azure Blob Storage │ Azure Queue   │ Azure Files│  │
         │  │ Customers, Products, │ product-images     │ order-trans-  │ contracts  │  │
         │  │ Orders               │                    │ actions       │            │  │
         │  └──────────────────────┴────────────────────┴───────────────┴────────────┘  │
         └──────────────────────────────────────────────────────────────────────────────┘
```

---

## 🚀 Azure Functions & Services Implemented

### Section A: Serverless Microservices (80 Marks)

1. **Azure Table Storage Function (`StoreTableInfo`)**
   - **Endpoint**: `/api/StoreTableInfo`
   - **Description**: Receives JSON payloads and upserts customer profiles, inventory items, and order entities directly into Azure Table Storage (`Customers`, `Products`, `Orders`).

2. **Azure Blob Storage Function (`UploadBlob`)**
   - **Endpoint**: `/api/UploadBlob`
   - **Description**: Receives image or document payloads and uploads them directly into Azure Blob Storage (`product-images` container) with public access policies.

3. **Azure Queue Storage Function (`WriteQueueTransaction` & `ReadQueueTransaction`)**
   - **Endpoints**: `/api/WriteQueueTransaction` / `/api/ReadQueueTransaction`
   - **Description**: Asynchronously publishes order transaction messages to Azure Queue Storage (`order-transactions`) and dequeues messages for fulfillment processing.

4. **Azure Files Function (`UploadAzureFile`)**
   - **Endpoint**: `/api/UploadAzureFile`
   - **Description**: Uploads contract documents, vendor agreements, and audit logs to Azure File Share (`contracts`).

---

### Section B: Customer Experience Services (20 Marks)

* **Azure Event Hubs**: Captures high-throughput real-time clickstream telemetry (product searches, cart updates, session analytics) without impacting checkout performance.
* **Azure Service Bus**: Enterprise message broker utilizing publish-subscribe (pub/sub) topics to orchestrate decoupled post-checkout operations (payment confirmation, email dispatch, inventory sync).

---

## 🛠️ Solution Structure

```
CLDV7112_PROJECT2/
├── CLDV7112_PROJECT2.sln
├── .gitignore                          # Excludes sensitive appsettings and build outputs
├── README.md                           # Project documentation
├── Azure_Portal_Setup_And_Submission_Guide.md # Azure setup & written discussion guide
├── CLDV7112_PROJECT2/                  # ASP.NET Core 9.0 Web Application
│   ├── Controllers/
│   │   ├── AdminController.cs          # Administrative inventory & order management
│   │   ├── CustomerController.cs       # Customer store, cart, & purchase history
│   │   ├── FunctionsController.cs      # Protected Admin Functions & Messaging Dashboard
│   │   └── HomeController.cs           # Authentication & public pages
│   ├── Services/
│   │   ├── TableStorageService.cs      # Azure Table Storage CRUD
│   │   ├── BlobStorageService.cs       # Azure Blob upload & retrieval
│   │   ├── QueueStorageService.cs      # Azure Queue message management
│   │   ├── FileShareService.cs         # Azure File Share log management
│   │   ├── FunctionsService.cs         # Azure Functions API Client
│   │   ├── EventHubService.cs          # Azure Event Hubs streaming service
│   │   └── ServiceBusService.cs        # Azure Service Bus messaging service
│   └── Views/
│       └── Functions/
│           └── Index.cshtml            # Admin Interactive Microservices Dashboard
└── CLDV7112_PROJECT2.Functions/        # Azure Functions .NET 9.0 Isolated Worker
    ├── StoreTableInfoFunction.cs       # Azure Tables Serverless Function
    ├── UploadBlobFunction.cs           # Azure Blob Storage Serverless Function
    ├── ProcessQueueTransactionFunction.cs # Azure Queue Storage Serverless Function
    ├── UploadAzureFileFunction.cs      # Azure Files Serverless Function
    └── EventHubAndBusDemoFunction.cs   # Event Hubs & Service Bus Serverless Function
```

---

## ⚙️ Prerequisites & Local Setup

### Prerequisites
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (with ASP.NET & Azure development workloads)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Azure Storage Account or [Azurite Local Emulator](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite)

### Local Configuration

1. **Clone the Repository**:
   ```bash
   git clone <YOUR_GITHUB_REPOSITORY_URL>
   cd CLDV7112_PROJECT2
   ```

2. **Configure `appsettings.json`**:
   Copy `appsettings.template.json` to `appsettings.json` inside the `CLDV7112_PROJECT2` directory and insert your Azure Storage Connection String:
   ```json
   {
     "AzureStorage": {
       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT_NAME;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net"
     },
     "AdminSettings": {
       "Username": "admin@abcretail.co.za",
       "Password": "AdminPassword123!"
     }
   }
   ```

3. **Configure `local.settings.json`**:
   Insert the same Azure Storage Connection String into `CLDV7112_PROJECT2.Functions/local.settings.json`:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "YOUR_AZURE_STORAGE_CONNECTION_STRING",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
       "AzureStorageConnectionString": "YOUR_AZURE_STORAGE_CONNECTION_STRING"
     }
   }
   ```

---

## 🏃 Running the Application

### Option A: From Visual Studio (Multiple Startup Projects)
1. Open `CLDV7112_PROJECT2.sln` in Visual Studio 2022.
2. Right-click the Solution node -> **Properties** -> **Startup Project**.
3. Select **Multiple startup projects** and set both `CLDV7112_PROJECT2` and `CLDV7112_PROJECT2.Functions` to **Start**.
4. Press **`F5`**.

### Option B: From Command Line
In separate terminal windows:
```powershell
# Start Azure Functions
dotnet run --project CLDV7112_PROJECT2.Functions/CLDV7112_PROJECT2.Functions.csproj

# Start Web Application
dotnet run --project CLDV7112_PROJECT2/CLDV7112_PROJECT2.csproj
```

---

## 🔒 Security & Role Authorization

- **Admin Dashboard & Functions Protection**: The `FunctionsController` and `/Functions` view are strictly authorized to **Admin** users.
- **Git Security**: Sensitive configuration files (`appsettings.json`, `local.settings.json`) are excluded via `.gitignore` to prevent credential exposure in version control.

---

## 📜 Academic Submission References

- **Module**: Cloud Development B (CLDV7112)
- **Assessment**: Project 2 (Paper and Marking Rubric)
- **Institution**: The Independent Institute of Education (Pty) Ltd

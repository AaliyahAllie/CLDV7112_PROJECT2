

# 🛒 ABC Retail - Cloud Microservices & Serverless Architecture (CLDV7112 Project 2)

![Azure](https://img.shields.io/badge/Azure-0089D6?style=for-the-badge&logo=microsoft-azure&logoColor=white)
![.NET 9](https://img.shields.io/badge/.NET%209.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Stripe](https://img.shields.io/badge/Stripe-008CDD?style=for-the-badge&logo=stripe&logoColor=white)
![Bootstrap 5](https://img.shields.io/badge/Bootstrap-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)

---

## 📌 Project Overview

**ABC Retail** is an enterprise-grade cloud e-commerce platform built for **CLDV7112 Project 2**. The solution expands upon the monolithic foundation of Project 1 by integrating **Azure Serverless Functions**, **Azure Event Hubs**, **Azure Service Bus**, and **Stripe Payment Gateway** into a decoupled, highly scalable, and cost-effective cloud microservices architecture.

---

## 🚀 Key Upgrades from Project 1 to Project 2

| Feature / Architectural Layer | Project 1 (Monolith Foundation) | Project 2 (Cloud Microservices Architecture) |
| :--- | :--- | :--- |
| **Backend Storage Operations** | Direct C# SDK calls inside MVC Web App controllers. | Decoupled via **4 Azure Serverless Functions** (`StoreTableInfo`, `UploadBlob`, `WriteQueueTransaction`, `UploadAzureFile`). |
| **Transaction Processing** | Synchronous database writes during checkout. | Asynchronous messaging via **Azure Queue Storage** & **ProcessQueueTransaction Function**. |
| **Payment Integration** | Demo static payment screen. | Real-time **Stripe Payment Gateway API** with client-side checkout & webhook verification. |
| **System Logging** | Local text file logging. | Enterprise logging to **Azure File Share** with real-time log reading capabilities. |
| **Real-time Telemetry & Messaging** | None. | **Azure Event Hubs** (clickstream telemetry) & **Azure Service Bus** (order fulfillment notifications). |
| **Fault Tolerance & Reliability** | Fails if storage is unreachable. | **Resilient Fallback Design**: If an Azure Function endpoint times out, the web app gracefully falls back to direct Azure SDK storage execution. |

---

## 🏗️ Solution Architecture

```
                       +-----------------------------------+
                       |    ASP.NET Core Web App (MVC)     |
                       |    - Customer Storefront          |
                       |    - Stripe Payment Gateway       |
                       |    - Admin Dashboard              |
                       +-----------------+-----------------+
                                         |
            +----------------------------+----------------------------+
            |                            |                            |
            v                            v                            v
  +------------------+         +-------------------+        +--------------------+
  | Azure Event Hubs |         | Azure Service Bus |        |   Stripe API       |
  | (Telemetry Logs) |         | (Fulfillment Topic|        | (Payment Processing|
  +------------------+         +-------------------+        +--------------------+
                                         |
                                         v
                     +---------------------------------------+
                     |    Azure Functions (.NET 9 Isolated)   |
                     |  - StoreTableInfo (Tables)            |
                     |  - UploadBlob (Blob Storage)          |
                     |  - ProcessQueueTransaction (Queues)   |
                     |  - UploadAzureFile (File Shares)      |
                     +-------------------+-------------------+
                                         |
            +----------------------------+----------------------------+
            |                            |                            |
            v                            v                            v
  +-------------------+        +-------------------+        +--------------------+
  | Azure Table Store |        | Azure Blob Store  |        | Azure File Share   |
  | (Customers/Orders)|        | (Product Images)  |        | (System Audit Logs)|
  +-------------------+        +-------------------+        +--------------------+
```

---

## ⚡ Azure Functions Specification

The platform utilizes **4 core Azure Functions** hosted in `func-cldv7112-project2`:

1. **`StoreTableInfoFunction` (`HTTP POST /api/StoreTableInfo`)**:
   - **Purpose:** Receives JSON payloads for customer registrations or order entities and persists them in **Azure Table Storage**.
2. **`UploadBlobFunction` (`HTTP POST /api/UploadBlob`)**:
   - **Purpose:** Decodes base64 image strings and uploads product images directly to **Azure Blob Storage** (`product-images` container).
3. **`ProcessQueueTransactionFunction` (`HTTP POST /api/WriteQueueTransaction` & Queue Trigger)**:
   - **Purpose:** Enqueues transaction payloads to **Azure Queue Storage** (`order-transactions`) and dequeues messages asynchronously for background order processing.
4. **`UploadAzureFileFunction` (`HTTP POST /api/UploadAzureFile`)**:
   - **Purpose:** Generates invoice contracts and system logs stored in **Azure File Share** (`system-logs`).

---

## 📂 Project Structure

```
CLDV7112_PROJECT2/
├── CLDV7112_PROJECT2.sln                  # Main Visual Studio Solution file
│
├── CLDV7112_PROJECT2/                     # ASP.NET Core Web Application (MVC)
│   ├── Controllers/
│   │   ├── AdminController.cs             # Product & order administration
│   │   ├── CartController.cs              # Shopping cart & Stripe checkout
│   │   ├── CustomerController.cs          # User registration, login, & order history
│   │   ├── FunctionsController.cs         # Azure Functions monitoring dashboard
│   │   └── HomeController.cs              # Product catalog storefront
│   ├── Services/
│   │   ├── BlobStorageService.cs          # Azure Blob Storage helper
│   │   ├── TableStorageService.cs         # Azure Table Storage helper
│   │   ├── QueueStorageService.cs         # Azure Queue Storage helper
│   │   ├── FileShareService.cs            # Azure File Share logging helper
│   │   ├── FunctionsService.cs            # Azure Functions REST caller with fallback
│   │   ├── EventHubService.cs             # Azure Event Hubs telemetry producer
│   │   ├── ServiceBusService.cs           # Azure Service Bus publisher
│   │   └── StripePaymentService.cs        # Stripe payment integration
│   ├── Models/                            # Customer, Order, CartItem, Product data models
│   └── Views/                             # Razor UI templates with glassmorphism design
│
└── CLDV7112_PROJECT2.Functions/           # Azure Functions Serverless Project (.NET 9)
    ├── StoreTableInfoFunction.cs          # Azure Table Storage Function
    ├── UploadBlobFunction.cs             # Azure Blob Storage Function
    ├── ProcessQueueTransactionFunction.cs # Azure Queue Storage Function
    ├── UploadAzureFileFunction.cs        # Azure File Share Function
    ├── EventHubAndBusDemoFunction.cs     # Telemetry & Messaging Functions
    └── Program.cs                         # Function worker configuration
```

---

## 🛠️ Local Setup Instructions

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- Visual Studio 2022 (v17.12+) or VS Code
- Azure Storage Account Connection String

---

### Step 1: Clone the Repository
```bash
git clone https://github.com/YourUsername/CLDV7112_PROJECT2.git
cd CLDV7112_PROJECT2
```

### Step 2: Configure Web Application (`appsettings.json`)
Open `CLDV7112_PROJECT2/appsettings.json` and ensure your Azure Storage Connection String is populated:

```json
{
  "AzureStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=your_account;AccountKey=your_key;EndpointSuffix=core.windows.net"
  },
  "AzureFunctions": {
    "BaseUrl": "https://func-cldv7112-project2-dcahhwfxaqh0ejgj.southafricanorth-01.azurewebsites.net/api"
  }
}
```

### Step 3: Configure Azure Functions (`local.settings.json`)
Create `CLDV7112_PROJECT2.Functions/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=your_account;AccountKey=your_key;EndpointSuffix=core.windows.net",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

### Step 4: Run the Solution
1. Start Azure Functions:
   ```bash
   cd CLDV7112_PROJECT2.Functions
   func start
   ```
2. Start Web Application:
   ```bash
   cd ../CLDV7112_PROJECT2
   dotnet run
   ```
3. Open your browser and navigate to `https://localhost:7198`.

---

## 🌐 Live Azure Cloud Deployment Links

- **Web Application URL:** `https://st10212542-cldv7112-2-csbufnbafqffccdb.southafricanorth-01.azurewebsites.net//`
- **Azure Functions Base Endpoint:** `https://func-cldv7112-project2-dcahhwfxaqh0ejgj.southafricanorth-01.azurewebsites.net/`


---

## 🔒 Security & Quality Assurance

- **Input Sanitization & Validation:** All registration, checkout, and product input forms enforce strict data validation.
- **Role-Based Authentication:** Session-based authorization separates Customer capabilities from Admin capabilities.
- **Error Handling & Resiliency:** All Azure Function client calls feature `try/catch` fallbacks to ensure the web application remains 100% operational even if remote function endpoints experience network latency.
- **Code Cleanliness:** All C# source code files and Razor view templates contain clear, friendly, human-readable comments explaining system functionality.

---

## ✒️ Author & Academic Details

- **Student Name:** Aaliyah
- **Module:** CLDV7112 - Cloud Development 2
- **Institution:** Rosebank College / IIE
- **Project:** Project 2 Final Submission

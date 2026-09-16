# CLDV7112 Project 2: Azure Portal Setup, Function Integration & Academic Submission Guide

**Module Code:** CLDV7112 / Cloud Development B  
**Assessment:** Project 2 (Integrating Azure Services into a Web Application)  
**Solution Location:** `C:\Users\Aaliyah\source\repos\CLDV7112_PROJECT2`  
**Web Application:** `CLDV7112_PROJECT2`  
**Azure Functions App:** `CLDV7112_PROJECT2.Functions`  
**Live Deployed Azure Function App URL:** `https://func-cldv7112-project2-dcahhwfxaqh0ejgj.southafricanorth-01.azurewebsites.net/api`  

---

## Executive Summary & Solution Architecture

This document provides complete step-by-step instructions for setting up Azure resources in the Azure Portal, deploying your Visual Studio solution (`CLDV7112_PROJECT2.sln`), taking the mandatory screenshots required by the rubric, and completing the written discussion for Section B (Azure Event Hubs & Azure Service Bus).

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
                        │  Calls Azure Functions via HttpClient & API Endpoints         │
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

## Part 1: Step-by-Step Azure Portal Setup Guide

Follow these steps in the **Azure Portal** ([https://portal.azure.com](https://portal.azure.com)):

### Step 1: Create a Resource Group
1. Sign in to the Azure Portal.
2. In the search bar at the top, type **Resource groups** and select it.
3. Click **+ Create**.
4. Configure the following details:
   - **Subscription**: Select your active Azure subscription (e.g. Azure Student / Pass).
   - **Resource group**: `RSG-RCGPON-ST10212542-SANORTH`
   - **Region**: `South Africa North`.
5. Click **Review + create**, then click **Create**.

---

### Step 2: Create Azure Storage Account & Services
1. Search for **Storage accounts** in the Azure Portal and click **+ Create**.
2. Fill in:
   - **Resource Group**: `RSG-RCGPON-ST10212542-SANORTH`
   - **Storage account name**: `stcldv7112project2`
   - **Region**: `South Africa North`.
   - **Performance**: Standard.
   - **Redundancy**: Locally-redundant storage (LRS).
3. Click **Review + create** -> **Create**.
4. Once deployed, click **Go to resource**.
5. Under **Security + networking**, click **Access keys**.
6. Copy **Key 1 -> Connection string**. Save this connection string; you will paste it into your App Service and Function App configuration.

#### Creating Storage Containers/Tables/Queues/Shares in Azure Portal:
- **Azure Tables**:
  1. In your Storage Account menu, select **Tables** under Data storage.
  2. Click **+ Table** and create three tables:
     - `Customers`
     - `Products`
     - `Orders`
- **Blob Containers**:
  1. Select **Containers** under Data storage.
  2. Click **+ Container**, set name to `product-images`, and set **Anonymous access level** to `Blob (anonymous read access for blobs only)`. Click **Create**.
- **Queue Storage**:
  1. Select **Queues** under Data storage.
  2. Click **+ Queue**, name it `order-transactions`, and click **OK**.
- **File Shares (Azure Files)**:
  1. Select **File shares** under Data storage.
  2. Click **+ File share**, name it `contracts`, select tier `Transaction optimized`, and click **Create**.

---

### Step 3: Deployed Azure Function App Details
- **Function App Name**: `func-cldv7112-project2`
- **Live Function App Base URL**: `https://func-cldv7112-project2-dcahhwfxaqh0ejgj.southafricanorth-01.azurewebsites.net/api`
- **Region**: `South Africa North`
- **Runtime**: `.NET 9 (Isolated Worker)`

---

### Step 4: Create Azure App Service (Web App) in Azure Portal
1. Search for **App Services** and click **+ Create -> Web App**.
2. Configure settings:
   - **Resource Group**: `RSG-RCGPON-ST10212542-SANORTH`
   - **Name**: `student10212542-cldv7112` (this gives URL `http://student10212542-cldv7112.azurewebsites.net`)
   - **Publish**: `Code`
   - **Runtime stack**: `.NET 9 (STS)`
   - **Operating System**: `Windows`
   - **Pricing Plan**: `Free F1` or `Basic B1`
3. Click **Review + create** -> **Create**.
4. Go to the newly created Web App resource -> **Settings -> Environment variables**.
5. Add the following app settings:
   - `AzureStorage:ConnectionString` = `DefaultEndpointsProtocol=https;AccountName=stcldv7112project2;AccountKey=/lKAXMa6xl+gYtg7iYXo8ZUVFl8JJTZNHnz1ooOXSbIWDxYppFaOLBJRw90bJkENCIGA5E4B0Ob8+AStb68m4w==;EndpointSuffix=core.windows.net`
   - `AzureFunctions:BaseUrl` = `https://func-cldv7112-project2-dcahhwfxaqh0ejgj.southafricanorth-01.azurewebsites.net/api`
6. Click **Apply** / **Save**.

---

### Step 5: Publishing Code from Visual Studio to Azure
1. Open **Visual Studio**.
2. Open `CLDV7112_PROJECT2.sln`.
3. **Publish Azure Functions App**:
   - Right-click project `CLDV7112_PROJECT2.Functions` -> click **Publish...**
   - Select **Target**: `Azure` -> **Specific Target**: `Azure Function App (Windows)`
   - Select your subscription, resource group `RSG-RCGPON-ST10212542-SANORTH`, and Function App `func-cldv7112-project2-dcahhwfxaqh0ejgj`.
   - Click **Finish**, then click **Publish**.
4. **Publish Web Application**:
   - Right-click project `CLDV7112_PROJECT2` -> click **Publish...**
   - Select **Target**: `Azure` -> **Specific Target**: `Azure App Service (Windows)`
   - Select your Web App `student10212542-cldv7112`.
   - Click **Finish**, then click **Publish**.

---

## Part 2: Section B Written Discussion (20 Marks)

> **Instructions for Submission**: Copy the text below directly into Section B of your final MS Word document (`StudentNumber_CLDV7112_Project2.docx`).

---

### B. Using Services for Improving Customer Experience

To support ABC Retail's transition from legacy on-premises architecture to a high-volume cloud e-commerce enterprise, integrating real-time telemetry streaming and reliable asynchronous messaging services is vital. Below is a comprehensive technical evaluation of **Azure Event Hubs** and **Azure Event Bus (Azure Service Bus)**.

---

### 1. Azure Event Hubs

#### a) Description of Service
**Azure Event Hubs** is a fully managed, real-time data ingestion and event streaming service capable of receiving and processing millions of events per second. Positioned as a big data streaming platform and event ingestion service, Event Hubs acts as the "front door" for event pipelines, capturing data sent by web applications, IoT devices, microservices, and mobile clients with low latency and high throughput.

#### b) Mechanism
* **Partitioned Consumer Model**: Azure Event Hubs uses a partitioned consumer model where streaming data is organized into parallel partitions. Each partition stores data sequentially as a stream of events.
* **Append-Only Time-Series Log**: Incoming event streams are stored in an append-only log format. Events remain in the stream for a configurable retention period (from 1 to 7 days) regardless of whether they have been read.
* **Consumer Groups**: Multiple consuming applications (e.g. real-time analytics engines, Azure Stream Analytics, Azure Functions, data lakes) can attach separate consumer groups to read the exact same event stream concurrently at their own pace without affecting each other.
* **Protocol & SDK Support**: Supports AMQP 1.0, HTTPS, and Apache Kafka APIs, allowing seamless integration with existing telemetry and analytics tools.

#### c) How it Adds Value to End Users & ABC Retail
* **Real-Time Clickstream & Personalization**: ABC Retail can stream real-time customer activity (product views, search queries, cart additions, session durations) to Azure Stream Analytics. This enables immediate personalized product recommendations while the customer is actively browsing the store.
* **Zero Performance Degradation During Peak Sales**: During high-concurrency events like Christmas and Black Friday, millions of clickstream events can be ingested into Event Hubs without degrading web app response times or locking primary database tables.
* **Fraud Detection & Operational Monitoring**: Telemetry streams can immediately flag abnormal purchasing behavior or backend operational bottlenecks before customers experience failed checkouts.

---

### 2. Azure Event Bus (Azure Service Bus)

#### a) Description of Service
**Azure Service Bus** (often referred to as Azure Event Bus or Enterprise Service Bus) is a fully managed enterprise integration message broker featuring message queues and publish-subscribe (pub/sub) topics. Designed for high-value transactional messaging, Service Bus decouples applications and microservices, ensuring reliable message processing, state consistency, and guaranteed message delivery across distributed cloud systems.

#### b) Mechanism
* **Queues (Point-to-Point)**: Messages are sent to a queue and stored until a single receiver retrieves and processes them (first-in, first-out guaranteed processing).
* **Topics & Subscriptions (Pub/Sub)**: Publishers post messages to a topic. Multiple independent subscriptions can filter and receive a copy of each message using SQL-like filter rules.
* **At-Least-Once Delivery & Transactions**: Supports Peek-Lock mode, duplicate detection, dead-letter queues, and atomic transactional messaging to guarantee that critical messages are never lost or double-processed.
* **Session Ordering & Scheduled Delivery**: Ensures strict sequence order for financial transactions and allows deferred or scheduled message execution.

#### c) How it Adds Value to End Users & ABC Retail
* **Reliable Order Processing & Notifications**: When a customer completes checkout, an order event is published to a Service Bus topic. Subscriptions instantly trigger parallel independent workflows: payment settlement, inventory reduction, warehouse packing, and automated email/SMS tracking notifications to the customer.
* **Seamless Customer Communication**: Even if the email gateway or warehouse system experiences temporary maintenance, Service Bus safely buffers messages and retries automatically, ensuring customers never miss order confirmation updates.
* **Decoupled Architecture & Scalability**: ABC Retail can add new microservices (e.g. loyalty reward point calculation, accounting sync) simply by adding new topic subscriptions without modifying existing checkout code or impacting customer checkout speed.

---

## Part 3: Mandatory Screenshots Checklist for Submission Document

When creating your final MS Word document (`StudentNumber_CLDV7112_Project2.docx`), capture and insert clear screenshots for the following:

| # | Required Screenshot | Location in Azure Portal / Code | Rubric Mark |
|---|---------------------|---------------------------------|-------------|
| 1 | **Azure Table Function** | Azure Function App -> Functions -> `StoreTableInfo` screen AND Visual Studio `StoreTableInfoFunction.cs` code. | 20 Marks |
| 2 | **Azure Blob Storage Function** | Azure Function App -> Functions -> `UploadBlob` screen AND Visual Studio `UploadBlobFunction.cs` code. | 20 Marks |
| 3 | **Azure Queue Function & Message** | Azure Function App -> Functions -> `ProcessQueueTransaction` screen, Visual Studio code, AND Storage Account -> Queues -> `order-transactions` showing queued message. | 20 Marks |
| 4 | **Azure Files Function & File** | Azure Function App -> Functions -> `UploadAzureFile` screen, Visual Studio code, AND Storage Account -> File shares -> `contracts` showing uploaded file. | 20 Marks |
| 5 | **Section B Discussion** | Typed written responses under the 3 required headings (Description, Mechanism, Value to end users). | 20 Marks |
| 6 | **Deployed Web Application URL** | Web browser showing live web app at `http://student_number.azurewebsites.net` and Web App resource overview in Azure Portal. | Mandatory |

---

## Conclusion & Submission Reminders

1. **Document Naming Convention**:  
   Name your Word document strictly as: `StudentNumber_ModuleCode_Project2.docx` (e.g., `10212542_CLDV7112_Project2.docx`).
2. **GitHub Repository**:  
   Commit and push all project files from `C:\Users\Aaliyah\source\repos\CLDV7112_PROJECT2` to your public/academic GitHub repository.
3. **Include Links in Document**:  
   - Student Number & Module Code
   - URL of deployed Web Application (`http://student_number.azurewebsites.net`)
   - Link to your GitHub repository containing the Visual Studio solution.

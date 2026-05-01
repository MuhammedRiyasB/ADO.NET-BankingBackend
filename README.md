# BankingBackend API

A robust, secure, and highly scalable Core Banking Backend built with **.NET 9**. This project strictly adheres to **Clean Architecture** and **SOLID principles**, completely decoupling the core business logic from the infrastructure and presentation layers.

## 🏗 Architecture & Tech Stack

*   **Framework**: ASP.NET Core Web API (.NET 9)
*   **Architecture**: Clean Architecture (Domain, Application, Infrastructure, API)
*   **Database**: SQL Server
*   **Data Access**: Raw ADO.NET (for maximum performance and control)
*   **Transaction Management**: Custom Ambient Unit of Work (`SqlUnitOfWork`)
*   **Authentication**: JWT (JSON Web Tokens) with Role-Based Access Control
*   **Error Handling**: Result Pattern & Global Exception Middleware

## ✨ Key Features

*   **Secure Authentication**: JWT-based login with Admin/Teller/Auditor roles.
*   **Customer & Account Management**: Full lifecycle management for users and banking accounts (Savings, Current, Fixed Deposit).
*   **Transaction Processing**: Secure deposits, withdrawals, and account-to-account transfers.
*   **Ledger System**: Immutable double-entry ledger tracking for all financial movements.
*   **Business Rules Validation**: Enforced daily debit limits, currency matching, and balance checks within the Domain layer.
*   **API Rate Limiting**: Built-in protection against brute-force and DDoS attacks.

## 🚀 Getting Started

### Prerequisites
*   .NET 9 SDK
*   SQL Server (LocalDB or full instance)

### Setup Configuration
Set the following environment variables on your system before running the API:
*   `BANKING_DB_CONNECTION`: Your SQL Server connection string.
*   `BANKING_JWT_SIGNING_KEY`: A secure 32+ character string.
*   `BANKING_ADMIN_PASSWORD`: The password for the seeded `admin` account.

The Database Initializer will automatically create the necessary schemas and seed the initial administrator account upon the first run.

---
title: "Project Pica"
shortDescription: "Co-founder and development lead of an AI-driven platform that makes the permit and compliance status of Belgian real estate clear at a glance."
startDate: 2024-10-01
endDate: 2026-10-23
featured: true
image: images/logos/project-pica.svg
skills:
  - .NET
  - C#
  - Clean Architecture
  - Domain-Driven Design
  - MediatR
  - Dapr
  - Azure Service Bus
  - SignalR
  - Microsoft Entra ID
  - Angular
  - Playwright
  - Terraform
  - Azure Container Apps
  - Azure SQL Database
  - GitHub Actions
  - .NET Aspire
  - Python
  - Azure AI
  - Anthropic Claude
  - Google Gemini
  - Figma
---
Co-founded **Project Pica** with three others, to streamline real estate document verification for the Belgian market. Part-time, remote, since October 2024.

**Platform.** Designed and built the full-stack, cloud-native platform from the ground up. The .NET back end follows Clean Architecture and Domain-Driven Design, with aggregate roots, domain events and MediatR CQRS throughout. Dapr pub/sub over Azure Service Bus handles async orchestration between services, and SignalR delivers real-time notifications in the app.

**Authentication.** Multi-tenant sign-in with Microsoft Entra External ID (CIAM) for customers and Entra ID for internal staff, enforced through policy-based JWT Bearer schemes on a Minimal API layer.

**Shared packages.** A suite of standalone NuGet packages, each with its own CI pipeline, including a messaging wire contract used by both .NET and Python services, and a reusable composite GitHub Actions workflow for tag-based semantic versioning across all repositories.

**Front end and testing.** An Angular customer portal and admin interface with MSAL, Angular Signals and a custom design system. A Playwright end-to-end suite with Page Object Models, multi-tenant fixtures and Microsoft Graph integration that mints temporary access passes for MFA-protected admin accounts in CI.

**Infrastructure.** All cloud resources in Terraform: Azure Container Apps, Azure SQL Database, Key Vault, Blob Storage, Application Insights and Azure Container Registry. GitHub Actions deploys to test on every push to main, with a manual promotion to production. .NET Aspire runs the full local environment (API, background runner, Redis, SQL Server, Azurite and the Dapr sidecars) with one command.

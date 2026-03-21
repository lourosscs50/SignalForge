# SignalForge

SignalForge is a platform-focused signal detection system designed to ingest events, evaluate them against configurable rules, and generate actionable alerts in real time.

Built with Clean Architecture and a modular monolith approach, SignalForge emphasizes clear boundaries, testability, and long-term scalability.

---

## 🚀 Overview

SignalForge provides a foundation for building real-time detection and monitoring systems. It ingests signals, applies rule-based evaluation, and produces alerts when conditions are met.

This project is part of a broader ecosystem focused on **platform engineering**, not just application development.

---

## 🧱 Architecture

SignalForge follows **Clean Architecture** with strict separation of concerns:
src/
├── SignalForge.Api # HTTP layer (endpoints, middleware)
├── SignalForge.Application # Use cases and orchestration
├── SignalForge.Domain # Core business entities
├── SignalForge.Contracts # DTOs and request/response models
└── SignalForge.Infrastructure # Implementations (auth, persistence, services)
tests/
├── SignalForge.Api.Tests
├── SignalForge.Application.Tests
├── SignalForge.Domain.Tests
└── SignalForge.Architecture.Tests

### Key Principles

- **No domain logic in API layer**
- **Application layer orchestrates behavior**
- **Domain layer remains framework-agnostic**
- **Infrastructure implements external concerns**
- **Strict boundaries between layers**

---

## 🔐 Authentication

SignalForge uses **JWT (JSON Web Token)** authentication:

- `/auth/register` → creates user and returns token
- `/auth/login` → authenticates user and returns token
- `/me` → protected endpoint for current user
- `/signals` → protected signal access

Security is enforced via:
- Bearer token validation
- Consistent signing and validation configuration
- Integration tests covering auth flows

---

## 📡 Core Capabilities

### 1. Signal Ingestion
- Accepts incoming signals via API
- Stores signals for processing
- Designed for future streaming/event-based extensions

### 2. Rule-Based Evaluation (Phase 2)
- Evaluates signals against configurable rules
- Extensible evaluator strategy (planned)
- Supports future rule types:
  - Equality
  - Threshold
  - Pattern matching
  - Temporal rules

### 3. Alert Generation
- Alerts are created when rules match signals
- Alerts link back to both:
  - Signal
  - Rule

---

## 🧪 Testing

SignalForge is built with **test-driven discipline**.

### Test Types

- **API Tests**
  - Auth flows
  - Protected endpoints
- **Application Tests**
  - Use case validation
- **Domain Tests**
  - Core logic integrity
- **Architecture Tests**
  - Enforces layer boundaries

### Run Tests

```bash
dotnet test tests/SignalForge.Api.Tests/SignalForge.Api.Tests.csproj
dotnet test tests/SignalForge.Application.Tests/SignalForge.Application.Tests.csproj
⚙️ Getting Started
Prerequisites
.NET 10 SDK
Docker (optional, for future database integration)
Run API
dotnet run --project src/SignalForge.Api
Run Tests
dotnet test
📁 Project Structure Highlights
Domain
Signal
Rule
Alert
User
Application
IngestSignal
ListSignals
CreateRule
RegisterUser
LoginUser
GetCurrentUser
Infrastructure
JWT authentication
Password hashing
In-memory persistence (for testing)
Dependency injection wiring
🧠 Design Philosophy
SignalForge is built with a platform mindset:
Prioritize extensibility over shortcuts
Build foundations before features
Enforce architectural boundaries early
Validate behavior through tests, not assumptions
🔮 Roadmap
Phase 1 (Completed)
Clean Architecture foundation
JWT authentication
Signal ingestion
Basic alert flow
Integration testing
Phase 2 (In Progress)
Rule evaluation engine
Strategy-based rule evaluators
Alert enrichment
Expanded test coverage
Future
PostgreSQL persistence
Event streaming integration
Multi-tenant support
Real-time processing pipelines
📄 License
Apache License 2.0
👤 Author
Lou Carron
GitHub: https://github.com/lourosscs50
💬 Final Note
SignalForge is not just an API — it is a foundation for building real-time detection systems.

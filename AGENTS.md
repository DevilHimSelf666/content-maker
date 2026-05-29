AGENTS.md

Project

Engineering Digest

An internal platform that monitors technical YouTube channels, extracts transcripts, evaluates relevance, generates Persian engineering articles, and publishes approved content to Telegram.

Primary Goal

Convert software engineering videos into practical Persian technical articles for developers.

The output must be educational and actionable, not merely a summary.

Target Audience

* .NET Developers
* ASP.NET Core Developers
* Blazor Developers
* Backend Engineers
* Software Architects
* DevOps Engineers

Architecture

Use:

* .NET 10
* Clean Architecture
* Vertical Slice Architecture
* Wolverine
* SQL Server
* EF Core
* Blazor Server
* YARP (if API gateway becomes necessary)
* Docker Compose

Do not use:

* MassTransit
* MediatR
* RabbitMQ (unless explicitly requested later)

Messaging

Use Wolverine.

Commands:

* DiscoverVideos
* ExtractTranscript
* ClassifyVideo
* GenerateArticle
* ApproveArticle
* PublishArticle

All handlers must be idempotent.

Persistence

Use SQL Server.

Requirements:

* EF Core
* Migrations
* Unique index on YouTubeVideoId
* Soft delete where appropriate
* Audit fields

LLM Requirements

Support:

* OpenAI
* Groq
* OpenAI-compatible local endpoints

Use abstraction:

* ILlmClient

Never hardcode models.

Transcript Service

Use a Python FastAPI service.

Responsibilities:

* Retrieve transcript
* Normalize transcript
* Return plain text

Telegram Publishing

Publishing must be manual initially.

Workflow:

Discovered
→ TranscriptReady
→ Classified
→ ArticleDrafted
→ PendingReview
→ Approved
→ Published

Never auto-publish in MVP.

Quality Requirements

Generated content must:

* Be written in Persian
* Keep technical terms in English
* Be educational
* Be practical
* Avoid hype
* Avoid clickbait
* Avoid hallucinations

Coding Standards

* SOLID
* Dependency Injection
* Typed Options
* Async/Await
* Cancellation Tokens
* Structured Logging
* Unit Tests for business logic
* No static service locators

Deliverables

Every feature must include:

* Implementation
* Tests
* Documentation
* Migration (if needed)

Definition of Done

A feature is complete only when:

* Builds successfully
* Tests pass
* Documentation updated
* No obvious architectural violations
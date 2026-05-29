# Engineering Digest

Engineering Digest is an internal platform that turns software engineering YouTube videos into practical Persian technical articles for professional developers.

Phase 1 MVP implements this manual-review workflow:

```text
Discovered → TranscriptReady → Classified → ArticleDrafted → PendingReview → Approved → Published
```

Articles are **never auto-published**. Generated content is saved as `PendingReview`, then an admin must approve it before Telegram publishing is available.

## Architecture

- **.NET 10** solution using Clean Architecture boundaries and vertical workflow handlers.
- **Wolverine** for command/message flow. MassTransit, MediatR, and RabbitMQ are intentionally not used.
- **SQL Server + EF Core** for persistence and migrations.
- **Blazor Server** admin UI for listing videos/articles and approving/publishing articles.
- **Python FastAPI** transcript service for retrieving and normalizing YouTube transcripts.
- **OpenAI-compatible LLM abstraction** through `ILlmClient`, supporting OpenAI, Groq, and local compatible endpoints.
- **Telegram publishing** through a manual `PublishArticle` command after approval.

## Solution Layout

```text
src/EngineeringDigest.Domain          Domain entities, workflow/status enums, audit base type
src/EngineeringDigest.Application     Commands and service abstractions
src/EngineeringDigest.Infrastructure  EF Core, Wolverine handlers, RSS, transcript, LLM, Telegram integrations
src/EngineeringDigest.Worker          Scheduled discovery/processing worker
src/EngineeringDigest.Admin           Blazor Server admin UI
tests/EngineeringDigest.Tests         Business workflow tests
transcript-service                    Python FastAPI transcript service
```

## Prerequisites

- .NET SDK 10.0+
- Docker and Docker Compose
- Python 3.14+ if running the transcript service outside Docker
- An OpenAI-compatible API key/model for classification and article generation
- Telegram bot token and chat id for manual publishing

## Configuration

Copy the example environment file and fill in secrets:

```bash
cp .env.example .env
```

Important variables:

| Variable | Description |
| --- | --- |
| `SQL_PASSWORD` | SQL Server `sa` password used by Docker Compose |
| `LLM_BASE_URL` | OpenAI-compatible base URL, e.g. OpenAI, Groq, or a local endpoint |
| `LLM_API_KEY` | API key for the configured LLM endpoint |
| `LLM_MODEL` | Model name. This is configuration-only and is never hardcoded |
| `TELEGRAM_BOT_TOKEN` | Telegram bot token |
| `TELEGRAM_CHAT_ID` | Telegram chat/channel id |

## Run with Docker Compose

```bash
docker compose up --build
```

Services:

- Admin UI: <http://localhost:8080>
- Transcript API: <http://localhost:8081/health>
- SQL Server: `localhost,1433`

The worker and admin both apply EF Core migrations on startup and seed configured channels. The default worker configuration seeds the official `dotNET` YouTube channel.

## Run Locally Without Docker

Start SQL Server and the transcript service, then run the .NET apps:

```bash
cd transcript-service
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn main:app --host 0.0.0.0 --port 8081
```

In another terminal:

```bash
dotnet restore EngineeringDigest.sln
dotnet build EngineeringDigest.sln
dotnet test EngineeringDigest.sln
dotnet run --project src/EngineeringDigest.Worker
dotnet run --project src/EngineeringDigest.Admin
```

## Database

EF Core migrations live in `src/EngineeringDigest.Infrastructure/Persistence/Migrations`.

The schema includes:

- `Channels` with unique `YouTubeChannelId` for seed idempotency.
- `Videos` with unique `YouTubeVideoId` for duplicate detection and idempotent processing.
- `Articles` with a unique `VideoId`, audit fields, soft-delete fields, and manual workflow status.

## Phase 1 Flow

1. `DiscoverVideos` reads enabled channel RSS feeds.
2. New videos are inserted only when `YouTubeVideoId` does not already exist.
3. `ExtractTranscript` calls the FastAPI transcript service.
4. `ClassifyVideo` calls `ILlmClient` and marks irrelevant videos as `NotRelevant`.
5. `GenerateArticle` calls `ILlmClient` and saves a Persian article as `PendingReview`.
6. Admin reviews pending articles in Blazor Server.
7. `ApproveArticle` marks an article approved.
8. `PublishArticle` publishes an approved article to Telegram manually and records the Telegram message id.

## Content Quality Rules

Generated articles must be Persian, keep technical terms in English, avoid hype/clickbait, and follow this structure:

1. Title
2. Introduction
3. Problem Statement
4. Technical Explanation
5. Enterprise Usage
6. Common Mistakes
7. Team Recommendations
8. Conclusion

## Useful Commands

```bash
dotnet build EngineeringDigest.sln
dotnet test EngineeringDigest.sln
docker compose up --build
```


## Phase 3 Knowledge Platform

Engineering Digest now includes a Knowledge Platform that turns approved Persian engineering articles into a searchable internal learning base.

### Capabilities

- Approved articles are promoted to `KnowledgeArticle` records.
- PostgreSQL + pgvector stores title, body, and key-takeaway embeddings.
- `IEmbeddingProvider` supports OpenAI, OpenAI-compatible, and local embedding endpoints through configuration.
- `/knowledge` provides full-text and semantic search with category filters.
- `/knowledge/ask` provides RAG answers with cited source articles and confidence scores.
- Related articles, learning paths, weekly digests, analytics dashboards, quality scores, and Markdown/HTML/PDF export endpoints are scaffolded for internal learning workflows.

### Knowledge API

- `GET /knowledge/search?q=...&semantic=true`
- `GET /knowledge/article/{id}`
- `GET /knowledge/related/{id}`
- `POST /knowledge/ask`
- `GET /knowledge/article/{id}/export/{format}` where format is `markdown`, `html`, or `pdf`

### Configuration

Set `ConnectionStrings:Knowledge` to PostgreSQL with pgvector enabled and configure the `Embedding` section. Models are never hardcoded.

See also:

- `docs/RAG.md`
- `docs/KNOWLEDGE_PLATFORM.md`
- `docs/SEARCH_ARCHITECTURE.md`

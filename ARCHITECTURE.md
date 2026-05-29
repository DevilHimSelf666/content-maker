# Engineering Digest Architecture

Engineering Digest uses Clean Architecture with vertical slices for the video processing workflow. The domain model contains videos, articles, prompt templates, article versions, processing jobs, and audit records. Infrastructure owns SQL Server persistence, Wolverine messaging, external HTTP integrations, observability, and publishing adapters.

## Workflow

`Discovered → TranscriptReady → Classified → ArticleDrafted → PendingReview → Approved → Published`

Publishing remains manual. Reviewers approve articles and publishers explicitly publish to Telegram.

## Durable Messaging

Wolverine is configured with SQL Server backed durable inbox/outbox storage, EF Core transactions, durable local queues, stale inbox/outbox recovery, retry cooldown policies, and fault events. Handlers remain idempotent by checking current video/article state and preserving the unique `YouTubeVideoId` and one-article-per-video constraints.

## Production Data Model

Phase 2 adds:

- `ProcessingJob` for started/completed timestamps, duration, status, failure reason, and correlation ID.
- `PromptTemplate` for editable, versioned, active/inactive prompts.
- `ArticleVersion` for immutable generated article revisions.
- Article quality score fields for technical depth, relevance, readability, practical value, and average score.
- `AuditTrail` for approval, rejection, and publishing actions.

## Observability

The admin app and worker use Serilog structured logging, OpenTelemetry tracing/metrics, and health checks. Metrics include discovered videos, generated/approved/published articles, failed jobs, and article generation duration.

## Security

The admin UI uses role-based authorization. Supported roles are Reader, Reviewer, Publisher, and Administrator. In containerized/internal deployments, the default header authentication handler can receive `X-User-Name` and `X-User-Roles` from a trusted reverse proxy.

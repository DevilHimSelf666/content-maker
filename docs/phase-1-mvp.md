# Phase 1 MVP Notes

Phase 1 creates a working YouTube-to-reviewable-article pipeline with manual Telegram publishing.

## Implemented Acceptance Criteria

1. Solution structure targets .NET 10.
2. EF Core SQL Server schema and initial migration exist.
3. Channel seeding is implemented by `DbInitializer` and is idempotent by `YouTubeChannelId`.
4. Worker periodically dispatches `DiscoverVideos` and reads YouTube RSS feeds.
5. Duplicate videos are ignored by checking and indexing `YouTubeVideoId`.
6. Transcript client calls the Python FastAPI transcript service.
7. Classification and article generation use `ILlmClient` and OpenAI-compatible configuration.
8. Generated articles are saved as `PendingReview`.
9. Blazor Admin UI lists videos and articles.
10. Admin can approve articles and manually publish approved articles to Telegram.
11. Docker Compose starts SQL Server, transcript service, worker, and admin UI.
12. README documents setup and local execution.

## Explicit MVP Safety Guardrails

- No automatic publishing exists in the worker pipeline.
- `PublishArticle` throws unless the article is already approved.
- LLM model names are only read from configuration.
- All message handlers check existing state before mutating data.

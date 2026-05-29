# Engineering Digest Runbook

## Health endpoints

- `/health` reports SQL Server, transcript service, and LLM provider health.
- `/ready` exposes the same readiness checks for orchestrators.

## Processing failures

1. Open **Jobs** in the admin UI.
2. Filter failed jobs by job type and inspect `FailureReason`.
3. Check worker logs using the correlation/trace ID stored on the job.
4. Fix the upstream dependency or invalid prompt/configuration.
5. Republish the relevant command from the UI where available, or use Wolverine dead-letter replay tooling for terminally failed messages.

## Restart recovery

Wolverine durable inbox/outbox messages are persisted in SQL Server. After a worker restart, Wolverine recovers unprocessed durable local queue messages and resumes processing. Handlers are idempotent and safe to retry.

## Prompt operations

1. Open **Prompts** as an Administrator.
2. Create a new version from the current prompt.
3. Edit and save the inactive version.
4. Activate the version only after review; activation deactivates other prompts of the same kind.
5. Use **Regenerate** on an article to compare the new version against prior article versions.

## Publishing operations

1. Review quality score and article content.
2. Expand Telegram preview to verify Markdown formatting and splitting.
3. Approve the article.
4. Publish manually to Telegram.
5. Confirm audit trail contains the publisher and timestamp.

## Cleanup jobs

The worker periodically publishes cleanup commands to abandon long-running jobs, soft-delete old transient failed job records, and refresh channel metadata timestamps.

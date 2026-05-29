PLAN.md

Phase 1 — MVP

Goal:

Create a working pipeline from YouTube video to reviewable Persian article.

Tasks:

* Create solution structure
* Create database schema
* Implement YouTube RSS ingestion
* Implement transcript service integration
* Implement article generation
* Implement article storage
* Implement Blazor admin panel
* Implement article approval workflow
* Implement Telegram publishing
* Docker Compose environment

Success Criteria:

* New videos are discovered automatically
* Duplicate videos are ignored
* Transcript can be retrieved
* Article is generated
* Article enters PendingReview
* Approved article can be published

⸻

Phase 2 — Production Ready

Goal:

Increase reliability and content quality.

Tasks:

* Wolverine durable messaging
* Failure handling
* Retry policies
* Article quality scoring
* Re-generate article feature
* Prompt management
* Rich logging
* Health checks
* Metrics

Success Criteria:

* System survives failures
* Jobs can resume
* Failed processing is visible

⸻

Phase 3 — Knowledge Platform

Goal:

Turn generated articles into searchable organizational knowledge.

Tasks:

* PostgreSQL pgvector
* RAG implementation
* Semantic search
* Internal knowledge portal
* Related article recommendations

Success Criteria:

* Users can search knowledge
* Relevant articles are retrieved

⸻

Phase 4 — Engineering Intelligence

Goal:

Generate actionable engineering insights.

Tasks:

* Azure DevOps integration
* Suggested backlog items
* Architecture recommendations
* Technical debt detection
* Trend analysis

Success Criteria:

* Articles generate actionable recommendations
* Engineering managers receive insights

⸻

Future Ideas

* Podcast ingestion
* Conference talk ingestion
* GitHub repository analysis
* Release note generation
* Weekly engineering digest
* Email newsletter
* Mattermost integration
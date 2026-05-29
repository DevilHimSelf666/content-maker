# Knowledge Platform

Phase 3 transforms approved Persian engineering articles into a long-term internal knowledge base.

## Modules

- `KnowledgeArticle`: searchable article projection with body, key takeaways, embeddings, quality scores, and usage counters.
- `KnowledgeCategory`: curated topics such as EF Core, ASP.NET Core, Architecture, Security, Performance, AI, and DevOps.
- `KnowledgeTag`: many-to-many technology labels.
- `KnowledgeReference`: source links, including the original YouTube video where available.
- `KnowledgeArticleRelation`: related article graph.
- `LearningPath`: curated multi-article study paths.
- `WeeklyDigest`: generated weekly engineering digest.
- `KnowledgeSearchLog`: analytics events for search frequency and topic popularity.

## Approval integration

When an article is approved, the Wolverine handler publishes `PromoteApprovedArticleToKnowledge`. The knowledge service idempotently creates or updates a `KnowledgeArticle`, infers a category, and stores source references. Existing approved articles can be backfilled with `ReindexKnowledge`.

## Learning paths

The MVP creates default paths:

- ASP.NET Core Path
- EF Core Path
- Architecture Path
- Security Path
- AI Engineering Path

Articles can belong to multiple paths. Assignment is rule-based in Phase 3 and can later be replaced by an AI curator.

## Weekly digest

`GenerateWeeklyKnowledgeDigest` builds inputs from important, viewed, and useful articles, then asks the configured LLM for a Persian markdown digest. Digests are stored for later publication or review.

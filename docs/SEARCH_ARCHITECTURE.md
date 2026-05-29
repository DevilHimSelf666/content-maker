# Search Architecture

## Storage

The operational ingestion database remains SQL Server. Phase 3 adds a PostgreSQL knowledge database with the `pgvector` extension for semantic search and long-term retrieval.

Docker Compose includes `pgvector/pgvector:pg17` and the `KnowledgeDbContext` migration creates vector columns for:

- `TitleEmbedding`
- `BodyEmbedding`
- `KeyTakeawaysEmbedding`

A HNSW cosine index is created for body embeddings.

## Search modes

### Full text search

Full text search checks title, body markdown, and key takeaways. Filters can narrow by category and tags.

### Semantic search

Semantic search embeds the user query through `IEmbeddingProvider`, compares it with stored title/body/takeaway vectors, and sorts by highest cosine similarity. The C# scorer provides deterministic fallback behavior for tests and development while the schema is pgvector-ready.

## Reindexing

- `RebuildKnowledgeEmbeddings` generates missing embeddings in batches.
- `ReindexKnowledge` promotes all approved articles, rebuilds related article links, and updates learning path membership.

## Analytics

Knowledge analytics records search queries and displays:

- search frequency
- most viewed articles
- most useful articles
- topic popularity

# RAG Service

Phase 3 adds retrieval augmented generation over approved Engineering Digest articles.

## Workflow

1. A user submits a question through `/knowledge/ask` or `POST /knowledge/ask`.
2. `IEmbeddingProvider` creates a question embedding using the configured embedding endpoint.
3. `IKnowledgeService` retrieves the best matching `KnowledgeArticle` records by semantic score and text score.
4. `RagService` builds a bounded markdown context from the top source articles.
5. `ILlmClient.AnswerFromContextAsync` asks the configured LLM to answer only from the internal context.
6. The response returns markdown, cited source articles, and a confidence score derived from retrieval relevance.

## Source citation

RAG answers include `RagSource` values with article id, title, and relevance score. The UI displays these sources under the answer so developers can inspect the original article before applying guidance.

## Configuration

Embeddings are provider-agnostic and use OpenAI-compatible `/embeddings` endpoints:

```json
"Embedding": {
  "BaseUrl": "https://api.openai.com/v1",
  "ApiKey": "",
  "Model": "",
  "Dimensions": 1536
}
```

No embedding model is hardcoded. Local OpenAI-compatible embedding servers can be used by changing `Embedding:BaseUrl` and `Embedding:Model`.

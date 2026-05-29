from __future__ import annotations

import re
from typing import Annotated

from fastapi import FastAPI, HTTPException
from pydantic import AliasChoices, BaseModel, Field
from youtube_transcript_api import YouTubeTranscriptApi, TranscriptsDisabled, NoTranscriptFound

app = FastAPI(title="Engineering Digest Transcript Service", version="0.1.0")


class TranscriptRequest(BaseModel):
    youtube_video_id: Annotated[str, Field(validation_alias=AliasChoices("YouTubeVideoId", "youTubeVideoId", "youtube_video_id"), min_length=3)]


class TranscriptResponse(BaseModel):
    youtube_video_id: str = Field(serialization_alias="YouTubeVideoId")
    text: str = Field(serialization_alias="Text")


def normalize_text(parts: list[str]) -> str:
    text = " ".join(part.strip() for part in parts if part.strip())
    text = re.sub(r"\s+", " ", text)
    return text.strip()


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/transcripts", response_model=TranscriptResponse, response_model_by_alias=True)
def get_transcript(request: TranscriptRequest) -> TranscriptResponse:
    try:
        transcript = YouTubeTranscriptApi.get_transcript(
            request.youtube_video_id,
            languages=["en", "en-US", "fa"],
        )
    except (TranscriptsDisabled, NoTranscriptFound) as exc:
        raise HTTPException(status_code=404, detail="Transcript not available") from exc
    except Exception as exc:  # FastAPI boundary: convert third-party failures to HTTP responses.
        raise HTTPException(status_code=502, detail="Failed to retrieve transcript") from exc

    text = normalize_text([entry.get("text", "") for entry in transcript])
    if not text:
        raise HTTPException(status_code=404, detail="Transcript is empty")

    return TranscriptResponse(youtube_video_id=request.youtube_video_id, text=text)

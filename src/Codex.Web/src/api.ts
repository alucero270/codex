export type SearchResult = {
  id: number;
  path: string;
  title: string;
  snippet: string;
  rank: number;
};

export type SearchResponse = {
  query: string;
  limit: number;
  results: SearchResult[];
};

export type DocumentResponse = {
  id: number;
  path: string;
  title: string;
  content: string;
  updatedAt: string;
};

const apiBaseUrl =
  // Default keeps local dev simple while allowing env-based overrides per environment.
  import.meta.env.VITE_CODEX_API_BASE_URL?.trim().replace(/\/+$/, "") ??
  "http://localhost:8080";

async function parseError(response: Response): Promise<string> {
  // Return server payload when available so UI surfaces actionable errors.
  const payload = await response.text();
  if (!payload) {
    return `Request failed with status ${response.status}.`;
  }

  return `Request failed with status ${response.status}: ${payload}`;
}

export async function searchDocuments(query: string, limit = 10): Promise<SearchResponse> {
  const response = await fetch(`${apiBaseUrl}/api/search`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ query, limit })
  });

  if (!response.ok) {
    throw new Error(await parseError(response));
  }

  return (await response.json()) as SearchResponse;
}

export async function getDocumentById(id: number): Promise<DocumentResponse> {
  const response = await fetch(`${apiBaseUrl}/api/documents/${id}`);

  if (!response.ok) {
    throw new Error(await parseError(response));
  }

  return (await response.json()) as DocumentResponse;
}

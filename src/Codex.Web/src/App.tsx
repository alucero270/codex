import { FormEvent, Fragment, ReactNode, useMemo, useState } from "react";
import { DocumentResponse, SearchResult, getDocumentById, searchDocuments } from "./api";

const DEFAULT_LIMIT = 10;

function renderSnippet(snippet: string): ReactNode[] {
  // Keep rendering safe by treating only <mark> tags as formatting hints.
  return snippet
    .split(/(<mark>.*?<\/mark>)/g)
    .filter((part) => part.length > 0)
    .map((part, index) => {
      if (part.startsWith("<mark>") && part.endsWith("</mark>")) {
        const content = part.slice("<mark>".length, -"</mark>".length);
        return <mark key={`${index}-mark`}>{content}</mark>;
      }

      return <Fragment key={`${index}-text`}>{part}</Fragment>;
    });
}

export default function App() {
  const [queryInput, setQueryInput] = useState("");
  const [activeQuery, setActiveQuery] = useState("");
  const [results, setResults] = useState<SearchResult[]>([]);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [searchLoading, setSearchLoading] = useState(false);
  const [selectedDocument, setSelectedDocument] = useState<DocumentResponse | null>(null);
  const [documentError, setDocumentError] = useState<string | null>(null);
  const [documentLoadingId, setDocumentLoadingId] = useState<number | null>(null);

  const canSubmit = queryInput.trim().length > 0 && !searchLoading;

  const emptyStateLabel = useMemo(() => {
    if (!activeQuery) {
      return "Enter a query to search indexed documents.";
    }

    if (results.length === 0 && !searchLoading) {
      return "No results found for this query.";
    }

    return null;
  }, [activeQuery, results.length, searchLoading]);

  async function handleSearchSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedQuery = queryInput.trim();
    if (!trimmedQuery) {
      return;
    }

    setSearchLoading(true);
    setSearchError(null);
    setSelectedDocument(null);
    setDocumentError(null);
    setActiveQuery(trimmedQuery);

    try {
      const response = await searchDocuments(trimmedQuery, DEFAULT_LIMIT);
      setResults(response.results);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Search request failed.";
      setResults([]);
      setSearchError(message);
    } finally {
      setSearchLoading(false);
    }
  }

  async function handleOpenDocument(result: SearchResult) {
    // Use API-provided id so document fetch is independent from list ordering.
    setDocumentLoadingId(result.id);
    setDocumentError(null);

    try {
      const document = await getDocumentById(result.id);
      setSelectedDocument(document);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Document request failed.";
      setSelectedDocument(null);
      setDocumentError(message);
    } finally {
      setDocumentLoadingId(null);
    }
  }

  return (
    <main className="page">
      <section className="panel">
        <h1 className="title">Codex Search</h1>
        <p className="subtitle">
          Search indexed Atlas markdown documents and open a document preview.
        </p>

        <form className="search-form" onSubmit={handleSearchSubmit}>
          <input
            value={queryInput}
            onChange={(event) => setQueryInput(event.target.value)}
            className="search-input"
            type="text"
            placeholder="Search documentation..."
            aria-label="Search query"
          />
          <button className="search-button" type="submit" disabled={!canSubmit}>
            {searchLoading ? "Searching..." : "Search"}
          </button>
        </form>

        {searchError ? <p className="state error">{searchError}</p> : null}
        {emptyStateLabel ? <p className="state">{emptyStateLabel}</p> : null}

        {results.length > 0 ? (
          <ul className="results-list">
            {results.map((result) => (
              <li className="result-card" key={result.id}>
                <button
                  className="result-button"
                  type="button"
                  onClick={() => handleOpenDocument(result)}
                  disabled={documentLoadingId !== null}
                >
                  <span className="result-title">{result.title || result.path}</span>
                  <span className="result-path">{result.path}</span>
                  <span className="result-snippet">{renderSnippet(result.snippet)}</span>
                </button>
              </li>
            ))}
          </ul>
        ) : null}
      </section>

      <section className="panel">
        <h2 className="doc-title">Document</h2>
        {documentLoadingId !== null ? <p className="state">Loading document...</p> : null}
        {documentError ? <p className="state error">{documentError}</p> : null}

        {!selectedDocument && !documentLoadingId && !documentError ? (
          <p className="state">Select a search result to view document content.</p>
        ) : null}

        {selectedDocument ? (
          <article className="document-view">
            <header className="document-meta">
              <h3>{selectedDocument.title || selectedDocument.path}</h3>
              <p>ID: {selectedDocument.id}</p>
              <p>Path: {selectedDocument.path}</p>
              <p>Updated: {new Date(selectedDocument.updatedAt).toLocaleString()}</p>
            </header>
            {/* MVP: render markdown as plain text to keep the viewer dependency-free. */}
            <pre className="document-content">{selectedDocument.content}</pre>
          </article>
        ) : null}
      </section>
    </main>
  );
}

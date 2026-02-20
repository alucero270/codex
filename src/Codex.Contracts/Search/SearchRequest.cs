using System.ComponentModel.DataAnnotations;

namespace Codex.Contracts.Search;

public sealed record SearchRequest
{
    [Required(AllowEmptyStrings = false)]
    [StringLength(500)]
    public string Query { get; init; } = string.Empty;

    // Keep bounded limits for predictable query cost in Phase 1.
    [Range(1, 50)]
    public int Limit { get; init; } = 10;
}

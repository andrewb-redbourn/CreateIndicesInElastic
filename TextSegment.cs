public class TextSegment
{
    public required string DocReference { get; init; }
    public required string Heading { get; init; }
    public string? Content { get; set; } = string.Empty;
    public DateTime ExtractedOn { get; init; }
}
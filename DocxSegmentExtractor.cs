using System.Diagnostics;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Spreadsheet;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

internal partial class DocxSegmentExtractor
{
    internal static List<TextSegment> ExtractSegments(string docxFilePath)
    {
        List<TextSegment> segments = new List<TextSegment>();
        TextSegment? currentSegment = null;
        var docName = Path.GetFileNameWithoutExtension(docxFilePath);
        try
        {
            using var document = WordprocessingDocument.Open(docxFilePath, false);
            var body = document?.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                return segments;
            }

            StringBuilder? builder = null;
            bool isHeader = false;
            foreach (var element in body.Elements())
            {
                if (element is Paragraph paragraph)
                {
                    var text = GetParagraphText(paragraph);
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    if (isHeader || IsHeading(paragraph))
                    {
                        isHeader = false;
                        if (currentSegment is { Heading.Length: > 0 } && builder is { Length: > 0 })
                        {
                            currentSegment.Content = builder?.ToString() ?? string.Empty;
                            segments.Add(currentSegment);
                        }

                        currentSegment = new TextSegment
                        {
                            DocReference = docName.ToLower(),
                            Heading = text!.ToLower(),
                            ExtractedOn = DateTime.UtcNow
                        };
                        builder = new();
                    }
                    else if (!string.IsNullOrWhiteSpace(text))
                    {
                        builder?.AppendLine(MultiSpaceRegex().Replace(text.ToLower(), " ").TrimStart());
                    } else {
                        isHeader = true;
                    }
                }
            }

            if (currentSegment is { } && builder is { Length: > 0 })
            {
                currentSegment.Content = builder.ToString();
                segments.Add(currentSegment);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        var segmentStatic = (ReadOnlySpan<TextSegment>)segments.ToArray();
        var bulkCoding = new StringBuilder();
        for (var index = 1; index <= segmentStatic.Length; index++)
        {
            var segment = segmentStatic[index - 1];
            bulkCoding.AppendLine($"{{\"index\":{{\"_id\":\"{index}\"}}}}");
            bulkCoding.AppendLine(System.Text.Json.JsonSerializer.Serialize(segment));
        }

        return segments;
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        return string.Join("", paragraph.Descendants<Text>().Select(t => t.Text));
    }

    /// <summary>
    /// Determines whether the given paragraph is a heading.
    /// </summary>
    /// <param name="paragraph">The paragraph to check.</param>
    /// <returns>True if the paragraph is a heading; otherwise, false.</returns>
    private static bool IsHeading(Paragraph paragraph)
    {
        var paragraphProperties = paragraph.ParagraphProperties;

        var paragraphStyle = paragraphProperties?.ParagraphStyleId?.Val?.Value ?? string.Empty;
        var isHeading = paragraphStyle.StartsWith("Heading") || paragraphStyle.StartsWith("Title");
        if (!isHeading) {
            var allText = paragraph.Descendants<Run>().Select(run => run.InnerText).ToList();
            isHeading = paragraph.Descendants<Run>()
                .Any(run => run.RunProperties?.Bold != null && run.InnerText.Trim().Length > 0 && run.InnerText.Equals(paragraph.InnerText, StringComparison.OrdinalIgnoreCase));

            // var runProperties = paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.RunProperties>().FirstOrDefault(prop=>!string.IsNullOrWhiteSpace(prop.InnerText));
            // if (runProperties is not null)
            // {
            //     var bold = runProperties.Bold?.Val;
            //     isHeading = bold?.Value??false;
            // }
        }
        return isHeading;
    }

    [GeneratedRegex(@"(\s{2,}|\r\n|\r|\n)", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex MultiSpaceRegex();
}
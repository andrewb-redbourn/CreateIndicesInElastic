using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace CreateIndicesInElastic;

internal partial class PdfSegmentExtractor
{
    public static List<TextSegment> ExtractSegments(string pdfFilePath)
    {
        List<TextSegment> segments = new List<TextSegment>();
        TextSegment? currentSegment = null;
        StringBuilder? builder = null;
        var docName = Path.GetFileNameWithoutExtension(pdfFilePath);
        try
        {
            //builder = new();
            using var document = PdfDocument.Open(pdfFilePath);
            var endOfFile = false;
            for (int pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
            {
                if (endOfFile)
                {
                    break;
                }
                var page = document.GetPage(pageNumber);
                var letters = page.Letters;

                var wordExtractor = NearestNeighbourWordExtractor.Instance;
                var words = wordExtractor.GetWords(letters);

                var pageSegmenter = DocstrumBoundingBoxes.Instance;
                var textBlox = pageSegmenter.GetBlocks(words);

                var readingOrder = UnsupervisedReadingOrderDetector.Instance;
                var orderedTextBlox = readingOrder.Get(textBlox);

                var index1 = 1;
                PdfRectangle? oldLocation = null, newLocation = null;
                foreach (var block in orderedTextBlox)
                {
                    if (endOfFile)
                    {
                        break;
                    }
                    oldLocation = newLocation;
                    newLocation = block.BoundingBox;
                    bool isHeader = (newLocation?.Bottom??0) - 50 > (oldLocation?.Top ?? 0);
                    var lines = block.Text.Split('\n');
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || line == "NFP")
                        {
                            continue;
                        }

                        // Not interested in anything after this point
                        if (line.Contains("price list", StringComparison.OrdinalIgnoreCase))
                        {
                            endOfFile = true;
                            break;
                        }

                        if (isHeader || IsHeading(line))
                        {
                            isHeader = false;
                            if (currentSegment is { Heading.Length: > 0 })
                            {
                                currentSegment.Content = builder?.ToString() ?? string.Empty;
                                segments.Add(currentSegment);
                            }

                            currentSegment = new TextSegment
                            {
                                DocReference = docName,
                                Heading = $"Page {pageNumber} - Paragraph {index1} {line}",
                                Content = null,
                                ExtractedOn = DateTime.UtcNow
                            };
                            builder = new();
                            index1++;
                        }
                        else
                        {
                            builder?.Append(MultiSpaceRegex().Replace(line.ToLower(), " ").TrimStart()).Append(' ');
                        }
                    }
                }


            }

                if (currentSegment is { Heading.Length: > 0 } && builder is { Length: > 0 })
                {
                    currentSegment.Content = builder!.ToString().Trim();
                    segments.Add(currentSegment);
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        return segments;
    }

    private static bool IsHeading(string line)
    {
        return line.StartsWith("Chapter", StringComparison.OrdinalIgnoreCase) ||
               line.StartsWith("Section", StringComparison.OrdinalIgnoreCase) ||
               line.StartsWith("Part", StringComparison.OrdinalIgnoreCase) ||
               line.StartsWith("Advantage", StringComparison.OrdinalIgnoreCase);
    }

    private static Regex MultiSpaceRegex()
    {
        return MultiSpaceReg();
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiSpaceReg();
}
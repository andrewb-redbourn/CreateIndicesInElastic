namespace CreateIndicesInElastic;

public class ToElastic
{
    public static async Task IndexDocumentAsync(ExtractToElastic Elastic
                                              , string[] filePath
                                              , Func<string, IEnumerable<TextSegment>>? ExtractSegments)
    {
        if (ExtractSegments is null)
        {
            Console.WriteLine("No extractor found for this file type.");
            return;
        }
        var segments = ExtractSegments!.Invoke(filePath[0]);
        var rootFileName = filePath[1];
        foreach (var document in segments.Select(segment => new
                 {
                     filename = rootFileName, title = segment.Heading, text = segment.Content,
                     extractedon = DateTime.UtcNow
                 }))
        {
            var response = await Elastic.Client.IndexAsync(document, idx => idx.Index(Elastic.ElasticIndex));

            if (!response.IsValidResponse)
            {
                Console.WriteLine($"Failed to index segment: {response.DebugInformation}");
            }
        }                          
    }
}
using CreateIndicesInElastic;
using DotNetEnv;

var elasticUrl = string.Empty;
var elasticUser = string.Empty;
var elasticPassword = string.Empty;
var elasticApi = string.Empty;
var elasticIndex = string.Empty;
var filesToIndex = Array.Empty<string[]>();
var clearIndex = false;

ExtractToElastic extractToElastic;

ProcessArgs(args);

var startTime = DateTime.UtcNow;
Console.WriteLine($"Start Time: {startTime:O}");
//IndexDocumentsAsync(_elasticIndex, marketing());
IndexDocumentsAsync(elasticIndex, filesToIndex);
var endTime = DateTime.UtcNow;
Console.WriteLine($"End Time: {endTime:O}");

var timeTaken = endTime - startTime;
Console.WriteLine($"Time Taken: {timeTaken}");
// End of main program

const string helpMessage = @"
Usage: CreateIndicesInElastic [{elasticUrl} {elasticuser} {elasticpassword}] -i|--index index {-f|--file file} {-d|--dir directory} [-clear]
alternatively, set the environment variables ELASTIC_URL, ELASTIC_USER, ELASTIC_PASSWORD
   -i index: the name of the index to create (required)
at least one of the following:
   -f|--file file: the path to a file containing a list of comma-separated files and their names to index
   -d|--dir directory: the path to a directory of files to index
optionally:
   -clear will delete the index before creating it
   -h or --help flag will display this message
Currently, the program only supports .docx, .pptx, and .pdf files";

void ProcessArgs(string[] args)
{
    if (args.Contains("-h") || args.Contains("--help"))
    {
        Console.WriteLine(helpMessage);
        Environment.Exit(0);
    }

    var startPoint = 0;
    if (args[0].StartsWith('-'))
    {
        // must be the environment variables
        Env.Load();
        elasticUrl = Environment.GetEnvironmentVariable("ELASTIC_URL") ??
                      throw new ArgumentNullException("ELASTIC_URL");
        elasticUser = Environment.GetEnvironmentVariable("ELASTIC_USER") ??
                       throw new ArgumentNullException("ELASTIC_USER");
        elasticPassword = Environment.GetEnvironmentVariable("ELASTIC_PASSWORD");
        elasticApi = Environment.GetEnvironmentVariable("ELASTIC_APIKEY");
    }
    else
    {
        elasticUrl = args[0]?.Trim() ?? throw new ArgumentNullException("elasticUrl");
        startPoint = 1;
    }

    for (var index = startPoint; index < args.Length; index++)
    {
        switch (args[index].ToLower())
        {
            case "-i":
            case "--index":
                if (index + 1 < args.Length)
                {
                    elasticIndex = args[++index];
                    break;
                }

                throw new ArgumentNullException("index");
            case "-f":
            case "--file":
                if (index + 1 < args.Length)
                {
                    var fileList = File.ReadAllLines(args[++index]);
                    filesToIndex = new string[fileList.Length][];
                    var fileIndex = 0;
                    foreach (var file in fileList)
                    {
                        var name = file.Split(',');
                        filesToIndex[fileIndex++] = [name[0], name.Length > 1 ? name[1] : Path.GetFileName(name[0])];
                    }

                    break;
                }

                throw new ArgumentNullException("file");
            case "-d":
            case "--dir":
                if (++index < args.Length)
                {
                    var files = Directory.GetFiles(args[index]).Where(fl =>
                        fl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) || fl.EndsWith(".pptx",
                            StringComparison.OrdinalIgnoreCase) ||
                        fl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToArray();
                    filesToIndex = new string[files.Length][];
                    for (var fileIndex = 0; fileIndex < files.Length; fileIndex++)
                    {
                        filesToIndex[fileIndex] = [files[fileIndex], Path.GetFileName(files[fileIndex])];
                    }

                    break;
                }

                throw new ArgumentNullException("directory");
            case "-c":
            case "--clear":
                clearIndex = true;
                break;
            case "-u":
                case "--user":
                elasticUser = args[++index];
                break;
            case "-p":
                case "--password":
                elasticPassword = args[++index];
                break;
            case "-a":
            case "--apikey":
                elasticApi = args[++index];
                break;
            default:
                throw new NotSupportedException($"Unknown argument: {args[index]}");
        }
    }

    if (string.IsNullOrWhiteSpace(elasticIndex))
    {
        Console.WriteLine("**Index name is required**");
        Environment.Exit(-1);
    }

    if (string.IsNullOrWhiteSpace(elasticUrl))
    {
        Console.WriteLine("**URL is required**");
    } 
    if ((string.IsNullOrWhiteSpace(elasticUser) ||
        string.IsNullOrWhiteSpace(elasticPassword)) && string.IsNullOrWhiteSpace(elasticApi))
    {
        Console.WriteLine("(user, password) OR Api are required**");
        Environment.Exit(-3);
    }
}

async Task ProcessDocumentAsync(string[] docPath)
{
    var timeNow = DateTime.UtcNow;
    Console.WriteLine($"Ingesting {docPath[0]}...");
    await ToElastic.IndexDocumentAsync(extractToElastic, docPath, Path.GetExtension(docPath[0]) switch
    {
        ".docx" => DocxSegmentExtractor.ExtractSegments,
        ".pptx" => PptxSegmentExtractor.ExtractSegments,
        ".pdf" => PdfSegmentExtractor.ExtractSegments,
        _ => null
    });
    Console.WriteLine($"Time taken for ingestion: {DateTime.UtcNow - timeNow}");
}

void IndexDocumentsAsync(string index, ReadOnlySpan<string[]> documents)
{
    extractToElastic = new ExtractToElastic(index, elasticUrl, elasticUser, elasticPassword, elasticApi);
    extractToElastic.CreateIndex(clearIndex).GetAwaiter().GetResult();
    foreach (var documentToIndex in documents)
    {
        var timeNow = DateTime.UtcNow;
        Console.WriteLine($"Ingesting {documentToIndex[0]}...");
        ProcessDocumentAsync(documentToIndex).GetAwaiter().GetResult();
        Console.WriteLine($"Time taken for ingestion: {DateTime.UtcNow - timeNow}");
    }

    Console.WriteLine($"Index {index} completed.");
}

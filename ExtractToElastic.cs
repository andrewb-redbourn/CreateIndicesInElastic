﻿using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport;
using UglyToad.PdfPig.Fonts.TrueType.Tables;

namespace CreateIndicesInElastic;
public class ExtractToElastic
{
    private readonly ElasticsearchClient _elasticClient;
    public ElasticsearchClient Client => _elasticClient;
    public string Basicauth { get; init; }
    public string Url { get; init; }
    public string UserName { get; init; }
    public string ApiKey { get; set; }
    public string ElasticIndex { get;  init; }

    public ExtractToElastic(string _elasticIndex, string url, string userName, string password, string apikey)
    {
        Url = url;
        UserName = userName;
        Basicauth = password;
        ApiKey = apikey;
        ElasticIndex = _elasticIndex;
        
        var settings = new ElasticsearchClientSettings(new Uri(Url))
            //.Authentication(authenticator)
            .DefaultIndex(ElasticIndex)
            .EnableDebugMode();
        if (ApiKey is { Length: >0 })
        {
            settings.Authentication(new ApiKey(ApiKey));
        }
        else
        {
            settings.Authentication(new BasicAuthentication(UserName, Basicauth));
        }
        _elasticClient = new ElasticsearchClient(settings);

        var response = _elasticClient.PingAsync().GetAwaiter().GetResult();
        if (response.IsValidResponse)
        {
            Console.WriteLine("Connected Successfully");
        }
    }

    ~ExtractToElastic()
    {
        _elasticClient.ClosePointInTimeAsync().GetAwaiter().GetResult();
    }

    public async Task CreateIndex(bool clearIndex)
    {
        if ((await _elasticClient.Indices.ExistsAsync(ElasticIndex)).Exists)
        {
            if (clearIndex)
            {
                await _elasticClient.Indices.DeleteAsync(ElasticIndex);
            }
            else
            {
                Console.WriteLine("Index already exists.");
                return;
            }
        }

        var indexRequest = new CreateIndexRequest(ElasticIndex)
        {
            Mappings = new TypeMapping()
            {
                Properties = new Properties
                {
                    { "title", new TextProperty() },
                    { "text", new TextProperty() },
                    { "filename", new TextProperty() },
                    { "extractedon", new DateProperty() }
                }
            }
        };

        var createIndexResponse = await _elasticClient.Indices.CreateAsync(indexRequest);

        if (!createIndexResponse.IsValidResponse)
        {
            Console.WriteLine($"Failed to create index: {createIndexResponse.DebugInformation}");
        }
    }

}
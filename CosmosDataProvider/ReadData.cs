using System.Net;
using CosmosDBProcessor.Library;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CosmosDataProvider
{
    public class ReadData(ILoggerFactory loggerFactory)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<ReadData>();
        private readonly CosmosDbHandler _handler = new();

        [Function("ReadData")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "data")] HttpRequestData req)
        {
            _logger.LogInformation("Starting read operations from CosmosDB");

            var response = req.CreateResponse(HttpStatusCode.OK);
            string? database = Environment.GetEnvironmentVariable("COSMOS_DATABASE");
            string? container = Environment.GetEnvironmentVariable("COSMOS_CONTAINER");
            string? partitionKey = Environment.GetEnvironmentVariable("COSMOS_PARTITION_KEY_PATH");

            if (database is null || container is null || partitionKey is null)
            {
                response.StatusCode = HttpStatusCode.ExpectationFailed;
                response.WriteString("Could not load data source");
                return response;
            }

            await _handler.LoadDataSource(database, container, partitionKey);

            IOrderedQueryable<RepositoryItem> queryable = _handler.Container
                                                .GetItemLinqQueryable<RepositoryItem>();

            var matches = queryable.Where(i => i != null);

            using FeedIterator<RepositoryItem> linqFeed = matches.ToFeedIterator();

            List<RepositoryItem> repositoryItems = [];

            while (linqFeed.HasMoreResults)
            {
                FeedResponse<RepositoryItem> feedResponse = await linqFeed.ReadNextAsync();

                foreach (RepositoryItem item in feedResponse)
                {
                    repositoryItems.Add(item);
                }
            }

            var first = JsonConvert.SerializeObject(repositoryItems.First());

            Console.WriteLine(first);

            await response.WriteAsJsonAsync(repositoryItems);

            return response;
        }
    }
}

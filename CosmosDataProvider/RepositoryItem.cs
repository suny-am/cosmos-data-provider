namespace CosmosDataProvider;

public record RepositoryItem(
#pragma warning disable IDE1006 // Naming Styles
    string id,
    string name,
    string homepageUrl,
    string url,
    DateTime pushedAt,
    long diskUsage,
    int commitTotal,
    string description
#pragma warning restore IDE1006 // Naming Styles
);
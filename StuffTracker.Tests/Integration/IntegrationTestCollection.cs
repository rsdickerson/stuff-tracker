namespace StuffTracker.Tests.Integration;

/// <summary>
/// Test collection to ensure integration tests run sequentially to avoid database conflicts.
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<GraphQLTestFixture>
{
    // This class is just a marker for xUnit test collection
}


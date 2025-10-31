using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace StuffTracker.Tests.Integration;

/// <summary>
/// Helper class for sending GraphQL queries/mutations and parsing responses.
/// </summary>
public static class GraphQLTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// GraphQL request payload structure.
    /// </summary>
    public record GraphQLRequest(string Query, Dictionary<string, object>? Variables = null);

    /// <summary>
    /// GraphQL response structure containing data and errors.
    /// </summary>
    public record GraphQLResponse<T>(T? Data, GraphQLError[]? Errors);

    /// <summary>
    /// GraphQL error structure.
    /// </summary>
    public record GraphQLError(string Message, object? Extensions = null, GraphQLErrorLocation[]? Locations = null);

    /// <summary>
    /// GraphQL error location structure.
    /// </summary>
    public record GraphQLErrorLocation(int Line, int Column);

    /// <summary>
    /// Sends a GraphQL query or mutation to the test server and returns the parsed response.
    /// </summary>
    /// <param name="client">HTTP client from WebApplicationFactory</param>
    /// <param name="query">GraphQL query/mutation string</param>
    /// <param name="variables">Optional variables dictionary</param>
    /// <returns>GraphQL response with data and errors</returns>
    public static async Task<GraphQLResponse<JsonElement?>> SendGraphQLRequestAsync(
        HttpClient client,
        string query,
        Dictionary<string, object>? variables = null)
    {
        var request = new GraphQLRequest(query, variables);
        
        var response = await client.PostAsJsonAsync(
            "/graphql",
            request,
            JsonOptions);

        // GraphQL can return 400 with errors in response body, so don't throw on 400
        // We'll parse the response and let callers check for errors
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.BadRequest)
        {
            response.EnsureSuccessStatusCode();
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(responseContent);
        var root = jsonDocument.RootElement;

        JsonElement? data = null;
        GraphQLError[]? errors = null;

        if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind != JsonValueKind.Null)
        {
            data = dataElement;
        }

        if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
        {
            errors = JsonSerializer.Deserialize<GraphQLError[]>(errorsElement.GetRawText(), JsonOptions);
        }

        return new GraphQLResponse<JsonElement?>(data, errors);
    }

    /// <summary>
    /// Sends a GraphQL query or mutation and deserializes the data to a specific type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize data to</typeparam>
    /// <param name="client">HTTP client from WebApplicationFactory</param>
    /// <param name="query">GraphQL query/mutation string</param>
    /// <param name="variables">Optional variables dictionary</param>
    /// <returns>GraphQL response with typed data and errors</returns>
    public static async Task<GraphQLResponse<T>> SendGraphQLRequestAsync<T>(
        HttpClient client,
        string query,
        Dictionary<string, object>? variables = null)
    {
        var response = await SendGraphQLRequestAsync(client, query, variables);

        T? data = default;
        if (response.Data is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.Null && jsonElement.ValueKind != JsonValueKind.Undefined)
            {
                data = JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), JsonOptions);
            }
        }

        return new GraphQLResponse<T>(data, response.Errors);
    }

    /// <summary>
    /// Asserts that a GraphQL response has no errors.
    /// </summary>
    public static void ShouldHaveNoErrors<T>(this GraphQLResponse<T> response)
    {
        response.Errors.Should().BeNull("GraphQL response should not contain errors");
    }

    /// <summary>
    /// Asserts that a GraphQL response has errors.
    /// </summary>
    public static void ShouldHaveErrors<T>(this GraphQLResponse<T> response)
    {
        response.Errors.Should().NotBeNull("GraphQL response should contain errors");
        response.Errors.Should().NotBeEmpty("GraphQL response should contain at least one error");
    }

    /// <summary>
    /// Asserts that a GraphQL response has data.
    /// </summary>
    public static void ShouldHaveData<T>(this GraphQLResponse<T> response)
    {
        response.Data.Should().NotBeNull("GraphQL response should contain data");
    }

    /// <summary>
    /// Asserts that a GraphQL response has no data.
    /// </summary>
    public static void ShouldHaveNoData<T>(this GraphQLResponse<T> response)
    {
        response.Data.Should().BeNull("GraphQL response should not contain data");
    }

    /// <summary>
    /// Asserts that a GraphQL response has a specific error message.
    /// </summary>
    public static void ShouldHaveErrorContaining<T>(this GraphQLResponse<T> response, string expectedMessage)
    {
        response.ShouldHaveErrors();
        response.Errors.Should().Contain(e => 
            e.Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase),
            $"GraphQL response should contain an error with message containing '{expectedMessage}'");
    }
}

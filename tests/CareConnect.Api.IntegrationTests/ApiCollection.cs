namespace CareConnect.Api.IntegrationTests;

/// <summary>
/// One host and one in-memory database for the whole suite. Tests therefore run
/// sequentially and each one uses unique emails so they never collide.
/// </summary>
[CollectionDefinition(nameof(ApiCollection))]
public class ApiCollection : ICollectionFixture<CareConnectApiFactory>;

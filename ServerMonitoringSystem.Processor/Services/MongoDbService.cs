using MongoDB.Driver;
using ServerMonitoringSystem.Processor.Models;

namespace ServerMonitoringSystem.Processor.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration config)
    {
        var connectionString = config["MongoDB:ConnectionString"];
        var databaseName = config["MongoDB:DatabaseName"];

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public async Task SaveServerStatisticsAsync(ServerStatistics statistics)
    {
        var collection = _database.GetCollection<ServerStatistics>("ServerStatistics");
        await collection.InsertOneAsync(statistics);
    }

    public async Task SaveAlertAsync(Alert alert)
    {
        var collection = _database.GetCollection<Alert>("Alerts");
        await collection.InsertOneAsync(alert);
    }

    public async Task<ServerStatistics?> GetLastServerStatisticsAsync(string serverId)
    {
        var collection = _database.GetCollection<ServerStatistics>("ServerStatistics");
        var filter = Builders<ServerStatistics>.Filter.Eq(s => s.ServerIdentifier, serverId);
        var sort = Builders<ServerStatistics>.Sort.Descending(s => s.Timestamp);

        return await collection.Find(filter).Sort(sort).FirstOrDefaultAsync();
    }
}

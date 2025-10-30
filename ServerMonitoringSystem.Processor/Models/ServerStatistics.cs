using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ServerMonitoringSystem.Processor.Models;

public class ServerStatistics
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string ServerIdentifier { get; set; }
    public double MemoryUsage { get; set; }
    public double AvailableMemory { get; set; }
    public double CpuUsage { get; set; }
    public DateTime Timestamp { get; set; }
}

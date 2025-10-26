using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ServerMonitoringSystem.Processor.Models;

public class Alert
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string ServerIdentifier { get; set; }
    public string AlertType { get; set; } // "MemoryAnomaly", "CpuAnomaly", "HighMemoryUsage", "HighCpuUsage"
    public string Message { get; set; }
    public double CurrentValue { get; set; }
    public double PreviousValue { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsResolved { get; set; } = false;
}

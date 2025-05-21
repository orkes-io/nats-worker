using System.Text.Json.Serialization;

public readonly struct Payload
{
    [JsonPropertyName("workflowInstanceId")]
    public string workflowInstanceId { get; init; }
    [JsonPropertyName("taskIdToComplete")]
    public string taskIdToComplete { get; init; }
}
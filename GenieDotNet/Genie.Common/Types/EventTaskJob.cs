namespace Genie.Common.Types;

public record EventTaskJob
{
    public EventTaskJob() { }
    public EventTaskJob(string id, string job, EventTaskJobStatus status, DateTime recordDate, BaseResponse? response = null, string? ex = null)
    {
        Id = id;
        Job = job;
        Status = status;
        RecordDate = recordDate;
        Response = response;
        Exception = ex;
    }

    public string? Id { get; set; }
    public string? Job { get; set; }
    public string? Exception { get; set; }
    public EventTaskJobStatus Status { get; set; }
    public DateTime RecordDate { get; set; }
    public BaseResponse? Response { get; set; }
}

public enum EventTaskJobStatus
{
    InProgress = 0,
    Completed = 1,
    Errored = 2
}
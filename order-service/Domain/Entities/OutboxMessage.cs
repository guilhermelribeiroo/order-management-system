using System.Drawing;

namespace Domain.Entities
{
    public class OutboxMessage : BaseEntity
    {
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime? ProcessedAt { get; set; }
        public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
        public int RetryCount { get; set; } = 0;
        public string? Error { get; set; }
        public DateTime? NextRetryAt { get; set; }

        public OutboxMessage(string type, string payload)
        {
            Type = type;
            Payload = payload;
        }
    }

    public enum OutboxStatus
    {
        Pending = 1,
        Published = 2,
        Failed = 3
    }
}

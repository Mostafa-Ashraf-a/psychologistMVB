using System;
using System.Collections.Generic;

namespace MentalHealthAssessment.Domain.Entities
{
    public class PatientTestSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PatientId { get; set; }
        public string TestId { get; set; }
        public Test Test { get; set; }
        public DeliveryMode CurrentDeliveryMode { get; set; }
        public bool IsCompleted { get; set; }
        public int TotalScore { get; set; }
        public string? AIAnalysisSummary { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public List<PatientResponse> Responses { get; set; } = new();
    }
}

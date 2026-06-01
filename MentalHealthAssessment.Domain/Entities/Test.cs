using System;
using System.Collections.Generic;

namespace MentalHealthAssessment.Domain.Entities
{
    public class Test
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; }
        public DeliveryMode DeliveryMode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public List<Question> Questions { get; set; } = new();
    }

    public enum DeliveryMode
    {
        ForceTraditional = 0,
        ForceAIChat = 1,
        PatientChoice = 2
    }
}

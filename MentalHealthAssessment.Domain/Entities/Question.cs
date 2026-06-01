using System;
using System.Collections.Generic;

namespace MentalHealthAssessment.Domain.Entities
{
    public class Question
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TestId { get; set; }
        public string Text { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public List<Option> Options { get; set; } = new();
    }
}

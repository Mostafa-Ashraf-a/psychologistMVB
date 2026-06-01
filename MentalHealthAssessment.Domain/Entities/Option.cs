using System;

namespace MentalHealthAssessment.Domain.Entities
{
    public class Option
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string QuestionId { get; set; }
        public string Text { get; set; }
        public int Score { get; set; }
        public string Tag { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
    }
}

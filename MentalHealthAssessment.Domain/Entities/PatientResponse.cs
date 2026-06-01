using System;

namespace MentalHealthAssessment.Domain.Entities
{
    public class PatientResponse
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PatientTestSessionId { get; set; }
        public PatientTestSession PatientTestSession { get; set; }
        public string QuestionId { get; set; }
        public Question Question { get; set; }
        public string? OptionId { get; set; }
        public Option? Option { get; set; }
        public string? AnswerText { get; set; } // The exact text inputted by user (especially in AI Chat)
        public int MappedScore { get; set; }
        public string? MappedTag { get; set; }
        public double AiConfidence { get; set; } // The confidence of Gemini mapping (e.g. 0.0 to 1.0)
        public string? AiNote { get; set; } // AI analysis notes/remarks about this answer
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
    }
}

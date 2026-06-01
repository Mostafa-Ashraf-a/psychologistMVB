using System.Collections.Generic;
using System.Threading.Tasks;
using MentalHealthAssessment.Domain.Entities;

namespace MentalHealthAssessment.Application.Interfaces
{
    public interface IFirestoreService
    {
        Task SaveTestAsync(Test test);
        Task<Test?> GetTestByIdAsync(string testId);
        Task<List<Test>> GetAllTestsAsync();
        Task SavePatientTestSessionAsync(PatientTestSession session);
        Task<PatientTestSession?> GetPatientTestSessionByIdAsync(string sessionId);
        Task SavePatientResponseAsync(string sessionId, PatientResponse response);
        Task<List<PatientResponse>> GetResponsesForSessionAsync(string sessionId);
    }
}

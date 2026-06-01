using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using MentalHealthAssessment.Application.Interfaces;
using MentalHealthAssessment.Domain.Entities;

namespace MentalHealthAssessment.Infrastructure.Services
{
    public class FirestoreService : IFirestoreService
    {
        private readonly FirestoreDb _db;

        public FirestoreService(IConfiguration configuration)
        {
            var projectId = configuration["Firebase:ProjectId"] ?? "mentalhealth-ee5f9";
            var credentialPath = configuration["Firebase:CredentialFile"] ?? "mentalhealth-ee5f9-firebase-adminsdk-fbsvc-3ad23b3fcd.json";

            // Initialize Firestore connection
            var builder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                CredentialsPath = credentialPath
            };
            _db = builder.Build();
        }

        public async Task SaveTestAsync(Test test)
        {
            var testRef = _db.Collection("tests").Document(test.Id);
            var testData = new Dictionary<string, object>
            {
                { "id", test.Id },
                { "title", test.Title },
                { "description", test.Description },
                { "deliveryMode", (int)test.DeliveryMode },
                { "createdAt", test.CreatedAt.ToUniversalTime() },
                { "createdBy", test.CreatedBy }
            };
            await testRef.SetAsync(testData);

            // Save sub-collections (Questions and Options)
            if (test.Questions != null)
            {
                foreach (var question in test.Questions)
                {
                    var questionRef = testRef.Collection("questions").Document(question.Id);
                    var questionData = new Dictionary<string, object>
                    {
                        { "id", question.Id },
                        { "testId", question.TestId },
                        { "text", question.Text },
                        { "order", question.Order },
                        { "createdAt", question.CreatedAt.ToUniversalTime() },
                        { "createdBy", question.CreatedBy }
                    };
                    await questionRef.SetAsync(questionData);

                    if (question.Options != null)
                    {
                        foreach (var option in question.Options)
                        {
                            var optionRef = questionRef.Collection("options").Document(option.Id);
                            var optionData = new Dictionary<string, object>
                            {
                                { "id", option.Id },
                                { "questionId", option.QuestionId },
                                { "text", option.Text },
                                { "score", option.Score },
                                { "tag", option.Tag },
                                { "createdAt", option.CreatedAt.ToUniversalTime() },
                                { "createdBy", option.CreatedBy }
                            };
                            await optionRef.SetAsync(optionData);
                        }
                    }
                }
            }
        }

        public async Task<Test?> GetTestByIdAsync(string testId)
        {
            var testRef = _db.Collection("tests").Document(testId);
            var snapshot = await testRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return null;

            var test = new Test
            {
                Id = snapshot.GetValue<string>("id"),
                Title = snapshot.GetValue<string>("title"),
                Description = snapshot.GetValue<string>("description"),
                DeliveryMode = (DeliveryMode)snapshot.GetValue<int>("deliveryMode"),
                CreatedAt = snapshot.GetValue<DateTime>("createdAt"),
                CreatedBy = snapshot.GetValue<string>("createdBy")
            };

            // Retrieve Questions
            var questionsSnapshot = await testRef.Collection("questions").GetSnapshotAsync();
            foreach (var qDoc in questionsSnapshot.Documents)
            {
                var question = new Question
                {
                    Id = qDoc.GetValue<string>("id"),
                    TestId = qDoc.GetValue<string>("testId"),
                    Text = qDoc.GetValue<string>("text"),
                    Order = qDoc.GetValue<int>("order"),
                    CreatedAt = qDoc.GetValue<DateTime>("createdAt"),
                    CreatedBy = qDoc.GetValue<string>("createdBy")
                };

                // Retrieve Options
                var optionsSnapshot = await qDoc.Reference.Collection("options").GetSnapshotAsync();
                foreach (var oDoc in optionsSnapshot.Documents)
                {
                    question.Options.Add(new Option
                    {
                        Id = oDoc.GetValue<string>("id"),
                        QuestionId = oDoc.GetValue<string>("questionId"),
                        Text = oDoc.GetValue<string>("text"),
                        Score = oDoc.GetValue<int>("score"),
                        Tag = oDoc.GetValue<string>("tag"),
                        CreatedAt = oDoc.GetValue<DateTime>("createdAt"),
                        CreatedBy = oDoc.GetValue<string>("createdBy")
                    });
                }

                test.Questions.Add(question);
            }

            // Order questions
            test.Questions = test.Questions.OrderBy(q => q.Order).ToList();
            return test;
        }

        public async Task<List<Test>> GetAllTestsAsync()
        {
            var testsRef = _db.Collection("tests");
            var snapshot = await testsRef.GetSnapshotAsync();
            var tests = new List<Test>();

            foreach (var doc in snapshot.Documents)
            {
                var test = await GetTestByIdAsync(doc.Id);
                if (test != null)
                {
                    tests.Add(test);
                }
            }

            return tests;
        }

        public async Task SavePatientTestSessionAsync(PatientTestSession session)
        {
            var sessionRef = _db.Collection("patientTestSessions").Document(session.Id);
            var sessionData = new Dictionary<string, object>
            {
                { "id", session.Id },
                { "patientId", session.PatientId },
                { "testId", session.TestId },
                { "currentDeliveryMode", (int)session.CurrentDeliveryMode },
                { "isCompleted", session.IsCompleted },
                { "totalScore", session.TotalScore },
                { "createdAt", session.CreatedAt.ToUniversalTime() },
                { "createdBy", session.CreatedBy }
            };

            if (session.AIAnalysisSummary != null)
            {
                sessionData.Add("aiAnalysisSummary", session.AIAnalysisSummary);
            }

            await sessionRef.SetAsync(sessionData);

            if (session.Responses != null)
            {
                foreach (var response in session.Responses)
                {
                    await SavePatientResponseAsync(session.Id, response);
                }
            }
        }

        public async Task<PatientTestSession?> GetPatientTestSessionByIdAsync(string sessionId)
        {
            var sessionRef = _db.Collection("patientTestSessions").Document(sessionId);
            var snapshot = await sessionRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return null;

            var session = new PatientTestSession
            {
                Id = snapshot.GetValue<string>("id"),
                PatientId = snapshot.GetValue<string>("patientId"),
                TestId = snapshot.GetValue<string>("testId"),
                CurrentDeliveryMode = (DeliveryMode)snapshot.GetValue<int>("currentDeliveryMode"),
                IsCompleted = snapshot.GetValue<bool>("isCompleted"),
                TotalScore = snapshot.GetValue<int>("totalScore"),
                CreatedAt = snapshot.GetValue<DateTime>("createdAt"),
                CreatedBy = snapshot.GetValue<string>("createdBy"),
                AIAnalysisSummary = snapshot.ContainsField("aiAnalysisSummary") ? snapshot.GetValue<string>("aiAnalysisSummary") : null
            };

            // Retrieve associated test details
            session.Test = await GetTestByIdAsync(session.TestId);

            // Retrieve responses
            session.Responses = await GetResponsesForSessionAsync(sessionId);

            return session;
        }

        public async Task SavePatientResponseAsync(string sessionId, PatientResponse response)
        {
            var responseRef = _db.Collection("patientTestSessions").Document(sessionId)
                .Collection("responses").Document(response.Id);

            var responseData = new Dictionary<string, object>
            {
                { "id", response.Id },
                { "patientTestSessionId", response.PatientTestSessionId },
                { "questionId", response.QuestionId },
                { "mappedScore", response.MappedScore },
                { "aiConfidence", response.AiConfidence },
                { "createdAt", response.CreatedAt.ToUniversalTime() },
                { "createdBy", response.CreatedBy }
            };

            if (response.OptionId != null) responseData.Add("optionId", response.OptionId);
            if (response.AnswerText != null) responseData.Add("answerText", response.AnswerText);
            if (response.MappedTag != null) responseData.Add("mappedTag", response.MappedTag);
            if (response.AiNote != null) responseData.Add("aiNote", response.AiNote);

            await responseRef.SetAsync(responseData);
        }

        public async Task<List<PatientResponse>> GetResponsesForSessionAsync(string sessionId)
        {
            var responsesRef = _db.Collection("patientTestSessions").Document(sessionId).Collection("responses");
            var snapshot = await responsesRef.GetSnapshotAsync();
            var responses = new List<PatientResponse>();

            foreach (var doc in snapshot.Documents)
            {
                var response = new PatientResponse
                {
                    Id = doc.GetValue<string>("id"),
                    PatientTestSessionId = doc.GetValue<string>("patientTestSessionId"),
                    QuestionId = doc.GetValue<string>("questionId"),
                    MappedScore = doc.GetValue<int>("mappedScore"),
                    AiConfidence = doc.GetValue<double>("aiConfidence"),
                    CreatedAt = doc.GetValue<DateTime>("createdAt"),
                    CreatedBy = doc.GetValue<string>("createdBy"),
                    OptionId = doc.ContainsField("optionId") ? doc.GetValue<string>("optionId") : null,
                    AnswerText = doc.ContainsField("answerText") ? doc.GetValue<string>("answerText") : null,
                    MappedTag = doc.ContainsField("mappedTag") ? doc.GetValue<string>("mappedTag") : null,
                    AiNote = doc.ContainsField("aiNote") ? doc.GetValue<string>("aiNote") : null
                };

                responses.Add(response);
            }

            return responses;
        }
    }
}

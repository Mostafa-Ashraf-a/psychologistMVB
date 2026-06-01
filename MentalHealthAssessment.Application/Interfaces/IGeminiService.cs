using System.Threading.Tasks;

namespace MentalHealthAssessment.Application.Interfaces
{
    public interface IGeminiService
    {
        /// <summary>
        /// Analyzes patient chat input, extracts the sentiment/context, and maps it to the closest predefined test option.
        /// </summary>
        /// <param name="conversationHistory">Full context of the active chat session.</param>
        /// <param name="latestUserMessage">The newest response from the patient.</param>
        /// <param name="questionOptionsJson">Predefined options set by the admin for the current question.</param>
        /// <returns>A JSON string containing: mappedOptionId, confidence (0.0-1.0), isSure (bool), and an aiNote.</returns>
        Task<string> AnalyzeChatResponseAsync(string conversationHistory, string latestUserMessage, string questionOptionsJson);

        /// <summary>
        /// Generates personalized self-help recommendations based on the completed chat conversation.
        /// </summary>
        /// <param name="conversationHistory">Full transcript of the completed test session.</param>
        /// <returns>Personalized self-help insights and recommendations.</returns>
        Task<string> GenerateSelfHelpInsightsAsync(string conversationHistory);
    }
}

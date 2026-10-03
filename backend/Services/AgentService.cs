using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace backend.Services;

public class AgentService(IConfiguration configuration)
{
    private readonly AIAgent _agent = CreateAgent(configuration);

    public async Task<string> GenerateResponseAsync(
        List<ChatMessage> messages)
    {
        var response = await _agent.RunAsync(messages);

        return response.Text ?? string.Empty;
    }

    private static AIAgent CreateAgent(IConfiguration configuration)
    {
        var apiKey = configuration["GEMINI_API_KEY"]
            ?? throw new InvalidOperationException(
                "Gemini API key is not configured.");

        var model = configuration["GEMINI_MODEL"]
            ?? "gemini-2.5-flash";

        var chatClient = new Client(
            vertexAI: false,
            apiKey: apiKey
        ).AsIChatClient(model);

        return new ChatClientAgent(
            chatClient,
            name: "CbotAssistant",
            instructions: """
                You are an AI assistant for a cBot development platform.

                Help users design, understand, debug, and improve cTrader cBots.

                When users discuss trading strategies, explain the strategy clearly before generating code.

                When users request cBot code, produce valid C# compatible with the cTrader Automate API.

                Do not claim that generated code has been compiled or tested unless the system explicitly provides that result.

                Be concise and practical.
                """
        );
    }
}
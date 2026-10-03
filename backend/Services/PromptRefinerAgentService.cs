using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace backend.Services;

public class PromptRefinerAgentService(IConfiguration configuration)
{
    private readonly AIAgent _agent = CreateAgent(configuration);

    public async Task<string> RefineAsync(
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
            name: "PromptRefinerAgent",
            instructions: """
                You are the Prompt Refiner Agent for a cTrader cBot development platform.

                Your job is to transform the user's conversation and understood trading strategy into a precise technical specification for a cBot.

                Analyze the complete conversation history.

                Identify:
                - Trading strategy
                - Entry conditions
                - Exit conditions
                - Indicators
                - Indicator parameters
                - Trading symbol or symbols
                - Timeframe
                - Position sizing
                - Stop loss
                - Take profit
                - Risk management
                - Position management
                - Additional constraints

                Do not generate C# code.

                Do not invent requirements that the user did not specify.

                If an important requirement is genuinely missing, clearly identify it as requiring clarification.

                Produce a concise, structured cBot specification that another agent can use to generate the source code.

                Separate confirmed requirements from unresolved requirements.

                Do not claim that the specification has been approved by the user.
                """
        );
    }
}
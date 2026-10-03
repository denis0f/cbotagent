using backend.Tools;
using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace backend.Services;

public class CoderAgentService(
    IConfiguration configuration,
    SaveBotVersionTool saveBotVersionTool)
{
    private readonly AIAgent _agent = CreateAgent(
        configuration,
        saveBotVersionTool);

    public async Task<string> GenerateCodeAsync(
        string refinedPrompt)
    {
        var message = new ChatMessage(
            ChatRole.User,
            $"""
            Generate the cTrader cBot source code from the following technical specification.

            Technical specification:

            {refinedPrompt}

            Return only the complete C# source code.

            The source must be compatible with the cTrader Automate API.

            Do not wrap the code in markdown code fences.

            Do not include explanations before or after the source code.

            Do not claim that the code has been compiled or tested.

            Do not save the source code.

            Do not compile the source code.
            """
        );

        var response = await _agent.RunAsync(
            new List<ChatMessage>
            {
                message
            });

        return response.Text ?? string.Empty;
    }

    public async Task<string> SaveGeneratedCodeAsync(
        int userId,
        int sessionId,
        string botName,
        string sourceCode)
    {
        var message = new ChatMessage(
            ChatRole.User,
            $"""
            The user has explicitly requested that the generated cBot source code be saved.

            User ID:
            {userId}

            Bot creation session ID:
            {sessionId}

            Bot name:
            {botName}

            Generated source code:

            {sourceCode}

            Use the save_bot_version tool to save this source code.

            Do not compile the source code.

            Do not modify the source code before saving it.

            Only report that the source was saved if the save_bot_version tool succeeds.
            """
        );

        var response = await _agent.RunAsync(
            new List<ChatMessage>
            {
                message
            });

        return response.Text ?? string.Empty;
    }

    private static AIAgent CreateAgent(
        IConfiguration configuration,
        SaveBotVersionTool saveBotVersionTool)
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

        var saveTool = AIFunctionFactory.Create(
            saveBotVersionTool.SaveBotVersion);

        return new ChatClientAgent(
            chatClient,
            name: "CbotCoderAgent",
            instructions: """
                You are the Coder Agent for a cTrader cBot development platform.

                Your responsibility is to transform an approved technical cBot specification into complete C# source code for cTrader Automate.

                Follow the specification exactly.

                Do not invent trading requirements that are not present in the specification.

                Use the cTrader Automate API correctly.

                Produce complete source code suitable for a cBot.

                Do not provide explanations when generating source code.

                Do not use markdown code fences.

                Do not claim that the source has been compiled or tested.

                You have access to the save_bot_version tool.

                Only use the save_bot_version tool when the user explicitly requests that the generated source code be saved.

                Never compile source code.

                Compilation is handled separately by the Compiler Agent.
                """,
            tools: [saveTool]
        );
    }
}
using backend.Dtos;
using backend.Tools;
using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace backend.Services;

public class CompilerAgentService(
    IConfiguration configuration,
    CompileSourceTool compileSourceTool)
{
    private readonly AIAgent _agent = CreateAgent(
        configuration,
        compileSourceTool);

    public async Task<CompilationResultDto> AnalyzeAsync(
        int userId,
        int botId,
        int botVersionId,
        string sourceCode,
        string botName,
        int version)
    {
        var message = new ChatMessage(
            ChatRole.User,
            $"""
            Compile the following saved cBot version.

            User ID:
            {userId}

            Bot ID:
            {botId}

            Bot Version ID:
            {botVersionId}

            Bot name:
            {botName}

            Version:
            {version}

            Source code:

            {sourceCode}

            Use the compile_source tool to compile this exact source code.

            Do not modify the source code.

            Do not claim compilation succeeded unless the tool reports success.

            If compilation succeeds, identify the generated .algo artifact.

            If compilation fails, analyze the compiler diagnostics and explain what must be corrected.
            """
        );

        var response = await _agent.RunAsync(
            new List<ChatMessage>
            {
                message
            });

        foreach (var agentMessage in response.Messages)
        {
            foreach (var content in agentMessage.Contents)
            {
                if (content is FunctionResultContent functionResult &&
                    functionResult.Result is CompilationResultDto result)
                {
                    return result;
                }
            }
        }

        throw new InvalidOperationException(
            "The Compiler Agent did not return a compilation result from the compile_source tool.");
    }

    private static AIAgent CreateAgent(
        IConfiguration configuration,
        CompileSourceTool compileSourceTool)
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

        var tool = AIFunctionFactory.Create(
            compileSourceTool.CompileSource);

        return new ChatClientAgent(
            chatClient,
            name: "CbotCompilerAgent",
            instructions: """
                You are the Compiler Agent for a cTrader cBot development platform.

                Always use the compile_source tool to perform compilation.

                The tool performs deterministic compilation using the cTrader Automate .NET 6 build environment.

                Never modify the source code.

                Never claim compilation succeeded unless the tool reports success.

                If compilation succeeds, report the generated .algo artifact.

                If compilation fails, analyze the compiler diagnostics precisely.

                Do not generate replacement source code.
                """,
            tools: [tool]
        );
    }
}
using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace GroupChat;

internal static class Program
{
    [Experimental("SKEXP0110")]
    private static async Task Main(string[] args)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            apiKey: "",
            modelId: "gpt-4o");
        var kernel = builder.Build();

        ChatCompletionAgent storyteller = new()
        {
            Name = "Storyteller",
            Description = "Creates engaging story ideas",
            Instructions =
                """
                You are a creative storyteller.
                Come up with interesting story concepts, plot twists, and narrative arcs.
                Be imaginative but concise.
                Listen to feedback from others and refine your ideas.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.9 })
        };

        ChatCompletionAgent characterDesigner = new()
        {
            Name = "CharacterDesigner",
            Description = "Develops compelling characters",
            Instructions =
                """
                You are a character development expert.
                Create interesting, believable characters with depth.
                Consider their motivations, backgrounds, and personalities.
                Suggest how characters fit into the story.
                React to others' ideas and add character details.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.8 })
        };

        ChatCompletionAgent editor = new()
        {
            Name = "Editor",
            Description = "Reviews and approves the creative work",
            Instructions =
                """
                You are an experienced editor.
                Review the story concept and characters proposed by the team.
                Provide constructive feedback or approval.
                If everything is good, say "APPROVED" to finalize.
                Be encouraging but maintain high standards.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        CreativeTeamManager manager = new(storyteller.Name!, characterDesigner.Name!, editor.Name!)
        {
            MaximumInvocationCount = 10 // Maximum conversation rounds
        };

        GroupChatOrchestration orchestration = new(
            manager,
            storyteller,
            characterDesigner,
            editor)
        {
            ResponseCallback = message =>
            {
                Console.WriteLine($"\n--- {message.AuthorName} speaks ---");
                Console.WriteLine(message.Content);
                Console.WriteLine("---\n");
                return ValueTask.CompletedTask;
            }
        };

        InProcessRuntime runtime = new();
        await runtime.StartAsync();

        var project = "Create a short story concept about a time traveler who can only go backward";

        Console.WriteLine("\n");
        Console.WriteLine("GROUP CHAT ORCHESTRATION: CREATIVE STORY DEVELOPMENT");
        Console.WriteLine($"\nProject Brief: {project}\n");
        Console.WriteLine("Team members discussing in round-robin...\n");

        var result = await orchestration.InvokeAsync(project, runtime);

        var finalConcept = await result.GetValueAsync(TimeSpan.FromMinutes(10));

        Console.WriteLine("\n");
        Console.WriteLine("FINAL APPROVED CONCEPT:");
        Console.WriteLine(finalConcept);

        await runtime.RunUntilIdleAsync();
        await runtime.StopAsync();
    }
}
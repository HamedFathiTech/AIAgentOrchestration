using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Concurrent;

internal static class Program
{
    [Experimental("SKEXP0110")]
    static async Task Main(string[] args)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            apiKey: "",
            modelId: "gpt-4o");
        var kernel = builder.Build();

        var destination = "Vienna, Austria";

        ChatCompletionAgent budgetExpert = new()
        {
            Name = "BudgetExpert",
            Description = "Provides cost analysis and budget planning advice for travel destinations",
            Instructions =
                """
                You are a travel budget expert.
                Analyze the destination from a cost perspective:
                - Average daily costs (accommodation, food, transport)
                - Budget tips and money-saving strategies
                - Best value for money options
                Give short practical budget advice for travelers.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        ChatCompletionAgent cultureExpert = new()
        {
            Name = "CultureExpert",
            Description = "Analyzes cultural attractions, local customs, and authentic experiences at travel destinations",
            Instructions =
                """
                You are a cultural travel expert.
                Analyze the destination from a cultural perspective:
                - Must-see cultural attractions and experiences
                - Local customs and etiquette
                - Cultural events and festivals
                - Authentic local experiences
                Help travelers appreciate the local culture is short answer.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        ChatCompletionAgent adventureExpert = new()
        {
            Name = "AdventureExpert",
            Description = "Recommends outdoor activities, adventures, and active experiences available at destinations",
            Instructions =
                """
                You are an adventure travel expert.
                Analyze the destination from an adventure perspective:
                - Outdoor activities and adventures
                - Best hiking, water sports, or extreme activities
                - Natural wonders and scenic spots
                - Physical activity recommendations
                Focus on active and adventurous experiences and share your insights in short answer.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        ChatCompletionAgent foodExpert = new()
        {
            Name = "FoodExpert",
            Description = "Explores culinary experiences, local dishes, and food culture at travel destinations",
            Instructions =
                """
                You are a culinary travel expert.
                Analyze the destination from a food perspective:
                - Must-try local dishes and specialties
                - Best restaurants and street food spots
                - Local markets and food experiences
                - Culinary traditions and food culture
                Help travelers discover the best food experiences in short answer.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        StructuredOutputTransform<TravelAnalysis> outputTransform =
            new(chatCompletionService,
                new OpenAIPromptExecutionSettings { ResponseFormat = typeof(TravelAnalysis) });

        ConcurrentOrchestration<string, TravelAnalysis> orchestration = new(
            budgetExpert,
            cultureExpert,
            adventureExpert,
            foodExpert)
        {
            ResultTransform = outputTransform.TransformAsync,
            ResponseCallback = message =>
            {
                Console.WriteLine($"\n{message.AuthorName} completed their analysis!");
                return ValueTask.CompletedTask;
            }
        };

        InProcessRuntime runtime = new();
        await runtime.StartAsync();

        Console.WriteLine("\n");
        Console.WriteLine("CONCURRENT ORCHESTRATION: TRAVEL DESTINATION ANALYSIS");
        Console.WriteLine($"\nDestination: {destination}\n");
        Console.WriteLine("4 experts analyzing simultaneously...\n");

        var result = await orchestration.InvokeAsync(
            $"Analyze {destination} as a travel destination",
            runtime);

        var analysis = await result.GetValueAsync(TimeSpan.FromSeconds(90));

        await runtime.RunUntilIdleAsync();
        await runtime.StopAsync();
    }

    private sealed class TravelAnalysis
    {
        public List<string> BudgetInsights { get; set; } = [];
        public List<string> CulturalHighlights { get; set; } = [];
        public List<string> AdventureActivities { get; set; } = [];
        public List<string> FoodExperiences { get; set; } = [];
    }
}
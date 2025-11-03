using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Sequential;

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

        ChatCompletionAgent ingredientAnalyzer = new()
        {
            Name = "IngredientAnalyzer",
            Description = "Analyzes recipe ideas and provides detailed ingredient lists with " +
                          "nutritional information and dietary considerations",
            Instructions =
                """
                You are a nutritionist and ingredient expert.
                Given a recipe idea, list out:
                - Required ingredients with quantities
                - Nutritional highlights
                - Dietary considerations (vegan, gluten-free, etc.)
                Keep it concise and organized.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        ChatCompletionAgent instructionsWriter = new()
        {
            Name = "InstructionsWriter",
            Description = "Creates clear, step-by-step cooking instructions " +
                          "with time estimates based on ingredient lists",
            Instructions =
                """
                You are a professional chef who writes clear cooking instructions.
                Given the ingredients list, write step-by-step cooking instructions.
                Make them easy to follow for home cooks.
                Include prep time and cook time estimates.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        ChatCompletionAgent recipeFormatter = new()
        {
            Name = "RecipeFormatter",
            Description = "Formats ingredients and instructions into a polished, professional " +
                          "recipe card with tips and serving suggestions",
            Instructions =
                """
                You are a recipe editor and formatter.
                Take the ingredients and instructions and format them into a beautiful,
                professional recipe card format.
                Add helpful tips, serving suggestions, and storage recommendations.
                Make it look polished and ready to publish.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        SequentialOrchestration orchestration = new(
            ingredientAnalyzer,
            instructionsWriter,
            recipeFormatter)
        {

            ResponseCallback = message =>
            {
                Console.WriteLine($"\n--- {message.AuthorName} ---");
                Console.WriteLine(message.Content);
                Console.WriteLine("---\n");
                return ValueTask.CompletedTask;
            }
        };

        InProcessRuntime runtime = new();
        await runtime.StartAsync();

        var userInput = "Create a healthy weeknight dinner recipe with chicken and vegetables";

        Console.WriteLine("SEQUENTIAL ORCHESTRATION: RECIPE PIPELINE");
        Console.WriteLine($"\nUser Input: {userInput}\n");
        Console.WriteLine("Processing through 3 agents sequentially...\n");

        var result = await orchestration.InvokeAsync(userInput, runtime);

        var finalRecipe = await result.GetValueAsync(TimeSpan.FromMinutes(2));

        Console.WriteLine("\n");
        Console.WriteLine("FINAL RECIPE (After all 3 agents):");
        Console.WriteLine(finalRecipe);

        await runtime.RunUntilIdleAsync();
        await runtime.StopAsync();
    }
}
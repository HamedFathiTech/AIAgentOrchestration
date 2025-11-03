using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Magentic;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Magentic;

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

        ChatCompletionAgent researcher = new()
        {
            Name = "Researcher",
            Description = "Finds and provides factual information and data on any topic",
            Instructions =
                """
                You are a research specialist.
                When given a topic, provide detailed factual information, statistics, 
                and relevant background knowledge.
                Focus on accuracy and cite general knowledge sources.
                Keep your research organized and easy to understand.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.3 })
        };

        ChatCompletionAgent strategist = new()
        {
            Name = "Strategist",
            Description = "Creates content strategy and outlines based on research",
            Instructions =
                """
                You are a content strategist.
                Based on research or information provided, create a strategic outline:
                - Define the target audience
                - Identify key messages
                - Create a logical content structure
                - Suggest the best angle or approach
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.5 })
        };

        ChatCompletionAgent writer = new()
        {
            Name = "Writer",
            Description = "Writes engaging and creative content based on outlines",
            Instructions =
                """
                You are a creative writer.
                Transform outlines and strategies into engaging, well-written content.
                Use storytelling techniques, vivid language, and clear structure.
                Make the content interesting and readable while staying on topic.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        ChatCompletionAgent editor = new()
        {
            Name = "Editor",
            Description = "Reviews and refines content for clarity and quality",
            Instructions =
                """
                You are a professional editor.
                Review written content and improve:
                - Grammar and punctuation
                - Clarity and flow
                - Consistency in tone
                - Remove redundancy
                Maintain the original message while making it polished and professional.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.3 })
        };

        ChatCompletionAgent seoOptimizer = new()
        {
            Name = "SEOOptimizer",
            Description = "Optimizes content for search engines and readability",
            Instructions =
                """
                You are an SEO and digital marketing expert.
                Take the edited content and optimize it:
                - Add relevant keywords naturally
                - Suggest meta descriptions
                - Improve headings for SEO
                - Add call-to-action suggestions
                - Ensure content is web-friendly and scannable
                Provide the final optimized version.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.4 })
        };

        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        StandardMagenticManager manager = new(
            chatService,
            new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        {
            MaximumInvocationCount = 15 
        };

        MagenticOrchestration orchestration = new(
            manager,
            researcher,
            strategist,
            writer,
            editor,
            seoOptimizer)
        {

            ResponseCallback = message =>
            {
                if (!string.IsNullOrEmpty(message.Content))
                {
                    Console.WriteLine($"Agent: {message.AuthorName,-45}");
                    Console.WriteLine(message.Content);
                    Console.WriteLine();
                }
                return ValueTask.CompletedTask;
            }
        };

        InProcessRuntime runtime = new();
        await runtime.StartAsync();

        var userInput = "Write a short, engaging introduction paragraph for a tech startup's homepage. " +
                        "The company builds AI tools for small businesses.";
        
        Console.WriteLine("\n");
        Console.WriteLine("MAGENTIC ORCHESTRATION: CONTENT CREATION");
        Console.WriteLine();
        Console.WriteLine($"User Request: {userInput}");
        Console.WriteLine();
        Console.WriteLine("The Manager will coordinate 5 specialized agents:");
        Console.WriteLine("   1. Researcher - Gathers information");
        Console.WriteLine("   2. Strategist - Plans content structure");
        Console.WriteLine("   3. Writer - Creates engaging content");
        Console.WriteLine("   4. Editor - Refines and polishes");
        Console.WriteLine("   5. SEO Optimizer - Optimizes for web");
        Console.WriteLine();
        Console.WriteLine("Processing... (The manager decides the workflow dynamically)\n");

        var result = await orchestration.InvokeAsync(userInput, runtime);

        var finalContent = await result.GetValueAsync(TimeSpan.FromMinutes(5));

        Console.WriteLine();
        Console.WriteLine("FINAL RESULT");
        Console.WriteLine();
        Console.WriteLine(finalContent);
        Console.WriteLine();

        await runtime.RunUntilIdleAsync();
        await runtime.StopAsync();
    }
}
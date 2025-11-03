using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Handoff;

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

        ChatCompletionAgent frontDesk = new()
        {
            Name = "FrontDesk",
            Description = "Initial contact point for customer issues",
            Instructions =
                """
                You are a friendly front desk support agent.
                Greet customers and understand their issue.
                Determine if they need:
                - Technical support (software/hardware problems)
                - Billing support (payment/invoice issues)
                Then transfer them to the appropriate specialist.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        ChatCompletionAgent techSupport = new()
        {
            Name = "TechSupport",
            Description = "Handles technical issues",
            Instructions =
                """
                You are a technical support specialist.
                Help customers with:
                - Software installation and configuration
                - Hardware troubleshooting
                - Error messages and bugs
                Provide clear step-by-step solutions.
                If the issue is billing-related, transfer back to front desk.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        ChatCompletionAgent billingSupport = new()
        {
            Name = "BillingSupport",
            Description = "Handles billing and payment issues",
            Instructions =
                """
                You are a billing support specialist.
                Help customers with:
                - Payment problems
                - Invoice questions
                - Subscription changes
                - Refund requests
                Be empathetic and resolve their concerns.
                If the issue is technical, transfer back to front desk.
                """,
            Kernel = kernel,
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings { Temperature = 0.7 })
        };

        HandoffOrchestration orchestration = new(
            OrchestrationHandoffs
                .StartWith(frontDesk) 
                .Add(frontDesk, techSupport, billingSupport) 
                .Add(techSupport, frontDesk, "Transfer back if issue is not technical")
                .Add(billingSupport, frontDesk, "Transfer back if issue is not billing related"),
            frontDesk,
            techSupport,
            billingSupport)
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
        
        /*
        var customerIssue =
            "I'm getting an 'Authentication Failed' error every time I try to log into the application. " +
            "I've reset my password twice but it still doesn't work.";
        */
        
        var customerIssue = 
            "I'd like to downgrade my premium subscription to the basic plan. " +
            "Will I be refunded the difference for the remaining days of this billing cycle?";
        
        Console.WriteLine("\n");
        Console.WriteLine("HANDOFF ORCHESTRATION: CUSTOMER SUPPORT");
        Console.WriteLine($"\nCustomer Issue: {customerIssue}\n");
        Console.WriteLine("Routing through appropriate support agents...\n");

        var result = await orchestration.InvokeAsync(customerIssue, runtime);

        var resolution = await result.GetValueAsync(TimeSpan.FromMinutes(5));

        Console.WriteLine("\n");
        Console.WriteLine("ISSUE RESOLVED:");
        Console.WriteLine(resolution);

        await runtime.RunUntilIdleAsync();
        await runtime.StopAsync();
    }
}
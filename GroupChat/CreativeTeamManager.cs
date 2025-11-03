using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

// ReSharper disable ConvertToPrimaryConstructor

namespace GroupChat;

[Experimental("SKEXP0110")]
internal sealed class CreativeTeamManager : RoundRobinGroupChatManager
{
    private readonly string _storytellerName;
    private readonly string _characterDesignerName;
    private readonly string _editorName;

    public CreativeTeamManager(string storytellerName, string characterDesignerName, string editorName)
    {
        _storytellerName = storytellerName;
        _characterDesignerName = characterDesignerName;
        _editorName = editorName;
    }

    public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
        ChatHistory history,
        CancellationToken cancellationToken = default)
    {
        // Checks if MaximumInvocationCount has been reached.
        var result = await base.ShouldTerminate(history, cancellationToken);

        if (!result.Value)
        {
            var lastMessage = history.LastOrDefault();
            if (lastMessage?.AuthorName == _editorName &&
                lastMessage.Content?.Contains("APPROVED", StringComparison.OrdinalIgnoreCase) == true)
            {
                result = new GroupChatManagerResult<bool>(true)
                {
                    Reason = "Editor approved the creative concept"
                };
            }
        }

        return result;
    }

    /*
        [Turn 1] Storyteller: "A robot finds love"
        [Turn 2] CharacterDesigner: "The robot is named R0-B3RT"
        [Turn 3] Storyteller: "R0-B3RT, a lonely maintenance bot, discovers..."
        [Turn 4] Editor: "Needs more emotion"
        [Turn 5] Storyteller: "R0-B3RT's circuits light up when he meets..."
        [Turn 6] CharacterDesigner: "She's an AI named EVA"
        [Turn 7] Storyteller: "FINAL VERSION: R0-B3RT and EVA's forbidden love..."
        [Turn 8] Editor: "APPROVED"
    */
    [Experimental("SKEXP0110")]
    public override ValueTask<GroupChatManagerResult<string>> FilterResults(
        ChatHistory history,
        CancellationToken cancellationToken = default)
    {
        var finalConcept = history
            .Reverse()
            .FirstOrDefault(m => m.AuthorName == _storytellerName);

        var result = finalConcept?.Content ?? "No final concept created";

        return ValueTask.FromResult(
            new GroupChatManagerResult<string>(result)
            {
                Reason = "Final approved story concept"
            });
    }
}
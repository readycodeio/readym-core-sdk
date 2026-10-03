using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.GameEvents;

/// <summary>A game event as the generator sees it: the struct, its one discriminator, and its subject field.</summary>
internal sealed class GameEventModel(INamedTypeSymbol @event, AttributeData discriminator, IFieldSymbol? subject, IReadOnlyList<string> errors)
{
    public INamedTypeSymbol Event { get; } = @event;
    public AttributeData Discriminator { get; } = discriminator;
    public IFieldSymbol? Subject { get; } = subject;
    public IReadOnlyList<string> Errors { get; } = errors;

    public string DiscriminatorName => Discriminator.AttributeClass!.Name;
}

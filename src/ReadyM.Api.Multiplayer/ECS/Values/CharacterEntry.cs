using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Values;

/// <summary>One of a player's characters.</summary>
internal readonly record struct CharacterEntry(CharacterId Id, DateTime CreatedUtc, DateTime LastPlayedUtc);

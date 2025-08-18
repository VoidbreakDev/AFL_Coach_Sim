//File: Assets/Scripts/Adapters/SimAdapters/TeamDirectory.cs
using System;
using System.Collections.Generic;
using System.Linq;
using AFLCoachSim.Core.Data;
using AFLCoachSim.Core.Domain.ValueObjects;

public sealed class TeamDirectory
{
    private readonly Dictionary<int, string> _idToName;
    public TeamDirectory(IEnumerable<TeamConfig> teams)
        => _idToName = teams.ToDictionary(t => t.Id, t => t.Name);

    public string NameOf(TeamId id)
        => _idToName.TryGetValue(id.Value, out var name) ? name : $"Team {id.Value}";
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PlayerTags.Features;

// Shortened names are used only when they identify a single job label.
public sealed class UiNameRules
{
    private readonly Dictionary<string, string?> aliases = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Regex> patterns = new(StringComparer.Ordinal);
    private KeyValuePair<string, string?>[] ordered = [];

    public void Add(string fullName, string jobName)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(jobName)) return;
        AddAlias(fullName, jobName);
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return;
        AddAlias(parts[0][..1] + ". " + parts[1], jobName);
        AddAlias(parts[0] + " " + parts[1][..1] + ".", jobName);
        AddAlias(parts[0][..1] + ". " + parts[1][..1] + ".", jobName);
    }

    private void AddAlias(string name, string label)
    {
        if (aliases.TryGetValue(name, out var previous) && previous != label) aliases[name] = null;
        else if (!aliases.ContainsKey(name)) aliases[name] = label;
    }

    public void Seal() => ordered = aliases.Where(p => p.Value != null).OrderByDescending(p => p.Key.Length).ToArray();

    public void Exclude(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return;
        var excluded = new UiNameRules();
        excluded.Add(fullName, "exclude");
        foreach (var key in excluded.aliases.Keys) aliases[key] = null;
    }

    public string Replace(string text)
    {
        foreach (var (name, label) in ordered)
        {
            if (!text.Contains(name, StringComparison.Ordinal)) continue;
            if (!patterns.TryGetValue(name, out var pattern))
                patterns[name] = pattern = new Regex(@"(?<![\p{L}'-])" + Regex.Escape(name) + @"(?![\p{L}\p{N}'-])", RegexOptions.CultureInvariant);
            // A name field has one player; do not reinterpret a replacement as another name.
            var replaced = pattern.Replace(text, _ => label!);
            if (replaced != text) return replaced;
        }
        return text;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PlayerTags.Configuration;
using PlayerTags.Data;

namespace PlayerTags.Features;

public sealed unsafe class UiJobNamesFeature : IDisposable
{
    private static readonly string[] Addons = ["_PartyList", "Character", "_FocusTargetInfo", "_TargetInfo", "_TargetInfoMainTarget", "_TargetInfoToT", "_AllianceList1", "_AllianceList2"];
    private readonly PluginConfiguration configuration;
    private readonly PluginData data;
    private readonly Dictionary<nint, Dictionary<nint, UiTextSnapshot>> snapshots = [];
    private readonly Dictionary<string, long> changed = [];
    private readonly Dictionary<string, nint> seen = [];
    private UiNameRules players = new();
    private UiNameRules self = new();
    private UiNameRules combinedTarget = new();
    private UiNameRules targetOfTarget = new();
    private UiNameRules primaryTarget = new();
    private long lastRefresh;
    private long lastStatusSave;
    private bool disposing;

    public UiJobNamesFeature(PluginConfiguration configuration, PluginData data)
    {
        this.configuration = configuration;
        this.data = data;
        PluginServices.AddonLifecycle.RegisterListener(AddonEvent.PreDraw, Addons, OnDraw);
        PluginServices.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, Addons, OnFinalize);
        PluginServices.Framework.Update += Refresh;
    }

    public string Diagnostics => string.Join(", ", Addons.Select(n => n + "=" + changed.GetValueOrDefault(n)));

    private void Refresh(Dalamud.Plugin.Services.IFramework _)
    {
        if (!PluginServices.Framework.IsInFrameworkUpdateThread || disposing) return;
        if (Environment.TickCount64 - lastRefresh < 250) return;
        lastRefresh = Environment.TickCount64;
        var all = new UiNameRules();
        var local = new UiNameRules();
        foreach (var player in PluginServices.ObjectTable.OfType<IPlayerCharacter>())
            all.Add(player.Name.TextValue, JobName(player.ClassJob.RowId));
        foreach (var member in PluginServices.PartyList)
            AddMember(all, member);
        if (PluginServices.PartyList.IsAlliance)
            for (var i = 0; i < 20; i++)
            {
                var address = PluginServices.PartyList.GetAllianceMemberAddress(i);
                if (address != nint.Zero) AddMember(all, PluginServices.PartyList.CreateAllianceMemberReference(address));
            }
        var me = PluginServices.ObjectTable.LocalPlayer;
        if (me != null)
        {
            var label = JobName(me.ClassJob.RowId);
            local.Add(me.Name.TextValue, label);
            all.Add(me.Name.TextValue, label);
        }
        var target = PluginServices.TargetManager.Target;
        var secondary = target?.TargetObject as IPlayerCharacter;
        var combined = new UiNameRules();
        var separate = new UiNameRules();
        var primary = new UiNameRules();
        if (target is IPlayerCharacter mainPlayer)
        {
            var label = JobName(mainPlayer.ClassJob.RowId);
            primary.Add(mainPlayer.Name.TextValue, label);
            if (configuration.JobNamesTarget) combined.Add(mainPlayer.Name.TextValue, label);
        }
        if (secondary != null)
        {
            var label = JobName(secondary.ClassJob.RowId);
            if (configuration.JobNamesTargetOfTarget) combined.Add(secondary.Name.TextValue, label);
            separate.Add(secondary.Name.TextValue, label);
        }
        // Keep the two switches independent in the combined HUD layout.
        if (!configuration.JobNamesTarget && target is IPlayerCharacter) combined.Exclude(target.Name.TextValue);
        if (!configuration.JobNamesTargetOfTarget && secondary != null) combined.Exclude(secondary.Name.TextValue);
        primary.Seal(); primaryTarget = primary;
        combined.Seal(); separate.Seal(); combinedTarget = combined; targetOfTarget = separate;
        all.Seal(); local.Seal(); players = all; self = local;
        if (Environment.TickCount64 - lastStatusSave > 2000)
        {
            lastStatusSave = Environment.TickCount64;
            try
            {
                File.WriteAllText(Path.Combine(PluginServices.DalamudPluginInterface.ConfigDirectory.FullName, "ui-name-status.json"),
                    JsonSerializer.Serialize(new { enabled = configuration.EnabledGlobal, pvp = PluginServices.ClientState.IsPvP,
                        seenAddons = seen.Keys.ToArray(), rewrites = changed, updatedUtc = DateTime.UtcNow }));
            }
            catch (IOException) { }
        }
    }

    private void AddMember(UiNameRules rules, IPartyMember? member)
    {
        if (member != null) rules.Add(member.Name.TextValue, JobName(member.ClassJob.RowId));
    }

    private string JobName(uint rowId)
    {
        var english = PluginServices.DataManager.GetExcelSheet<ClassJob>(ClientLanguage.English)?.GetRowOrDefault(rowId);
        if (english == null) return "";
        var abbreviation = english.Value.Abbreviation.ExtractText();
        var localized = PluginServices.DataManager.GetExcelSheet<ClassJob>()?.GetRowOrDefault(rowId);
        var localAbbreviation = localized?.Abbreviation.ExtractText() ?? abbreviation;
        if ((data.JobTags.TryGetValue(localAbbreviation, out var tag) || data.JobTags.TryGetValue(abbreviation, out tag))
            && !string.IsNullOrWhiteSpace(tag.Text.InheritedValue) && tag.Text.InheritedValue != abbreviation && tag.Text.InheritedValue != localAbbreviation)
            return tag.Text.InheritedValue;
        return localized?.Name.ExtractText() ?? english.Value.Name.ExtractText();
    }

    private bool Enabled(string name) => !disposing && configuration.EnabledGlobal && !PluginServices.ClientState.IsPvP && name switch
    {
        "_PartyList" => configuration.JobNamesPartyList,
        "Character" => configuration.JobNamesCharacter,
        "_FocusTargetInfo" => configuration.JobNamesFocusTarget,
        "_TargetInfo" => configuration.JobNamesTarget || configuration.JobNamesTargetOfTarget,
        "_TargetInfoMainTarget" => configuration.JobNamesTarget,
        "_TargetInfoToT" => configuration.JobNamesTargetOfTarget,
        "_AllianceList1" or "_AllianceList2" => configuration.JobNamesAlliance,
        _ => false,
    };

    private void OnDraw(AddonEvent _, AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;
        seen[args.AddonName] = (nint)addon;
        Process(addon, args.AddonName, Enabled(args.AddonName));
    }

    private void Process(AtkUnitBase* addon, string name, bool enabled)
    {
        if (!snapshots.TryGetValue((nint)addon, out var saved)) snapshots[(nint)addon] = saved = [];
        var visited = new HashSet<nint>();
        Walk(&addon->UldManager, name, enabled, saved, visited, 0);
        foreach (var stale in saved.Keys.Where(k => !visited.Contains(k)).ToArray()) saved.Remove(stale);
    }

    private void Walk(AtkUldManager* manager, string name, bool enabled, Dictionary<nint, UiTextSnapshot> saved, HashSet<nint> visited, int depth)
    {
        if (manager == null || depth > 8 || manager->NodeListCount > 4096) return;
        for (var i = 0; i < manager->NodeListCount && visited.Count < 4096; i++)
        {
            var node = manager->NodeList[i];
            if (node == null || !visited.Add((nint)node)) continue;
            if (node->Type == NodeType.Text) Rewrite((AtkTextNode*)node, name, enabled, saved);
            else if ((int)node->Type >= 1000)
            {
                var component = ((AtkComponentNode*)node)->Component;
                if (component != null) Walk(&component->UldManager, name, enabled, saved, visited, depth + 1);
            }
        }
    }

    private void Rewrite(AtkTextNode* node, string addonName, bool enabled, Dictionary<nint, UiTextSnapshot> saved)
    {
        var current = UiTextSnapshot.TextBytes(node->NodeText.AsSpan());
        if (current.Length == 0 || current.Length > 1024) return;
        saved.TryGetValue((nint)node, out var old);
        var stillOurs = old != null && old.StillOurs(current);
        var original = old?.SourceFor(current) ?? current.ToArray();
        if (!enabled)
        {
            if (stillOurs) node->SetText(original);
            saved.Remove((nint)node);
            return;
        }
        var parsed = SeString.Parse(original);
        var payloads = new List<Payload>();
        var any = false;
        var rules = addonName switch { "Character" => self, "_TargetInfo" => combinedTarget, "_TargetInfoMainTarget" => primaryTarget, "_TargetInfoToT" => targetOfTarget, _ => players };
        foreach (var payload in parsed.Payloads)
        {
            if (payload is TextPayload text)
            {
                var replacement = rules.Replace(text.Text);
                any |= replacement != text.Text;
                payloads.Add(replacement == text.Text ? payload : new TextPayload(replacement));
            }
            else payloads.Add(payload);
        }
        if (!any)
        {
            if (stillOurs) node->SetText(original);
            saved.Remove((nint)node);
            return;
        }
        var encoded = new SeString(payloads).Encode();
        var length = encoded.Length;
        while (length > 0 && encoded[length - 1] == 0) length--;
        var written = encoded.AsSpan(0, length).ToArray();
        if (!current.SequenceEqual(written))
        {
            node->SetText(written);
            changed[addonName] = changed.GetValueOrDefault(addonName) + 1;
        }
        saved[(nint)node] = new UiTextSnapshot(original, UiTextSnapshot.TextBytes(node->NodeText.AsSpan()).ToArray());
    }

    private void OnFinalize(AddonEvent _, AddonArgs args)
    {
        snapshots.Remove(args.Addon.Address);
        seen.Remove(args.AddonName);
    }

    public void Dispose()
    {
        disposing = true;
        void Cleanup()
        {
            foreach (var (name, address) in seen.ToArray())
            {
                var live = PluginServices.GameGui.GetAddonByName(name);
                if (live.Address == address) Process((AtkUnitBase*)address, name, false);
            }
            PluginServices.AddonLifecycle.UnregisterListener(AddonEvent.PreDraw, Addons, OnDraw);
            PluginServices.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, Addons, OnFinalize);
            PluginServices.Framework.Update -= Refresh;
            snapshots.Clear(); seen.Clear();
        }
        if (PluginServices.Framework.IsInFrameworkUpdateThread) Cleanup();
        else PluginServices.Framework.RunOnFrameworkThread(Cleanup).GetAwaiter().GetResult();
    }
}

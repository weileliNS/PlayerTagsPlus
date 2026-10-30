using Dalamud.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Pilz.Dalamud.ActivityContexts;
using Pilz.Dalamud.Tools.NamePlates;
using PlayerTags.Data;
using PlayerTags.Inheritables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayerTags.Configuration;

[Serializable]
public class PluginConfiguration : IPluginConfiguration
{
    private const int DEFAULT_CONFIG_VERSION = 1;

    [JsonProperty]
    public int RootVersion { get; private set; } = DEFAULT_CONFIG_VERSION;
    public int Version { get; set; } = DEFAULT_CONFIG_VERSION;
    public bool IsVisible = false;
    public bool EnabledGlobal = true;

    [JsonProperty("GeneralOptionsV2")]
    public Dictionary<ActivityType, GeneralOptionsClass> GeneralOptions = new()
    {
        { ActivityType.None, new GeneralOptionsClass() },
        { ActivityType.PveDuty, new GeneralOptionsClass() },
        { ActivityType.PvpDuty, new GeneralOptionsClass() }
    };

    public DefaultPluginDataTemplate DefaultPluginDataTemplate = DefaultPluginDataTemplate.None;
    public StatusIconPriorizerSettings StatusIconPriorizerSettings = new(true);
    public bool MoveStatusIconToNameplateTextIfPossible = true;
    public bool IsPlayerNameRandomlyGenerated = false;
    public bool IsCustomTagsContextMenuEnabled = true;
    public bool IsShowInheritedPropertiesEnabled = true;
    public bool IsPlayersTabOrderedByProximity = false;
    public bool IsPlayersTabSelfVisible = true;
    public bool IsPlayersTabFriendsVisible = true;
    public bool IsPlayersTabPartyVisible = true;
    public bool IsPlayersTabAllianceVisible = true;
    public bool IsPlayersTabEnemiesVisible = true;
    public bool IsPlayersTabOthersVisible = false;
    public bool IsGeneralOptionsAllTheSameEnabled = true;
    public bool JobNamesPartyList = true;
    public bool JobNamesCharacter = true;
    public bool JobNamesFocusTarget = true;
    public bool JobNamesTargetOfTarget = true;
    public bool JobNamesTarget = true;
    public bool JobNamesAlliance = true;

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public Dictionary<string, InheritableData> AllTagsChanges = [];

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public Dictionary<string, InheritableData> AllRoleTagsChanges = [];

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public Dictionary<Role, Dictionary<string, InheritableData>> RoleTagsChanges = [];

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public Dictionary<DpsRole, Dictionary<string, InheritableData>> DpsRoleTagsChanges = [];

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public Dictionary<RangedDpsRole, Dictionary<string, InheritableData>> RangedDpsRoleTagsChanges = [];

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public Dictionary<LandHandRole, Dictionary<string, InheritableData>> LandHandRoleTagsChanges = [];

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public Dictionary<string, Dictionary<string, InheritableData>> JobTagsChanges = [];

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public Dictionary<string, InheritableData> AllCustomTagsChanges = [];

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public List<Dictionary<string, InheritableData>> CustomTagsChanges = [];

    [JsonProperty(TypeNameHandling = TypeNameHandling.None, ItemTypeNameHandling = TypeNameHandling.None)]
    public List<Identity> Identities = [];

    #region Obsulate Properties

    [Obsolete]
    [JsonProperty("GeneralOptions")]
    private Dictionary<Data.ActivityContext, GeneralOptionsClass> GeneralOptionsV1
    {
        set
        {
            GeneralOptions.Clear();
            foreach (var kvp in value)
                GeneralOptions.Add((ActivityType)kvp.Key, kvp.Value);
        }
    }

    [JsonProperty("NameplateFreeCompanyVisibility"), Obsolete]
    private NameplateFreeCompanyVisibility NameplateFreeCompanyVisibilityV1
    {
        set
        {
            foreach (var key in GeneralOptions.Keys)
                GeneralOptions[key].NameplateFreeCompanyVisibility = value;
        }
    }
    [JsonProperty("NameplateTitleVisibility"), Obsolete]
    public NameplateTitleVisibility NameplateTitleVisibilityV1
    {
        set
        {
            foreach (var key in GeneralOptions.Keys)
                GeneralOptions[key].NameplateTitleVisibility = value;
        }
    }
    [JsonProperty("NameplateTitlePosition"), Obsolete]
    public NameplateTitlePosition NameplateTitlePositionV1
    {
        set
        {
            foreach (var key in GeneralOptions.Keys)
                GeneralOptions[key].NameplateTitlePosition = value;
        }
    }

    [JsonProperty("IsApplyTagsToAllChatMessagesEnabled"), Obsolete]
    private bool IsApplyTagsToAllChatMessagesEnabledV1
    {
        set
        {
            foreach (var key in GeneralOptions.Keys)
                GeneralOptions[key].IsApplyTagsToAllChatMessagesEnabled = value;
        }
    }

    #endregion

    public event System.Action? Saved;

    public void Save(PluginData pluginData)
    {
        AllTagsChanges = pluginData.AllTags.GetChanges(pluginData.Default.AllTags.GetChanges());
        AllRoleTagsChanges = pluginData.AllRoleTags.GetChanges(pluginData.Default.AllRoleTags.GetChanges());

        RoleTagsChanges = [];
        foreach ((var role, var roleTag) in pluginData.RoleTags)
        {
            Dictionary<string, InheritableData>? defaultChanges = [];
            if (pluginData.Default.RoleTags.TryGetValue(role, out var defaultTag))
            {
                defaultChanges = defaultTag.GetChanges();
            }

            var changes = roleTag.GetChanges(defaultChanges);
            if (changes.Any())
            {
                RoleTagsChanges[role] = changes;
            }
        }

        DpsRoleTagsChanges = [];
        foreach ((var dpsRole, var dpsRoleTag) in pluginData.DpsRoleTags)
        {
            Dictionary<string, InheritableData>? defaultChanges = [];
            if (pluginData.Default.DpsRoleTags.TryGetValue(dpsRole, out var defaultTag))
            {
                defaultChanges = defaultTag.GetChanges();
            }

            var changes = dpsRoleTag.GetChanges(defaultChanges);
            if (changes.Any())
            {
                DpsRoleTagsChanges[dpsRole] = changes;
            }
        }

        RangedDpsRoleTagsChanges = [];
        foreach ((var rangedDpsRole, var rangedDpsRoleTag) in pluginData.RangedDpsRoleTags)
        {
            Dictionary<string, InheritableData>? defaultChanges = [];
            if (pluginData.Default.RangedDpsRoleTags.TryGetValue(rangedDpsRole, out var defaultTag))
            {
                defaultChanges = defaultTag.GetChanges();
            }

            var changes = rangedDpsRoleTag.GetChanges(defaultChanges);
            if (changes.Any())
            {
                RangedDpsRoleTagsChanges[rangedDpsRole] = changes;
            }
        }

        LandHandRoleTagsChanges = [];
        foreach ((var landHandRole, var landHandRoleTag) in pluginData.LandHandRoleTags)
        {
            Dictionary<string, InheritableData>? defaultChanges = [];
            if (pluginData.Default.LandHandRoleTags.TryGetValue(landHandRole, out var defaultTag))
            {
                defaultChanges = defaultTag.GetChanges();
            }

            var changes = landHandRoleTag.GetChanges(defaultChanges);
            if (changes.Any())
            {
                LandHandRoleTagsChanges[landHandRole] = changes;
            }
        }

        JobTagsChanges = [];
        foreach ((var jobAbbreviation, var jobTag) in pluginData.JobTags)
        {
            Dictionary<string, InheritableData>? defaultChanges = [];
            if (pluginData.Default.JobTags.TryGetValue(jobAbbreviation, out var defaultTag))
            {
                defaultChanges = defaultTag.GetChanges();
            }

            var changes = jobTag.GetChanges(defaultChanges);
            if (changes.Any())
            {
                JobTagsChanges[jobAbbreviation] = changes;
            }
        }

        AllCustomTagsChanges = pluginData.AllCustomTags.GetChanges(pluginData.Default.AllCustomTags.GetChanges());

        CustomTagsChanges = [];
        foreach (var customTag in pluginData.CustomTags)
        {
            CustomTagsChanges.Add(customTag.GetChanges());
        }

        Identities = pluginData.Identities;

        SavePluginConfig();

        Saved?.Invoke();
    }

    private void SavePluginConfig()
    {
        Version = DEFAULT_CONFIG_VERSION;
        var configFilePath = GetConfigFilePath();
        var configFileContent = JsonConvert.SerializeObject(this, Formatting.Indented, GetJsonSettings());
        File.WriteAllText(configFilePath, configFileContent);
    }

    private static void BackupPluginConfig()
    {
        var configFilePath = GetConfigFilePath();
        var configFilePathOld = Path.ChangeExtension(configFilePath, ".old" + Path.GetExtension(configFilePath));
        File.Copy(configFilePath, configFilePathOld, true);
    }

    public static PluginConfiguration LoadPluginConfig()
    {
        var configFilePath = GetConfigFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(configFilePath)!);
        if (!File.Exists(configFilePath))
        {
            var parent = Directory.GetParent(PluginServices.DalamudPluginInterface.ConfigDirectory.FullName);
            var original = Path.Combine(parent!.FullName, "PlayerTags", "Config.json");
            if (File.Exists(original)) File.Copy(original, configFilePath, false);
        }
        object config = null;

        if (File.Exists(configFilePath))
        {
            var configFileContent = File.ReadAllText(configFilePath);
            config = JsonConvert.DeserializeObject<PluginConfiguration>(configFileContent, GetJsonSettings());
        }
        else
        {
            // Try loading the old settings, if possible
            configFilePath = PluginServices.DalamudPluginInterface.ConfigFile.FullName;
            config = PluginServices.DalamudPluginInterface.GetPluginConfig();
        }

        if (config is PluginConfiguration pluginConfig)
        {
            if (PluginConfigFix(pluginConfig))
            {
                BackupPluginConfig();
                pluginConfig.SavePluginConfig();
            }
        }

        return config as PluginConfiguration;
    }

    private static string GetConfigFilePath()
    {
        return Path.Combine(PluginServices.DalamudPluginInterface.ConfigDirectory.FullName, "Config.json");
    }

    private static JsonSerializerSettings GetJsonSettings()
    {
        var jsonSettings = new JsonSerializerSettings
        {
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.Auto,
        };

        jsonSettings.Converters.Add(new StringEnumConverter());

        return jsonSettings;
    }

    private static bool PluginConfigFix(PluginConfiguration config)
    {
        bool hasFixes = false;

        // Patch 6.4 - Disable all Job & Role specific colors & prefix
        // Not used yet, but keeping it there, just for the case,
        //if (config.Version <= 1)
        //{
        //    void fixTags(Dictionary<string, InheritableData> dic)
        //    {
        //        foreach (var change in config.AllRoleTagsChanges.ToArray())
        //        {
        //            var key = change.Key;
        //            if (key == nameof(Tag.IsTextVisibleInChat) ||
        //                key == nameof(Tag.IsTextVisibleInNameplates) ||
        //                key == nameof(Tag.IsRoleIconVisibleInChat) ||
        //                key == nameof(Tag.IsRoleIconVisibleInNameplates) ||
        //                key == nameof(Tag.IsTextColorAppliedToNameplateName) ||
        //                key == nameof(Tag.IsTextColorAppliedToChatName) ||
        //                key == nameof(Tag.IsJobIconVisibleInNameplates))
        //            {
        //                var data = change.Value;
        //                data.Behavior = InheritableBehavior.Disabled;
        //            }
        //        }
        //    }

        //    // "All Roles" tag changes
        //    fixTags(config.AllRoleTagsChanges);

        //    // Role tags changes
        //    foreach (var kvp in config.RoleTagsChanges)
        //        fixTags(kvp.Value);

        //    // Job tags changes
        //    foreach (var kvp in config.JobTagsChanges)
        //        fixTags(kvp.Value);

        //    hasFixes = true;
        //}

        return hasFixes;
    }
}

public class GeneralOptionsClass
{
    public NameplateFreeCompanyVisibility NameplateFreeCompanyVisibility = NameplateFreeCompanyVisibility.Default;
    public NameplateTitleVisibility NameplateTitleVisibility = NameplateTitleVisibility.WhenHasTags;
    public NameplateTitlePosition NameplateTitlePosition = NameplateTitlePosition.AlwaysAboveName;
    public DeadPlayerHandling NameplateDeadPlayerHandling = DeadPlayerHandling.Include;
    public bool IsApplyTagsToAllChatMessagesEnabled = true;
}

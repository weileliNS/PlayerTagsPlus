using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace PlayerTags;

public class PluginServices
{
    [PluginService] public static IDalamudPluginInterface DalamudPluginInterface { get; set; }
    [PluginService] public static IPluginLog PluginLog { get; set; }
    [PluginService] public static IGameConfig GameConfig { get; set; }
    [PluginService] public static IChatGui ChatGui { get; set; }
    [PluginService] public static ICommandManager CommandManager { get; set; }
    [PluginService] public static IDataManager DataManager { get; set; }
    [PluginService] public static IObjectTable ObjectTable { get; set; }
    [PluginService] public static IPartyList PartyList { get; set; }
    [PluginService] public static IContextMenu ContextMenu { get; set; }
    [PluginService] public static INamePlateGui NamePlateGui { get; set; }

    public static void Initialize(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<PluginServices>();
    }
}

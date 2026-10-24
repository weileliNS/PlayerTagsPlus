using Dalamud.Game.Command;
using Dalamud.Plugin;
using PlayerTags.Configuration;
using PlayerTags.Data;
using PlayerTags.Features;

namespace PlayerTags;

public sealed class Plugin : IDalamudPlugin
{
    private const string c_CommandName = "/playertagsplus";
    private const string c_SubCommandName_EnableGlobal = "enableglobal";
    private const string c_CommandArg_On = "on";
    private const string c_CommandArg_Off = "off";
    private const string c_CommandArg_toggle = "toggle";

    private readonly PluginConfiguration pluginConfiguration = null;
    private readonly PluginData pluginData = null;
    private readonly PluginConfigurationUI pluginConfigurationUI = null;

    private readonly CustomTagsContextMenuFeature customTagsContextMenuFeature;
    private readonly NameplateTagTargetFeature nameplatesTagTargetFeature;
    private readonly ChatTagTargetFeature chatTagTargetFeature;
    private readonly UiJobNamesFeature uiJobNamesFeature;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginServices.Initialize(pluginInterface);
        Pilz.Dalamud.PluginServices.Initialize(pluginInterface);

        pluginConfiguration = PluginConfiguration.LoadPluginConfig() ?? new();
        pluginData = new PluginData(pluginConfiguration);
        pluginConfigurationUI = new PluginConfigurationUI(pluginConfiguration, pluginData);

        Localizer.SetLanguage(PluginServices.DalamudPluginInterface.UiLanguage);
        PluginServices.DalamudPluginInterface.LanguageChanged += DalamudPluginInterface_LanguageChanged;

        PluginServices.DalamudPluginInterface.UiBuilder.Draw += UiBuilder_Draw;
        PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi += UiBuilder_OpenConfigUi;
        PluginServices.CommandManager.AddHandler(c_CommandName, new CommandInfo(CommandManager_Handler)
        {
            HelpMessage = Resources.Strings.Loc_Command_playertags_v2
        });
        customTagsContextMenuFeature = new CustomTagsContextMenuFeature(pluginConfiguration, pluginData);
        nameplatesTagTargetFeature = new NameplateTagTargetFeature(pluginConfiguration, pluginData);
        chatTagTargetFeature = new ChatTagTargetFeature(pluginConfiguration, pluginData);
        uiJobNamesFeature = new UiJobNamesFeature(pluginConfiguration, pluginData);
        PluginServices.DalamudPluginInterface.UiBuilder.OpenMainUi += UiBuilder_OpenConfigUi;
    }

    public void Dispose()
    {
        uiJobNamesFeature.Dispose();
        PluginServices.DalamudPluginInterface.UiBuilder.OpenMainUi -= UiBuilder_OpenConfigUi;
        chatTagTargetFeature.Dispose();
        nameplatesTagTargetFeature.Dispose();
        customTagsContextMenuFeature.Dispose();
        PluginServices.DalamudPluginInterface.LanguageChanged -= DalamudPluginInterface_LanguageChanged;
        PluginServices.CommandManager.RemoveHandler(c_CommandName);
        PluginServices.DalamudPluginInterface.UiBuilder.OpenConfigUi -= UiBuilder_OpenConfigUi;
        PluginServices.DalamudPluginInterface.UiBuilder.Draw -= UiBuilder_Draw;
    }

    private void DalamudPluginInterface_LanguageChanged(string langCode)
    {
        Localizer.SetLanguage(langCode);
    }

    private void CommandManager_Handler(string command, string arguments)
    {
        switch (command)
        {
            case c_CommandName:
                if (string.IsNullOrWhiteSpace(arguments))
                    UiBuilder_OpenConfigUi();
                else
                {
                    var lowerArgs = arguments.ToLower().Split(' ');
                    if (lowerArgs[0] == "diagnostics")
                    {
                        PluginServices.ChatGui.Print("[Player Tags Plus] " + uiJobNamesFeature.Diagnostics);
                        return;
                    }
                    if (lowerArgs.Length >= 1)
                    {
                        switch (lowerArgs[0])
                        {
                            case c_SubCommandName_EnableGlobal:
                                if (lowerArgs.Length >= 2)
                                {
                                    switch (lowerArgs[1])
                                    {
                                        case c_CommandArg_On:
                                            pluginConfiguration.EnabledGlobal = true;
                                            break;
                                        case c_CommandArg_Off:
                                            pluginConfiguration.EnabledGlobal = false;
                                            break;
                                        case c_CommandArg_toggle:
                                            pluginConfiguration.EnabledGlobal = !pluginConfiguration.EnabledGlobal;
                                            break;
                                    }
                                }
                                break;
                        }
                    }
                }
                break;
        }
    }

    private void UiBuilder_Draw()
    {
        if (pluginConfiguration.IsVisible)
            pluginConfigurationUI.Draw();
    }

    private void UiBuilder_OpenConfigUi()
    {
        pluginConfiguration.IsVisible = true;
        pluginConfiguration.Save(pluginData);
    }
}

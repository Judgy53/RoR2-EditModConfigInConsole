using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EditModConfigInConsole
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [R2APISubmoduleDependency(nameof(CommandHelper))]

    public class EditModConfigInConsolePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Judgy";
        public const string PluginName = "EditModConfigInConsole";
        public const string PluginVersion = "1.0.0";

        private readonly static StringBuilder strBuilder = new StringBuilder();

        public void Awake()
        {
            Log.Init(Logger);

            R2API.Utils.CommandHelper.AddToConsoleWhenReady();

            Log.LogInfo(nameof(Awake) + " done.");
        }

        private static void EditEntry(string modName, string entrySection, string entryKey, List<string> newValueList)
        {
            if (Chainloader.PluginInfos.TryGetValue(modName, out PluginInfo modInfo))
            {
                if (!string.IsNullOrWhiteSpace(entrySection) || !string.IsNullOrWhiteSpace(entryKey))
                {
                    ConfigFile config = modInfo.Instance.Config;
                    ConfigDefinition entryDef = config.Keys.FirstOrDefault(def => def.Section == entrySection && def.Key == entryKey);
                    if (entryDef != null)
                    {
                        ConfigEntryBase entry = config[entryDef];
                        string newValueStr = GetNewValue(entry, newValueList);
                        object newValue = ConvertNewValue(entry, newValueStr);
                        if (newValue != null)
                        {
                            entry.BoxedValue = newValue;
                            Debug.Log($"<color=#00ff00>Successfully set \"{modName}[{entrySection}/{entryKey}]\" to value '{Utils.GetConfigEntryValueToString(entry)}'</color>");
                        }
                        else
                            Debug.LogError($"New Value \"{newValueStr}\" is invalid");
                    }
                    else
                        Debug.LogError($"Couldn't find entry \"{entrySection}/{entryKey}\" in mod \"{modName}\".");
                }
                else
                    Utils.LogConfigFileToConsole(modName, modInfo.Instance.Config, strBuilder);
            }
            else
                Debug.LogError($"Couldn't find mod named \"{modName}\".");
        }

        private static string GetNewValue(ConfigEntryBase entry, List<string> newValueList)
        {
            if (newValueList == null || newValueList.Count == 0)
                return null;

            string currentValueStr = Utils.GetConfigEntryValueToString(entry);
            string newValueStr = newValueList[0];
            if (newValueList.Count > 1)
            {
                for (int i = 0; i < newValueList.Count; i++)
                {
                    if (currentValueStr == newValueList[i])
                        newValueStr = newValueList[(i + 1) % newValueList.Count];
                }
            }

            return newValueStr;
        }

        private static object ConvertNewValue(ConfigEntryBase entry, string newValueStr)
        {
            if (newValueStr == null)
                return null;

            Type entryValueType = Utils.GetConfigEntryValueType(entry);
            try
            {
                object newValue = TomlTypeConverter.ConvertToValue(newValueStr, entryValueType);
                return newValue;
            }
            catch
            {
                Debug.LogError($"Couldn't convert new value \"{newValueStr}\" to Type \"{entryValueType}\".");
            }

            return null;
        }        

        [ConCommand(commandName = "editmodconfig", flags = ConVarFlags.None, helpText = "Edit another mod config entry. args[0]=(string)modName, args[1]=(string)entrySection, args[2]=(string)entryKey, args[3]=(string)newValue")]
        private static void CommandEditModConfig(ConCommandArgs args)
        {
            string modName = args.TryGetArgString(0);
            string entrySection = args.TryGetArgString(1);
            string entryKey = args.TryGetArgString(2);
            string newValueRaw = args.TryGetArgString(3);

            if (!string.IsNullOrWhiteSpace(modName))
                EditEntry(modName, entrySection, entryKey, new List<string>() { newValueRaw });
            else
                Utils.LogModListToConsole(strBuilder);
        }

        [ConCommand(commandName = "editmodconfig_toggle", flags = ConVarFlags.None, helpText = "Toggle another mod config entry between multiple values. args[0]=(string)modName, args[1]=(string)entrySection, args[2]=(string)entryKey, args[3]=(string)newValue1, args[4]=(string)newValue2, args[5]=(string)newValue3, ...")]
        private static void CommandEditModConfigToggle(ConCommandArgs args)
        {
            string modName = args.TryGetArgString(0);
            string entrySection = args.TryGetArgString(1);
            string entryKey = args.TryGetArgString(2);
            List<string> newValueList = args.userArgs.Skip(3).ToList();

            if (!string.IsNullOrWhiteSpace(modName))
                EditEntry(modName, entrySection, entryKey, newValueList);
            else
                Utils.LogModListToConsole(strBuilder);
        }
    }
}
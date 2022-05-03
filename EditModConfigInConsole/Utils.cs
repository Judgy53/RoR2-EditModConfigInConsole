using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EditModConfigInConsole
{
    public static class Utils
    {
        internal static string GetConfigEntryValueToString(ConfigEntryBase entry, int maxLength = int.MaxValue)
        {
            string serialized = entry.GetSerializedValue();
            int targetLength = Math.Max(0, Math.Min(serialized.Length, maxLength));
            if (serialized.Length > targetLength)
                serialized = entry.GetSerializedValue().Substring(0, targetLength) + "...";

            return serialized;
        }

        internal static Type GetConfigEntryValueType(ConfigEntryBase entry)
        {
            Type entryType = entry.GetType();
            if (entryType.GetGenericTypeDefinition() == typeof(ConfigEntry<>))
                return entryType.GetGenericArguments().FirstOrDefault();

            return typeof(object);
        }

        internal static void LogModListToConsole(StringBuilder strBuilder)
        {
            var validPlugins = Chainloader.PluginInfos.Where(entry => entry.Value.Instance.Config != null && entry.Value.Instance.Config.Count > 0);
            if (validPlugins.Count() > 0)
            {
                strBuilder.Clear();
                strBuilder.AppendLine($"{validPlugins.Count()} mod(s) found :");
                string modIndent = new string(' ', 4);
                foreach (KeyValuePair<string, PluginInfo> entry in validPlugins)
                {
                    string readonlyTag = entry.Value.Instance.Config.IsReadOnly ? " WARNING: Config set to readonly." : "";
                    strBuilder.AppendLine($"{modIndent}{entry.Key} : {entry.Value.Instance.Config.Count} entries.{readonlyTag}");
                }

                Debug.Log(strBuilder.ToString().TrimEnd('\r', '\n'));
                strBuilder.Clear();
            }
            else
                Debug.LogWarning("No mods with editable config entries found.");
        }

        internal static void LogConfigFileToConsole(string modName, ConfigFile config, StringBuilder strBuilder)
        {
            if (config.Count > 0)
            {
                strBuilder.Clear();
                strBuilder.AppendLine($"\"{modName}\" has {config.Count} entries :");
                var groupedBySection = config.Keys.GroupBy(key => key.Section);
                foreach (IGrouping<string, ConfigDefinition> sectionGroup in groupedBySection)
                {
                    WriteConfigSectionToStringbuilder(config, sectionGroup.Key, sectionGroup, strBuilder);
                }

                Debug.Log(strBuilder.ToString().TrimEnd('\r', '\n'));
                strBuilder.Clear();
            }
            else
                Debug.LogWarning($"Mod \"{modName}\" doesn't have any config entries.");
        }

        internal static void WriteConfigSectionToStringbuilder(ConfigFile config, string sectionName, IEnumerable<ConfigDefinition> keys, StringBuilder strBuilder, int baseIndentLevel = 1)
        {
            string sectionIndent = new string(' ', baseIndentLevel * 4);
            string keyIndent = new string(' ', (baseIndentLevel + 1) * 4);

            strBuilder.AppendLine($"{sectionIndent}<color=#C0C0C0>Section: {sectionName}</color>");
            foreach (ConfigDefinition entryDef in keys)
            {
                ConfigEntryBase entry = config[entryDef];
                string entryType = GetConfigEntryValueType(entry).Name;
                string entryValue = GetConfigEntryValueToString(entry, 25);
                strBuilder.AppendLine($"{keyIndent}Key: {entryDef.Key} = '{entryValue}' [{entryType}]");
            }
        }
    }
}

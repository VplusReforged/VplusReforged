using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ValheimPlus.GameClasses;
using ValheimPlus.RPC;
using YamlDotNet.Core.Tokens;
using static CharacterDrop;

namespace ValheimPlus.Configurations
{
    public interface IConfig
    {
        void LoadIniData(KeyDataCollection data, string section);
    }

    public abstract class BaseConfig<T> : IConfig where T : class, IConfig, new()
    {

        public string ServerSerializeSection()
        {
            if (!IsEnabled || !NeedsServerSync) return "";

            var r = "";

            foreach (var prop in typeof(T).GetProperties())
            {
                r += $"{prop.Name}={prop.GetValue(this, null)}|";
            }
            return r;
        }
        [LoadingOption(LoadingMode.Never)]
        public bool IsEnabled { get; private set; } = false;
        [LoadingOption(LoadingMode.Never)]
        public virtual bool NeedsServerSync { get; set; } = false;

        public static IniData iniUpdated = null;

        public static T LoadIni(IniData data, string section)
        {
            var n = new T();


            Debug.Log($"Loading config section {section}");
            if (data[section] == null || data[section]["enabled"] == null || !data[section].GetBool("enabled"))
            {
                Debug.Log(" Section not enabled");
                return n;
            }
            var keyData = data[section];
            n.LoadIniData(keyData, section);

            return n;
        }
        private static Dictionary<Type, DGetDataValue> _getValues = new Dictionary<Type, DGetDataValue>()
        {
            {typeof(float), GetFloatValue },
            {typeof(int), GetIntValue },
            {typeof(KeyCode), GetKeyCodeValue },
            {typeof(bool), GetBoolValue }
        };

        public void LoadIniData(KeyDataCollection data, string section)
        {
            IsEnabled = true;
            var thisConfiguration = GetCurrentConfiguration(section);
            if (thisConfiguration == null)
            {
                Debug.Log("Configuration not set.");
                thisConfiguration = this as T;
                if (thisConfiguration == null) Debug.Log("Error on setting Configuration");
            }

            foreach (var property in typeof(T).GetProperties())
            {


                if (IgnoreLoading(property))
                {
                    continue;
                }
                var currentValue = property.GetValue(thisConfiguration);
                if (LoadLocalOnly(property))
                {
                    property.SetValue(this, currentValue, null);
                    continue;
                }

                var keyName = GetKeyNameFromProperty(property);

                if (!data.ContainsKey(keyName))
                {
                    Debug.Log($" Key {keyName} not defined, using default value");
                    continue;
                }

                Debug.Log($"{property.Name} [{keyName}] = {currentValue} ({property.PropertyType})");

                if (_getValues.ContainsKey(property.PropertyType))
                {
                    var getValue = _getValues[property.PropertyType];
                    var value = getValue(data, currentValue, keyName);
                    Debug.Log($"{keyName} = {currentValue} => {value}");
                    property.SetValue(this, value, null);
                }
                else Debug.LogWarning($" Could not load data of type {property.PropertyType} for key {keyName}");
            }
        }

        delegate object DGetDataValue(KeyDataCollection data, object currentValue, string keyName);

        private static object GetFloatValue(KeyDataCollection data, object currentValue, string keyName)
        {
            return data.GetFloat(keyName, (float)currentValue);
        }
        private static object GetBoolValue(KeyDataCollection data, object currentValue, string keyName)
        {
            return data.GetBool(keyName);
        }
        private static object GetIntValue(KeyDataCollection data, object currentValue, string keyName)
        {
            return data.GetInt(keyName, (int)currentValue);
        }
        private static object GetKeyCodeValue(KeyDataCollection data, object currentValue, string keyName)
        {
            return data.GetKeyCode(keyName, (KeyCode)currentValue);
        }

        private string GetKeyNameFromProperty(PropertyInfo property)
        {
            var keyName = property.Name;

            // Set first char of keyName to lowercase
            if (keyName != string.Empty && char.IsUpper(keyName[0]))
            {
                keyName = char.ToLower(keyName[0]) + keyName.Substring(1);
            }
            return keyName;
        }
        private bool IgnoreLoading(PropertyInfo property)
        {
            var loadingOption = property.GetCustomAttribute<LoadingOption>();
            var loadingMode = loadingOption?.LoadingMode ?? LoadingMode.Always;

            return (loadingMode == LoadingMode.Never);
        }
        private bool LoadLocalOnly(PropertyInfo property)
        {
            var loadingOption = property.GetCustomAttribute<LoadingOption>();
            var loadingMode = loadingOption?.LoadingMode ?? LoadingMode.Always;

            return VPlusConfigSync.SyncRemote && (property.PropertyType == typeof(KeyCode) && !ConfigurationExtra.SyncHotkeys || loadingMode == LoadingMode.LocalOnly);
        }

        private static object GetCurrentConfiguration(string section)
        {
            if (Configuration.Current == null) return null;
            Debug.Log($"Reading Config '{section}'");
            var properties = Configuration.Current.GetType().GetProperties();
            PropertyInfo property = properties.SingleOrDefault(p => p.Name.Equals(section, System.StringComparison.CurrentCultureIgnoreCase));
            if (property == null)
            {
                Debug.LogWarning($"Property '{section}' not found in Configuration");
                return null;
            }
            var thisConfiguration = property.GetValue(Configuration.Current) as T;
            return thisConfiguration;
        }
    }

    public abstract class ServerSyncConfig<T> : BaseConfig<T> where T : class, IConfig, new()
    {
        [LoadingOption(LoadingMode.Never)]
        public override bool NeedsServerSync { get; set; } = true;
    }

    public class LoadingOption : Attribute
    {
        public LoadingMode LoadingMode { get; }
        public LoadingOption(LoadingMode loadingMode)
        {
            LoadingMode = loadingMode;
        }
    }
    /// <summary>
    /// Defines, when a property is loaded
    /// </summary>
    public enum LoadingMode
    {
        Always = 0,
        RemoteOnly = 1,
        LocalOnly = 2,
        Never = 3
    }
}


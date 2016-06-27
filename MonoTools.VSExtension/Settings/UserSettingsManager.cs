using System;
using Microsoft.VisualStudio.Settings;
using Newtonsoft.Json;
using NLog;

namespace MonoTools.VSExtension.Settings
{
    public class UserSettingsManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly UserSettingsManager manager = new UserSettingsManager();
        private WritableSettingsStore store;

        private UserSettingsManager()
        {
        }

        public static UserSettingsManager Instance
        {
            get { return manager; }
        }

        public UserSettings Load()
        {
            var result = new UserSettings();

            if (store.CollectionExists("MonoTools.Debugger"))
            {
                try
                {
                    string content = store.GetString("MonoTools.Debugger", "Settings");
                    result = JsonConvert.DeserializeObject<UserSettings>(content);
                    return result;
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }

            return result;
        }

        public void Save(UserSettings settings)
        {
            string json = JsonConvert.SerializeObject(settings);
            if (!store.CollectionExists("MonoTools.Debugger"))
                store.CreateCollection("MonoTools.Debugger");
            store.SetString("MonoTools.Debugger", "Settings", json);
        }

        public static void Initialize(WritableSettingsStore configurationSettingsStore)
        {
            Instance.store = configurationSettingsStore;
        }
    }
}
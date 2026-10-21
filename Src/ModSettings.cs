using System;

namespace com.github.lhervier.ksp.mod.steaminput
{
    /// <summary>
    /// The settings of the mod, read from GameData/KSPModSteamInput/settings.cfg (node KSPModSteamInput).
    /// </summary>
    public static class ModSettings
    {
        private static readonly ModLogger LOGGER = new ModLogger("ModSettings");

        /// <summary>
        /// Reads the settings file and applies it. Missing file or values leave the defaults (log level Info).
        /// </summary>
        public static void Load()
        {
            ConfigNode node = GameDatabase.Instance.GetConfigNode(Constants.ModName + "/settings/" + Constants.ModName);
            if( node == null ) {
                LOGGER.LogInfo("No settings file, using the defaults");
                return;
            }
            string level = node.GetValue("logLevel");
            if( level != null && Enum.TryParse(level, true, out LogLevel logLevel) ) {
                ModLogger.SetLogLevel(logLevel);
                LOGGER.LogInfo("Log level: " + logLevel);
            }
        }
    }
}

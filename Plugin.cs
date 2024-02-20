using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EndlessMode
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	[BepInDependency("cyantist.inscryption.api", BepInDependency.DependencyFlags.HardDependency)]
	public class Plugin : BaseUnityPlugin
	{
		public const string PluginGuid = "jamesgames.inscryption.endlessmode";
		public const string PluginName = "Endless Mode";
		public const string PluginVersion = "0.3.0";

		public static Plugin Instance;
		public static ManualLogSource Log;
		public static string PluginDirectory;

		private void Awake()
		{
			Logger.LogInfo($"Loading {PluginName}...");
			Instance = this;
			Log = Logger;
			Configs.Instance = new Configs();

			PluginDirectory = this.Info.Location.Replace("EndlessMode.dll", "");

			new Harmony(PluginGuid).PatchAll();

			PrestigeSequencer.Initialize();
			Custom_AscensionStat.AddCustomStat(PluginGuid, "Final Bosses Killed", ()=>RunStats.TotalFinalBossesKilled);
			Custom_AscensionStat.AddCustomStat(PluginGuid, "Highest Floor", ()=>RunStats.CurrentFloor+1);

			Logger.LogInfo($"Loaded {PluginName}!");
		}
	}
}

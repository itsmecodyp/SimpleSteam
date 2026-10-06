using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		public void HarmonyPatchSetup()
		{
			Harmony harmony = new Harmony(pluginName);
			MethodInfo original;
			MethodInfo patch;
			original = AccessTools.Method(typeof(MapAction), "terraformTile");
			patch = AccessTools.Method(typeof(MainClass), "terraformTile_Postfix");
			harmony.Patch(original, null, new HarmonyMethod(patch));
			Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);


			// Add new power patch
			original = AccessTools.Method(typeof(PlayerControl), "clicked",
							new System.Type[] { typeof(Vector2Int), typeof(GodPower), typeof(bool) }); patch = AccessTools.Method(typeof(MainClass), "clicked_Postfix");
			harmony.Patch(original, null, new HarmonyMethod(patch));
			Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);

			original = AccessTools.Method(typeof(BuildingManager), "addBuilding",
							new System.Type[] { typeof(BuildingAsset), typeof(WorldTile), typeof(bool), typeof(bool), typeof(BuildPlacingType) }); patch = AccessTools.Method(typeof(MainClass), "addBuilding_Postfix");
			harmony.Patch(original, null, new HarmonyMethod(patch));
			Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);

			original = AccessTools.Method(typeof(Building), "setState");
			patch = AccessTools.Method(typeof(MainClass), "setState_Postfix");
			harmony.Patch(original, null, new HarmonyMethod(patch));
			Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);

			original = AccessTools.Method(typeof(Building), "startDestroyBuilding");
			patch = AccessTools.Method(typeof(MainClass), "startDestroyBuilding_Postfix");
			harmony.Patch(original, null, new HarmonyMethod(patch));
			Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);

			original = AccessTools.Method(typeof(ActorManager), "createNewUnit");
			patch = AccessTools.Method(typeof(MainClass), "createNewUnit_Postfix");
			harmony.Patch(original, null, new HarmonyMethod(patch));
			Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);

			original = AccessTools.Method(typeof(Actor), "getHit");
			patch = AccessTools.Method(typeof(MainClass), "getHit_Postfix");
			harmony.Patch(original, null, new HarmonyMethod(patch));
			Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);

			original = AccessTools.Method(typeof(Actor), "goTo");
			patch = AccessTools.Method(typeof(MainClass), "goTo_Postfix");
			harmony.Patch(original, null, new HarmonyMethod(patch));
			Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);

			original = AccessTools.Method(typeof(Actor), "die");
			patch = AccessTools.Method(typeof(MainClass), "die_Postfix");
			harmony.Patch(original, null, new HarmonyMethod(patch));
			Debug.Log(pluginName + ": Harmony patch finished: " + patch.Name);
		}

	}
}

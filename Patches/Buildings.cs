using BepInEx;
using System;
using UnityEngine;

namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		private static ChangeQueueManager<BuildingChangePacket> buildingAddManager;

		public static void addBuilding_Postfix(BuildingAsset pAsset, WorldTile pTile, bool pCheckForBuild, bool pSfx, BuildPlacingType pType, Building __result)
		{
			if (buildingAddManager.isEnabled && !isProcessingNetworkChange)
			{
				if (__result != null)
				{
					buildingAddManager.QueueChange(new BuildingChangePacket(
					pAsset.id,
					pTile.posV3
					));
				}
			}
		}

		public static void setState_Postfix(BuildingState pState, Building __instance)
		{
			if (buildingAddManager.isEnabled && !isProcessingNetworkChange)
			{
				if (__instance != null)
				{
					if (pState == BuildingState.Ruins)
					{
						buildingAddManager.QueueChange(new BuildingChangePacket(
	 __instance.asset.id,
	 __instance.current_position,
	 1
	 ));
					}

				}
			}
		}

		public static void startDestroyBuilding_Postfix(Building __instance)
		{
			if (buildingAddManager.isEnabled && !isProcessingNetworkChange)
			{
				buildingAddManager.QueueChange(new BuildingChangePacket(
__instance.asset.id,
__instance.current_position,
2
));
			}
		}

		public static void ReplicateBuildingAdd(BuildingChangePacket data)
		{
			try
			{
				isProcessingNetworkChange = true;
				WorldTile tile = World.world.GetTile((int)data.pos.x, (int)data.pos.y);
				if (tile != null)
				{
					Building building = World.world.buildings.addBuilding(AssetManager.buildings.get(data.buildingID), tile);
					building.checkSpriteToRender();
				}
			}
			finally
			{
				isProcessingNetworkChange = false;
			}
		}

		public static void ReplicateBuildingRuins(BuildingChangePacket data)
		{
			try
			{
				isProcessingNetworkChange = true;
				WorldTile tile = World.world.GetTile((int)data.pos.x, (int)data.pos.y);
				if (tile != null && tile.hasBuilding() && tile.building.asset.id == data.buildingID)
				{
					tile.building.makeRuins();
				}
			}
			finally
			{
				isProcessingNetworkChange = false;
			}
		}

		public static void ReplicateBuildingDestroy(BuildingChangePacket data)
		{
			try
			{
				isProcessingNetworkChange = true;
				WorldTile tile = World.world.GetTile((int)data.pos.x, (int)data.pos.y);
				if (tile != null && tile.hasBuilding() && tile.building.asset.id == data.buildingID)
				{
					tile.building.startDestroyBuilding();
				}
			}
			finally
			{
				isProcessingNetworkChange = false;
			}
		}


		[Serializable]
		public class BuildingChangePacket : INetworkChange
		{
			public string buildingID;
			public SerializableVector3 pos;
			public int changeState;	//0 = created, 1 = die/ruins, 2 = destroy

			public BuildingChangePacket(string buildingID, Vector3 pos, int state = 0)
			{
				this.buildingID = buildingID;
				this.pos = new SerializableVector3(pos);
				this.changeState = state;
			}

			public string GetKey() => $"{pos.x}_{pos.y}";
			public PacketType GetPacketType() => PacketType.BuildingChange;
			public int GetPriority() => 1;
		}
	}
}

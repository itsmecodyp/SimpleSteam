using BepInEx;
using System;
using UnityEngine;

namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		//create manager for tile changes
		private static ChangeQueueManager<TileChangePacket> tileChangeManager;

		//relays every single tile terraform and its type
		public static void terraformTile_Postfix(WorldTile pTile, TileType pNewTypeMain, TopTileType pTopType, TerraformOptions pOptions = null)
		{
			if (tileChangeManager.isEnabled && !isProcessingNetworkChange)
			{
				string typeMain = pNewTypeMain?.id;
				string typeTop = pTopType?.id;
				Debug.Log($"Queueing tile change: {typeMain}/{typeTop} at {pTile.posV3}");
				tileChangeManager.QueueChange(new TileChangePacket(
						typeMain,
						typeTop,
						pTile.posV3
				));
			}
		}

		//replicates the terraform that is relayed
		public static void ReplicateTileChange(TileChangePacket data)
		{
			try
			{
				isProcessingNetworkChange = true;
				WorldTile tile = World.world.GetTile((int)data.pos.x, (int)data.pos.y);
				if (tile != null)
				{
					TileType mainType = AssetManager.tiles.get(data.mainType);
					TopTileType topType = string.IsNullOrEmpty(data.topType) ? null : AssetManager.top_tiles.get(data.topType);
					MapAction.terraformTile(tile, mainType, topType);
					Log($"Replicated tile change at ({data.pos.x}, {data.pos.y})");
				}
			}
			finally
			{
				isProcessingNetworkChange = false;
			}
		}

		[Serializable]
		public class TileChangePacket : INetworkChange
		{
			public string mainType;
			public string topType;
			public SerializableVector3 pos;
			public TileChangePacket(string mainType, string topType, Vector3 pos)
			{
				this.mainType = mainType;
				this.topType = topType;
				this.pos = new SerializableVector3(pos);
			}
			public string GetKey() => $"{pos.x}_{pos.y}";
			public PacketType GetPacketType() => PacketType.TileChange;
			public int GetPriority() => 1;
		}
	}
}

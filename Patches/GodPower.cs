using BepInEx;
using System;
using UnityEngine;

namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		private static ChangeQueueManager<PowerUsePacket> powerChangeManager;

		//relay power id, brush id(size,shape), and position used
		public static void clicked_Postfix(Vector2Int pPos, GodPower pPower = null, bool pTrack = true)
		{
			if (powerChangeManager.isEnabled && !isProcessingNetworkChange && pPower != null)
			{
				if (powerChangeManager == null)
				{
					return;
				}
				powerChangeManager.QueueChange(new PowerUsePacket(
						pPower.id,
						new Vector3(pPos.x, pPos.y, 0),
						global::Config.current_brush
				));
			}
		}

		//replicate relayed power
		public static void ReplicatePowerUse(PowerUsePacket data)
		{
			try
			{
				isProcessingNetworkChange = true;
				Vector2Int position = new Vector2Int((int)data.pos.x, (int)data.pos.y);
				GodPower power = AssetManager.powers.get(data.powerId);
				if (power != null)
				{
					global::Config.current_brush = data.brushID;
					World.world.player_control.clickedFinal(position, power, true);
				}
			}
			finally
			{
				isProcessingNetworkChange = false;
			}
		}

		[Serializable]
		public class PowerUsePacket : INetworkChange
		{
			public string powerId;
			public SerializableVector3 pos;
			public string brushID;

			public PowerUsePacket(string powerId, Vector3 pos, string brushID)
			{
				this.powerId = powerId;
				this.pos = new SerializableVector3(pos);
				this.brushID = brushID;
			}

			public string GetKey() => $"{pos.x}_{pos.y}_{powerId}";
			public PacketType GetPacketType() => PacketType.PowerUse;
			public int GetPriority() => 1;
		}
	}
}

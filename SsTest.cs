using BepInEx;
using Steamworks;
using UnityEngine;
namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		public static void Init()
		{
			SteamNetworking.OnP2PSessionRequest += OnP2PSessionRequest;
			Log("Multiplayer system ready (static SteamNetworking).");
		}
		public static void OnP2PSessionRequest(SteamId remoteSteamId)
		{
			// Automatically accept the P2P session request from the user
			Debug.Log($"P2P Session Request from SteamID: {remoteSteamId}");
			SteamNetworking.AcceptP2PSessionWithUser(remoteSteamId);
			Debug.Log($"Accepted P2P session with {remoteSteamId}");
			MainClass.isConnected = true;
			connectedSteamId = remoteSteamId; // Store the connected friend's SteamID

			SendText(remoteSteamId, "accepted session");
		}
		public static void UpdateStuff()
		{
			// Keep checking for new incoming packets
			while (SteamNetworking.IsP2PPacketAvailable(0))
			{
				uint size = 0;
				SteamId remoteId = default;

				byte[] buffer = new byte[1024]; // Temporary buffer

				if (SteamNetworking.ReadP2PPacket(buffer, ref size, ref remoteId, 0))
				{
					// Read the packet type from the first byte
					PacketType packetType = (PacketType)buffer[0];
					byte[] data = new byte[size - 1];
					System.Array.Copy(buffer, 1, data, 0, data.Length);

					// Process the packet using our new system
					ProcessReceivedPacket(packetType, data, remoteId);
				}
			}
		}

		// Updated send methods that use the new system
		public static void SendText(SteamId targetId, string text)
		{
			SendData(targetId, PacketType.Text, text);
		}

		public static void SendPosition(SteamId targetId, Vector3 position)
		{
			SendData(targetId, PacketType.Position, position);
		}

		// New methods for sending various data types
		public static void SendInt(SteamId targetId, int value)
		{
			SendData(targetId, PacketType.IntValue, value);
		}

		public static void SendFloat(SteamId targetId, float value)
		{
			SendData(targetId, PacketType.FloatValue, value);
		}

		public static void SendDouble(SteamId targetId, double value)
		{
			SendData(targetId, PacketType.DoubleValue, value);
		}

		public static void SendULong(SteamId targetId, ulong value)
		{
			SendData(targetId, PacketType.ULongValue, value);
		}

		public static void SendLong(SteamId targetId, long value)
		{
			SendData(targetId, PacketType.LongValue, value);
		}

		public static void SendBool(SteamId targetId, bool value)
		{
			SendData(targetId, PacketType.BoolValue, value);
		}

		public static void SendMessage(SteamId targetId, PacketType packetType, byte[] data)
		{
			// Create a new buffer for the packet, start with the packet type
			byte[] packet = new byte[1 + data.Length]; // 1 byte for packet type

			packet[0] = (byte)packetType; // Set the packet type in the header
			System.Array.Copy(data, 0, packet, 1, data.Length); // Copy the data into the buffer

			bool success = SteamNetworking.SendP2PPacket(
							targetId,
							packet,
							packet.Length,
							0 // channel 0
			);

			if (success)
				Log($"Sent {packetType} message to {targetId}");
			else
				Log($"Failed to send {packetType} message to {targetId}");
		}

		private static void Log(string msg)
		{
			UnityEngine.Debug.Log("[WorldBoxMP] " + msg);
			LogMessage(msg);
		}

		public static void InviteFriend(SteamId friendId)
		{
			SteamFriends.OpenGameInviteOverlay(friendId);
			Log($"Opened invite overlay for {friendId}");
		}

		public static SteamId? FindFriendByName(string partialName)
		{
			foreach (var friend in SteamFriends.GetFriends())
			{
				if (friend.Name.IndexOf(partialName, System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					Log($"Found friend: {friend.Name} ({friend.Id})");
					return friend.Id;
				}
			}

			Log($"No friend matching \"{partialName}\" found.");
			return null;
		}

		public void Te()
		{
		}
	}
}

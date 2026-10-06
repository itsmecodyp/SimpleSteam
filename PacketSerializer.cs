using Steamworks;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using static SimplyNeedsAName.MainClass;

namespace SimplyNeedsAName
{
	public static class PacketSerializer
	{
		/// <summary>
		/// Serializes an object of any serializable type to a byte array
		/// </summary>
		public static byte[] Serialize<T>(T data)
		{
			Type type = typeof(T);

			// Use most efficient method based on type
			if (data == null)
			{
				return new byte[0];
			}

			// String - UTF8 encoding
			else if (type == typeof(string))
			{
				return Encoding.UTF8.GetBytes((string)(object)data);
			}

			// Vector3 - direct serialization
			else if (type == typeof(Vector3) || type == typeof(SerializableVector3))
			{
				Vector3 vector = type == typeof(Vector3) ?
						(Vector3)(object)data :
						(Vector3)(SerializableVector3)(object)data;

				byte[] result = new byte[12];
				BitConverter.GetBytes(vector.x).CopyTo(result, 0);
				BitConverter.GetBytes(vector.y).CopyTo(result, 4);
				BitConverter.GetBytes(vector.z).CopyTo(result, 8);
				return result;
			}

			// Numeric primitives - direct conversion
			else if (type == typeof(int))
			{
				return BitConverter.GetBytes((int)(object)data);
			}
			else if (type == typeof(float))
			{
				return BitConverter.GetBytes((float)(object)data);
			}
			else if (type == typeof(double))
			{
				return BitConverter.GetBytes((double)(object)data);
			}
			else if (type == typeof(ulong))
			{
				return BitConverter.GetBytes((ulong)(object)data);
			}
			else if (type == typeof(long))
			{
				return BitConverter.GetBytes((long)(object)data);
			}
			else if (type == typeof(bool))
			{
				return BitConverter.GetBytes((bool)(object)data);
			}

			// For TileChangePacket - hand-optimized serialization
			else if (type == typeof(TileChangePacket))
			{
				return SerializeTileChangePacket((TileChangePacket)(object)data);
			}

			// Fall back to BinaryFormatter for complex types
			else
			{
				try
				{
					using (MemoryStream stream = new MemoryStream())
					{
						BinaryFormatter formatter = new BinaryFormatter();
						formatter.Serialize(stream, data);
						return stream.ToArray();
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Failed to serialize object of type {type.Name}: {ex.Message}");
					return new byte[0];
				}
			}
		}

		// Custom fast serialization for TileChangePacket
		private static byte[] SerializeTileChangePacket(TileChangePacket packet)
		{
			// Get the byte arrays for strings
			byte[] mainTypeBytes = Encoding.UTF8.GetBytes(packet.mainType ?? "");
			byte[] topTypeBytes = Encoding.UTF8.GetBytes(packet.topType ?? "");

			// Calculate total size
			int totalSize =
					4 + mainTypeBytes.Length +  // 4 bytes for length + main type
					4 + topTypeBytes.Length +   // 4 bytes for length + top type
					12;                         // 12 bytes for Vector3

			byte[] result = new byte[totalSize];
			int position = 0;

			// Write main type length and bytes
			BitConverter.GetBytes(mainTypeBytes.Length).CopyTo(result, position);
			position += 4;
			mainTypeBytes.CopyTo(result, position);
			position += mainTypeBytes.Length;

			// Write top type length and bytes
			BitConverter.GetBytes(topTypeBytes.Length).CopyTo(result, position);
			position += 4;
			topTypeBytes.CopyTo(result, position);
			position += topTypeBytes.Length;

			// Write Vector3
			BitConverter.GetBytes(packet.pos.x).CopyTo(result, position);
			position += 4;
			BitConverter.GetBytes(packet.pos.y).CopyTo(result, position);
			position += 4;
			BitConverter.GetBytes(packet.pos.z).CopyTo(result, position);

			return result;
		}

		// Custom fast deserialization for TileChangePacket
		private static TileChangePacket DeserializeTileChangePacket(byte[] data)
		{
			int position = 0;


			// Read main type
			int mainTypeLength = BitConverter.ToInt32(data, position);
			position += 4;
			string mainType = Encoding.UTF8.GetString(data, position, mainTypeLength);
			position += mainTypeLength;

			// Read top type
			int topTypeLength = BitConverter.ToInt32(data, position);
			position += 4;
			string topType = Encoding.UTF8.GetString(data, position, topTypeLength);
			position += topTypeLength;

			// Read Vector3
			float x = BitConverter.ToSingle(data, position);
			position += 4;
			float y = BitConverter.ToSingle(data, position);
			position += 4;
			float z = BitConverter.ToSingle(data, position);

			// Create and return packet
			TileChangePacket packet = new TileChangePacket(mainType, topType, new Vector3(x, y, z));
			return packet;
		}
		/// <summary>
		/// Fast deserialization for primitive and compound types
		/// </summary>
		public static T Deserialize<T>(byte[] data)
		{
			Type type = typeof(T);

			if (data == null || data.Length == 0)
			{
				return default;
			}

			// String - UTF8 decoding
			if (type == typeof(string))
			{
				return (T)(object)Encoding.UTF8.GetString(data);
			}

			// Vector3 - direct deserialization
			else if (type == typeof(Vector3) || type == typeof(SerializableVector3))
			{
				if (data.Length >= 12)
				{
					float x = BitConverter.ToSingle(data, 0);
					float y = BitConverter.ToSingle(data, 4);
					float z = BitConverter.ToSingle(data, 8);

					return type == typeof(Vector3) ? (T)(object)new Vector3(x, y, z) : (T)(object)new SerializableVector3(x, y, z);
				}
				return default;
			}

			// Numeric primitives - direct conversion
			else if (type == typeof(int) && data.Length >= 4)
			{
				return (T)(object)BitConverter.ToInt32(data, 0);
			}
			else if (type == typeof(float) && data.Length >= 4)
			{
				return (T)(object)BitConverter.ToSingle(data, 0);
			}
			else if (type == typeof(double) && data.Length >= 8)
			{
				return (T)(object)BitConverter.ToDouble(data, 0);
			}
			else if (type == typeof(ulong) && data.Length >= 8)
			{
				return (T)(object)BitConverter.ToUInt64(data, 0);
			}
			else if (type == typeof(long) && data.Length >= 8)
			{
				return (T)(object)BitConverter.ToInt64(data, 0);
			}
			else if (type == typeof(bool) && data.Length >= 1)
			{
				return (T)(object)BitConverter.ToBoolean(data, 0);
			}

			// For TileChangePacket - hand-optimized deserialization
			else if (type == typeof(TileChangePacket))
			{
				return (T)(object)DeserializeTileChangePacket(data);
			}

			// Fall back to BinaryFormatter for complex types
			else
			{
				try
				{
					using (MemoryStream stream = new MemoryStream(data))
					{
						BinaryFormatter formatter = new BinaryFormatter();
						return (T)formatter.Deserialize(stream);
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Failed to deserialize to type {type.Name}: {ex.Message}");
					return default;
				}
			}
		}
	}
	// Modified MainClass to use the new serialization system
	internal partial class MainClass
	{
		public enum PacketType
		{
			Text = 1,
			Position = 2,
			GameState = 3,
			PlayerData = 4,
			IntValue = 5,
			FloatValue = 6,
			DoubleValue = 7,
			ULongValue = 8,
			LongValue = 9,
			BoolValue = 10,
			TileChange = 11,
			PowerUse = 12,
			BuildingChange = 13,
			ActorChange = 14,
			CustomData = 15
		}

		public static void SendData<T>(SteamId targetId, PacketType packetType, T data, int priority = 0)
		{
			byte[] serializedData = PacketSerializer.Serialize(data);

			// Queue instead of sending immediately
			QueuePacket(targetId, packetType, serializedData, priority);
		}

		public static void ProcessNetworkQueue()
		{
			UpdateRateLimits(packetQueue.Count);
			ProcessQueue();
		}

		/// <summary>
		/// Process received packets with automatic deserialization
		/// </summary>
		public static void ProcessReceivedPacket(PacketType packetType, byte[] data, SteamId senderId)
		{
			switch (packetType)
			{
				case PacketType.Text:
					string text = PacketSerializer.Deserialize<string>(data);
					Log($"Received Text from {senderId}: {text}");
					if (text == "accepted session")
					{
						MainClass.isConnected = true;
						connectedSteamId = senderId; // Store the connected friend's SteamID
					}
					break;

				case PacketType.Position:
					Vector3 position = PacketSerializer.Deserialize<Vector3>(data);
					Log($"Received Position from {senderId}: {position}");
					break;

				case PacketType.IntValue:
					int intValue = PacketSerializer.Deserialize<int>(data);
					Log($"Received Int from {senderId}: {intValue}");
					break;

				case PacketType.FloatValue:
					float floatValue = PacketSerializer.Deserialize<float>(data);
					Log($"Received Float from {senderId}: {floatValue}");
					break;

				case PacketType.DoubleValue:
					double doubleValue = PacketSerializer.Deserialize<double>(data);
					Log($"Received Double from {senderId}: {doubleValue}");
					break;

				case PacketType.ULongValue:
					ulong ulongValue = PacketSerializer.Deserialize<ulong>(data);
					Log($"Received ULong from {senderId}: {ulongValue}");
					break;

				case PacketType.LongValue:
					long longValue = PacketSerializer.Deserialize<long>(data);
					Log($"Received Long from {senderId}: {longValue}");
					break;

				case PacketType.BoolValue:
					bool boolValue = PacketSerializer.Deserialize<bool>(data);
					Log($"Received Bool from {senderId}: {boolValue}");
					break;

				case PacketType.TileChange:
					TileChangePacket tileChange = PacketSerializer.Deserialize<TileChangePacket>(data);
					if (tileChange != null)
					{
						ReplicateTileChange(tileChange);
					}
					break;

				case PacketType.BuildingChange:
					BuildingChangePacket buildingAdd = PacketSerializer.Deserialize<BuildingChangePacket>(data);
					if (buildingAdd != null)
					{
						switch (buildingAdd.changeState)
						{
							case 0:
								ReplicateBuildingAdd(buildingAdd);
								break;
							case 1:
								ReplicateBuildingRuins(buildingAdd);
								break;
							case 2:
								ReplicateBuildingDestroy(buildingAdd);
								break;
						}
					}
					break;

				case PacketType.ActorChange:
					ActorChangePacket actorChange = PacketSerializer.Deserialize<ActorChangePacket>(data);
					if (actorChange != null)
					{
						switch (actorChange.state)
						{
							case 0:
								ReplicateActorSpawn(actorChange);
								break;
							case 1:
								ReplicateActorDamage(actorChange);
								break;
							case 2:
								ReplicateActorGoto(actorChange);
								break;
							case 3:
								ReplicateActorDie(actorChange);
								break;
						}
					}
					break;

				case PacketType.PowerUse:
					PowerUsePacket powerData = PacketSerializer.Deserialize<PowerUsePacket>(data);
					ReplicatePowerUse(powerData);
					break;

				case PacketType.CustomData:
					// Example for a custom class - you'd need to specify the type
					try
					{
						// Replace YourCustomClass with the actual class you want to deserialize
						// YourCustomClass customData = PacketSerializer.Deserialize<YourCustomClass>(data);
						// Log($"Received Custom Data from {senderId}");
						Log($"Received Custom Data from {senderId}");
						HandleCustomData(data, senderId);
					}
					catch (Exception ex)
					{
						Log($"Failed to deserialize Custom Data: {ex.Message}");
					}
					break;

				default:
					Log($"Unknown packet type from {senderId}");
					break;
			}
		}

		// Add this to your ProcessReceivedPacket method
		private static void HandleCustomData(byte[] data, SteamId senderId)
		{
			try
			{
				// Try to deserialize as TileChangePacket first
				//TileChangePacket tileChange = PacketSerializer.Deserialize<TileChangePacket>(data);
				Log($"Received custom data from {senderId}");

				/*
				if (tileChange != null)
				{
					Log($"Received custom data from {senderId}: {tileChange.pos}, {tileChange.mainType}, {tileChange.topType}");
					// Handle the player stats as needed
					return;
				}
																				*/
				// If that fails, log a generic message
				Log($"Received Custom Data from {senderId} (unknown format)");
			}
			catch (Exception ex)
			{
				Log($"Failed to deserialize Custom Data: {ex.Message}");
			}
		}




		// Serializable wrapper for Vector3
		[Serializable]
		public struct SerializableVector3
		{
			public float x;
			public float y;
			public float z;

			// Constructors and conversion operators
			public SerializableVector3(float x, float y, float z)
			{
				this.x = x;
				this.y = y;
				this.z = z;
			}

			public SerializableVector3(Vector3 vector)
			{
				x = vector.x;
				y = vector.y;
				z = vector.z;
			}

			// Implicit conversion from SerializableVector3 to Vector3
			public static implicit operator Vector3(SerializableVector3 serializableVector)
			{
				return new Vector3(serializableVector.x, serializableVector.y, serializableVector.z);
			}

			// Implicit conversion from Vector3 to SerializableVector3
			public static implicit operator SerializableVector3(Vector3 vector)
			{
				return new SerializableVector3(vector);
			}

			public override string ToString()
			{
				return $"({x}, {y}, {z})";
			}
		}

	}
}
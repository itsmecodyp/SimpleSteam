using BepInEx;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		// Base interface for all network changes
		public interface INetworkChange
		{
			string GetKey();
			PacketType GetPacketType();
			int GetPriority();
		}

		// Generic manager for any type of change
		private class ChangeQueueManager<T> where T : INetworkChange
		{
			private Dictionary<string, T> pendingChanges = new Dictionary<string, T>();
			private float lastUpdateTime = 0f;
			private float updateInterval;
			private string queueName;
			private Action<T> sendAction;
			public bool isEnabled = false;

			public ChangeQueueManager(string name, float interval, Action<T> sender)
			{
				queueName = name;
				updateInterval = interval;
				sendAction = sender;
			}

			public void Enable(bool enabled)
			{
				isEnabled = enabled;
				Debug.Log($"{queueName} manager enabled: {enabled}");
			}

			public void QueueChange(T change)
			{
				if (!isEnabled)
				{
					Debug.Log($"{queueName} manager not enabled, skipping change");
					return;
				}

				if (isProcessingNetworkChange)
				{
					Debug.Log($"Currently processing network change, skipping {queueName} change");
					return;
				}

				string key = change.GetKey();
				pendingChanges[key] = change;
				Debug.Log($"{queueName} queue now has {pendingChanges.Count} pending changes");
			}

			public void ProcessQueue()
			{
				if (!isEnabled) return;

				float currentTime = Time.realtimeSinceStartup;

				// Only send updates periodically
				if (currentTime - lastUpdateTime < updateInterval || pendingChanges.Count == 0)
				{
					return;
				}

				if (connectedSteamId.HasValue)
				{
					int count = pendingChanges.Count;
					Debug.Log($"Processing {count} {queueName} changes...");

					foreach (T change in pendingChanges.Values)
					{
						try
						{
							sendAction(change);
							Debug.Log($"Sent {queueName} change with key: {change.GetKey()}");
						}
						catch (Exception ex)
						{
							Debug.LogError($"Error sending {queueName} change: {ex.Message}");
						}
					}

					Debug.Log($"Sent {count} batched {queueName} changes");
					pendingChanges.Clear();
					lastUpdateTime = currentTime;
				}
				else
				{
					Debug.LogWarning($"No Steam connection, couldn't send {pendingChanges.Count} {queueName} changes");
				}
			}

			public int GetPendingCount()
			{
				return pendingChanges.Count;
			}
		}

		// Ensure this is called once during initialization
		public static void InitializeChangeManagers()
		{
			Debug.Log("Initializing change managers...");

			// Make sure SendData exists and works properly
			tileChangeManager = new ChangeQueueManager<TileChangePacket>("Tile", 0.1f,
					change => {
						Debug.Log($"Sending tile change at ({change.pos.x}, {change.pos.y})");
						SendData(connectedSteamId.Value, change.GetPacketType(), change, change.GetPriority());
					});

			powerChangeManager = new ChangeQueueManager<PowerUsePacket>("Power", 0.1f,
					change => {
						Debug.Log($"Sending power change: {change.powerId} at ({change.pos.x}, {change.pos.y})");
						SendData(connectedSteamId.Value, change.GetPacketType(), change, change.GetPriority());
					});

			buildingAddManager = new ChangeQueueManager<BuildingChangePacket>("Building", 0.1f,
					change => {
						Debug.Log($"Sending building change: {change.buildingID} at ({change.pos.x}, {change.pos.y})");
						SendData(connectedSteamId.Value, change.GetPacketType(), change, change.GetPriority());
					});

			actorChangeManager = new ChangeQueueManager<ActorChangePacket>("Actor", 0.1f,
				change => {
					Debug.Log($"Sending actor change: {change.statsID} at ({change.pos.x}, {change.pos.y})");
					SendData(connectedSteamId.Value, change.GetPacketType(), change, change.GetPriority());
				});
			
			Debug.Log("Change managers initialized.");
		}
		// This is called from your Update method
		public static void ProcessAllChangeQueues()
		{
			// Process all queues
			tileChangeManager.ProcessQueue();
			powerChangeManager.ProcessQueue();
			buildingAddManager.ProcessQueue();
			actorChangeManager.ProcessQueue();
		}

		private static bool isProcessingNetworkChange = false;
	}
}

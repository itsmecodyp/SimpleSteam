using BepInEx;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimplyNeedsAName
{
  partial class MainClass : BaseUnityPlugin
  {
    private static Queue<QueuedPacket> packetQueue = new Queue<QueuedPacket>();
    private static float lastSendTime = 0f;
    private static int maxPacketsPerFrame = 10;
    private static float minTimeBetweenSends = 0.01f;
    private class QueuedPacket
    {
      public SteamId TargetId;
      public PacketType Type;
      public byte[] Data;
      public int Priority; // Higher priority packets get sent first
      public QueuedPacket(SteamId targetId, PacketType type, byte[] data, int priority = 0)
      {
        TargetId = targetId;
        Type = type;
        Data = data;
        Priority = priority;
      }
    }

    public static void QueuePacket(SteamId targetId, PacketType packetType, byte[] data, int priority = 0)
    {
      packetQueue.Enqueue(new QueuedPacket(targetId, packetType, data, priority));
    }

    public static void ProcessQueue()
    {
      float currentTime = Time.realtimeSinceStartup;

      // Check if enough time has passed since last send
      if (currentTime - lastSendTime < minTimeBetweenSends)
        return;

      // Process a limited number of packets per frame
      int packetsSent = 0;
      while (packetQueue.Count > 0 && packetsSent < maxPacketsPerFrame)
      {
        QueuedPacket packet = packetQueue.Dequeue();

        // Create header with packet type
        byte[] fullPacket = new byte[1 + packet.Data.Length];
        fullPacket[0] = (byte)packet.Type;
        Array.Copy(packet.Data, 0, fullPacket, 1, packet.Data.Length);

        // Send the packet
        bool success = SteamNetworking.SendP2PPacket(
            packet.TargetId,
            fullPacket,
            fullPacket.Length,
            0
        );

        if (success)
          MainClass.Log($"Sent queued {packet.Type} message to {packet.TargetId}");
        else
          MainClass.Log($"Failed to send queued {packet.Type} message to {packet.TargetId}");

        packetsSent++;
      }

      if (packetsSent > 0)
        lastSendTime = currentTime;
    }

    // Adjust the rate limiting parameters based on network conditions
    public static void UpdateRateLimits(int queueSize)
    {
      // Dynamically adjust packet sending rate based on queue size
      // More packets in queue = send more per frame up to a reasonable limit
      if (queueSize > 100)
        maxPacketsPerFrame = 20;
      else if (queueSize > 50)
        maxPacketsPerFrame = 15;
      else
        maxPacketsPerFrame = 10;
    }
  }
}
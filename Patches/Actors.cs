using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SimplyNeedsAName
{
	partial class MainClass : BaseUnityPlugin
	{
		private static ChangeQueueManager<ActorChangePacket> actorChangeManager;

		public static void createNewUnit_Postfix(string pStatsID, WorldTile pTile, bool pMiracleSpawn, float pSpawnHeight, Subspecies pSubspecies, Subspecies pSubspeciesMutateFrom, bool pSpawnWithItems, bool pAdultAge, Actor __result)
		{
			if (actorChangeManager.isEnabled && !isProcessingNetworkChange)
			{
				if (__result != null)
				{
					// Register the actor in our cache
					RegisterActor(__result);
					actorChangeManager.QueueChange(new ActorChangePacket(
						__result.asset.id,
					__result.data.id,
					pTile.posV3,
					Vector3.up
					));
				}
			}
		}

		public static void getHit_Postfix(float pDamage, bool pFlash, AttackType pAttackType, BaseSimObject pAttacker, bool pSkipIfShake, bool pMetallicWeapon, Actor __instance)
		{
			if (actorChangeManager.isEnabled && !isProcessingNetworkChange)
			{
				actorChangeManager.QueueChange(new ActorChangePacket(
__instance.asset.id,
__instance.data.id,
__instance.current_position,
Vector3.up,
1,
pDamage
));
			}
		}

		public static void goTo_Postfix(WorldTile pTile, bool pPathOnWater, bool pWalkOnBlocks, bool pWalkOnLava, Actor __instance)
		{
			if (actorChangeManager.isEnabled && !isProcessingNetworkChange)
			{
				actorChangeManager.QueueChange(new ActorChangePacket(
__instance.asset.id,
__instance.data.id,
__instance.current_position,
pTile.posV3,
2
));
			}
		}

		public static void die_Postfix(bool pDestroy, AttackType pType, bool pCountDeath, bool pLaunchCallbacks, bool pLogFavorite, Actor __instance)
		{
			if (actorChangeManager.isEnabled && !isProcessingNetworkChange)
			{
				actorChangeManager.QueueChange(new ActorChangePacket(
__instance.asset.id,
__instance.data.id,
__instance.current_position,
__instance.current_position,
3
));
			}
			if (__instance != null && actorCache.ContainsKey(__instance.data.id))
			{
				actorCache.Remove(__instance.data.id);
			}
		}

		public static void ReplicateActorSpawn(ActorChangePacket data)
		{
			try
			{
				Log("replicating actor spawn");
				isProcessingNetworkChange = true;
				WorldTile tile = World.world.GetTile((int)data.pos.x, (int)data.pos.y);
				if (tile != null)
				{
					Actor actor = World.world.units.createNewUnit(data.statsID, tile, pAdultAge: true);
					actor.data.id = data.actorID; //force "unique" id to be the same on replicated unit, to make it easier to search-find
					RegisterActor(actor);
				}
			}
			finally
			{
				isProcessingNetworkChange = false;
			}
		}

		public static void ReplicateActorGoto(ActorChangePacket data)
		{
			try
			{
				isProcessingNetworkChange = true;
				Actor targetActor = GetActorById(data.actorID);
				if (targetActor == null)
				{
					Log("target actor was null for goto");
					return;
				}

				WorldTile targetTile = World.world.GetTile((int)data.tileTargetPos.x, (int)data.tileTargetPos.y);
				if (targetTile != null)
				{
					targetActor.stopMovement();
					targetActor.goTo(targetTile);
				}

			}
			finally
			{
				isProcessingNetworkChange = false;
			}
		}

		public static void ReplicateActorDie(ActorChangePacket data)
		{
			try
			{
				isProcessingNetworkChange = true;
				Actor targetActor = GetActorById(data.actorID);
				if (targetActor == null)
				{
					Log("target actor was null for goto");
					return;
				}
				targetActor.die();
			}
			finally
			{
				isProcessingNetworkChange = false;
			}
		}


		// Actor cache for quick lookups by ID
		private static Dictionary<long, Actor> actorCache = new Dictionary<long, Actor>();
		// Method to register actors in the cache
		public static void RegisterActor(Actor actor)
		{
			if (actor != null && !actorCache.ContainsKey(actor.data.id))
			{
				actorCache[actor.data.id] = actor;
			}
		}

		// Method to retrieve actors from the cache
		public static Actor GetActorById(long actorId)
		{
			if (actorCache.TryGetValue(actorId, out Actor actor))
			{
				// Verify the actor is still valid
				if (actor != null && actor.isAlive())
				{
					return actor;
				}
				else
				{
					// Remove invalid actors from cache
					actorCache.Remove(actorId);
				}
			}
			// Fallback: Try to find the actor in the world if not in cache
			return FindActorInWorld(actorId);
		}

		// Utility method to find an actor in the world (slower, but complete search)
		private static Actor FindActorInWorld(long actorId)
		{
			foreach (var actor in World.world.units.getSimpleList())
			{
				if (actor.data.id == actorId)
				{
					// Add to cache for future lookups
					actorCache[actorId] = actor;
					return actor;
				}
			}
			return null;
		}

		// Method to clean up the cache (call periodically or on world events)
		public static void CleanupActorCache()
		{
			List<long> keysToRemove = new List<long>();

			foreach (var pair in actorCache)
			{
				if (pair.Value == null || !pair.Value.isAlive())
				{
					keysToRemove.Add(pair.Key);
				}
			}

			foreach (var key in keysToRemove)
			{
				actorCache.Remove(key);
			}
		}

		public static void ReplicateActorDamage(ActorChangePacket data)
		{
			try
			{
				isProcessingNetworkChange = true;

				// Try to get the actor from cache first
				Actor targetActor = GetActorById(data.actorID);

				if (targetActor != null)
				{
					targetActor.getHit(data.damage, true, AttackType.Other);
				}
				else
				{
					// Fallback to original implementation if not in cache
					WorldTile tile = World.world.GetTile((int)data.pos.x, (int)data.pos.y);
					if (tile != null && tile._units?.Count > 0)
					{
						for (int i = 0; i < tile._units.Count; i++)
						{
							Actor actorOnTile = tile._units[i];
							if (actorOnTile.data.id == data.actorID)
							{
								actorOnTile.getHit(data.damage, true, AttackType.Other);
								break;
							}
						}
					}
				}
			}
			finally
			{
				isProcessingNetworkChange = false;
			}
		}
		[Serializable]
		public class ActorChangePacket : INetworkChange
		{
			//asset id, but could we grab with unique id?
			public string statsID;
			public long actorID;
			public SerializableVector3 pos;
			public int state = 0;
			public float damage = 0;
			public SerializableVector3 tileTargetPos;

			public ActorChangePacket(string statsID, long actorID, Vector3 pos, Vector3 tilePos, int state = 0, float damage = 0)
			{
				this.statsID = statsID;
				this.actorID = actorID;
				this.pos = new SerializableVector3(pos);
				this.state = state;
				this.damage = damage;
				this.tileTargetPos = tilePos;
			}

			public string GetKey() => $"{pos.x}_{pos.y}";
			public PacketType GetPacketType() => PacketType.ActorChange;
			public int GetPriority() => 1;
		}
	}
}

using BepInEx;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

namespace SimplyNeedsAName
{
	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	internal partial class MainClass : BaseUnityPlugin
	{
        public static HarmonyLib.Harmony harmony;

		public const string pluginGuid = "cody.needs.a.name";
		public const string pluginName = "needAName";
		public const string pluginVersion = "0.0.0.1";
		public void Awake()
		{
			InitializeChangeManagers();
			HarmonyPatchSetup();
			Debug.Log("Loaded " + pluginName);
		}
        public void OnDestroy()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }



		public static bool hasInit;

		public static bool isConnected = false; // Flag for tracking connection status
		public static SteamId? connectedSteamId = null;

		public void Update()
		{
			if (global::Config.game_loaded)
			{
				if (hasInit == false)
				{
					Init();
					hasInit = true;
				}
				else
				{
					UpdateStuff();
					ProcessNetworkQueue();
					ProcessAllChangeQueues();
				}
			}
		}

		public void controlWindow(int windowID)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Name"))
			{

			}
			nameText = GUILayout.TextField(nameText);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("SendMessage"))
			{
				SteamId? friendId = FindFriendByName(nameText);
				if (friendId.HasValue)
				{
					SendText(friendId.Value, messageText);
				}
			}
			messageText = GUILayout.TextField(messageText);
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("SendMessageSelf"))
			{
				SteamId mySteamId = SteamClient.SteamId;
				SendText(mySteamId, messageText);
			}
			messageText = GUILayout.TextField(messageText);
			GUILayout.EndHorizontal();
			if (GUILayout.Button("send pos test"))
			{
				SteamId? friendId = FindFriendByName(nameText);
				if (friendId.HasValue)
				{
					SendPosition(friendId.Value, new Vector3(1, 15, 29));
				}
				else
				{
					SteamId mySteamId = SteamClient.SteamId;
					SendPosition(mySteamId, new Vector3(1, 15, 29));
				}
			}
			// Invite/Connect Button
			if (isConnected)
			{
				GUILayout.Label("Connected!");
			}
			else
			{

			}
			if (GUILayout.Button("send tile:" + tileChangeManager.isEnabled))
			{
				// Enable managers based on config
				tileChangeManager.isEnabled = !tileChangeManager.isEnabled;
			}
			if (GUILayout.Button("send power:" + powerChangeManager.isEnabled))
			{
				powerChangeManager.isEnabled = !powerChangeManager.isEnabled;
			}
			if (GUILayout.Button("send building:" + buildingAddManager.isEnabled))
			{
				buildingAddManager.isEnabled = !buildingAddManager.isEnabled;
			}
			if (GUILayout.Button("send actor:" + actorChangeManager.isEnabled))
			{
				actorChangeManager.isEnabled = !actorChangeManager.isEnabled;
			}
			GUI.DragWindow();
		}

		public static List<string> logMessages = new List<string>();

		public static void LogMessage(string message)
		{
			logMessages.Add(message);
			if (logMessages.Count > 20) // Keep the list size manageable
			{
				logMessages.RemoveAt(0);
			}
		}

		public void OnGUI()
		{
			if (GUILayout.Button("menuMpTest", GUILayout.MaxWidth(200f)))
			{
				showWindow = !showWindow;
			}

			if (showWindow)
			{
				controlWindowRect = GUILayout.Window(13364, controlWindowRect, controlWindow, "c", GUILayout.MaxWidth(300f), GUILayout.MinWidth(200f));
			}

			// Button to toggle log window
			if (GUILayout.Button("Show Logs"))
			{
				showLogWindow = !showLogWindow;
			}

			if (showLogWindow)
			{
				logWindowRect = GUILayout.Window(13365, logWindowRect, logWindow, "Log Messages", GUILayout.MaxWidth(300f), GUILayout.MinWidth(200f));
			}
		}

		public bool showLogWindow = false;
		public Rect logWindowRect = new Rect(10f, Screen.height - 300f, 300f, 200f);

		public void logWindow(int windowID)
		{
			foreach (string message in logMessages)
			{
				GUILayout.Label(message);
			}
			GUI.DragWindow();
		}


		public bool showWindow;
		public Rect controlWindowRect = new Rect(126f, 1f, 1f, 1f);
		public static string nameText = "";
		public string messageText = "";
	}


}

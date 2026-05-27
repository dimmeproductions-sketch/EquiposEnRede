using Unity.Netcode;
using UnityEngine;


public class CodeManager : MonoBehaviour
{
    private NetworkManager m_NetworkManager;

    private float m_GlobalEffectTimer = 0f;
    private float m_EffectDurationTimer = 0f;
    private bool m_IsEffectActive = false;
    private CodePlayer m_SelectedPlayer = null;
    public static int MaxPlayersPerTeam = 2;

    private void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();

            if (GUILayout.Button("Mover a inicio"))
            {
                if (m_NetworkManager.IsServer)
                {
                    // Si hace clic el Servidor/Host, recorre directamente a TODOS los jugadores
                    foreach (var client in m_NetworkManager.ConnectedClientsList)
                    {
                        if (client.PlayerObject != null)
                        {
                            var player = client.PlayerObject.GetComponent<CodePlayer>();
                            if (player != null) player.MoveToStart();
                        }
                    }
                }
                else if (m_NetworkManager.IsClient)
                {
                    // Si hace clic un cliente normal, le pide al servidor moverse solo a sí mismo
                    var localPlayer = m_NetworkManager.SpawnManager.GetLocalPlayerObject()?.GetComponent<CodePlayer>();
                    if (localPlayer != null)
                    {
                        localPlayer.RequestMoveToStartServerRpc();
                    }
                }
            }
        }

        GUILayout.EndArea();
    }

    private void StartButtons()
    {
        if (GUILayout.Button("Host")) m_NetworkManager.StartHost();
        if (GUILayout.Button("Client")) m_NetworkManager.StartClient();
        if (GUILayout.Button("Server")) m_NetworkManager.StartServer();
    }

    private void StatusLabels()
    {
        var mode = m_NetworkManager.IsHost ?
            "Host" : m_NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " + m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
        GUILayout.Label($"Jugadores: {m_NetworkManager.ConnectedClientsIds.Count}");

        if (m_NetworkManager.IsServer)
        {
            GUILayout.Space(5);
            GUILayout.Label($"Límite Máx por Equipo: {MaxPlayersPerTeam}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-"))
            {
                if (MaxPlayersPerTeam > 1)
                {
                    MaxPlayersPerTeam--;
                    SyncMaxPlayersValue(MaxPlayersPerTeam);
                }
            }
            if (GUILayout.Button("+"))
            {
                MaxPlayersPerTeam++;
                SyncMaxPlayersValue(MaxPlayersPerTeam);
            }
            GUILayout.EndHorizontal();
        }
    }

    private void SyncMaxPlayersValue(int value)
    {
        foreach (var client in m_NetworkManager.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var p = client.PlayerObject.GetComponent<CodePlayer>();
                if (p != null) p.UpdateMaxPlayersRpc(value);
            }
        }
    }

    private void Update()
    {
        // Solo el servidor lleva la cuenta del tiempo global
        if (m_NetworkManager == null || !m_NetworkManager.IsServer) return;
    }
}
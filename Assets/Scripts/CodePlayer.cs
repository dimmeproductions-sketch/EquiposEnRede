using Unity.Netcode;
using UnityEngine;


public class CodePlayer : NetworkBehaviour
{
    public NetworkVariable<int> CurrentMode = new NetworkVariable<int>(0); // 0: Server, 1: Rewind, 2: Client
    public NetworkVariable<Vector3> CustomPosition = new NetworkVariable<Vector3>();

    private Unity.Netcode.Components.NetworkTransform m_NetTransform;
    private float m_ClientVerticalVelocity = 0f;
    private bool m_ClientJumpRequested;

    private float m_VerticalVelocity = 0f;
    private Vector2 m_ServerInput;
    private bool m_JumpRequested;
    private Vector2 m_LastInput;
    private float mapLimitX = 5f; // Límite en el eje X (izquierda/derecha)
    private float mapLimitZ = 5f; // Límite en el eje Z (arriba/abajo)

    private float m_BaseSpeed = 5f;
    private Renderer m_Renderer;
    public float currentSpeed;

    public NetworkVariable<int> CurrentTeam = new NetworkVariable<int>(0); // 0: Centro, 1: Eq1, 2: Eq2
    public NetworkVariable<Color> CurrentTeamColor = new NetworkVariable<Color>(Color.white);

    private static Color[] Team1Colors = new Color[] { Color.red, new Color(1f, 0.5f, 0f), new Color(1f, 0.4f, 0.7f) }; // Rojo, Naranja, Rosa
    private static Color[] Team2Colors = new Color[] { new Color(0f, 0f, 0.5f), new Color(0.5f, 0f, 0.5f), Color.cyan }; // Azul oscuro, Violeta, Azul claro

    private void Awake()
    {
        m_Renderer = GetComponent<Renderer>();
        m_NetTransform = GetComponent<Unity.Netcode.Components.NetworkTransform>();
    }

    public override void OnNetworkSpawn()
    {
        CurrentTeamColor.OnValueChanged += OnTeamColorChanged;
        ApplyTeamColor(CurrentTeamColor.Value);

        if (IsServer)
        {
            MoveToStart();
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentTeamColor.OnValueChanged -= OnTeamColorChanged;
    }

    private void OnTeamColorChanged(Color previousValue, Color newValue)
    {
        ApplyTeamColor(newValue);
    }

    private void ApplyTeamColor(Color newColor)
    {
        if (m_Renderer != null)
        {
            m_Renderer.material.color = newColor;
        }
    }

    [Rpc(SendTo.Server)]
    private void SendInputServerRpc(Vector2 input, bool jump)
    {
        m_ServerInput = input;
        if (jump) m_JumpRequested = true;
    }

    [Rpc(SendTo.Server)]
    public void CycleModeServerRpc()
    {
        CurrentMode.Value = (CurrentMode.Value + 1) % 3; // Cicla entre 0, 1 y 2
    }

    [Rpc(SendTo.Server)]
    private void UpdateClientAuthorityPositionServerRpc(Vector3 clientPos)
    {
        transform.position = clientPos;
        CustomPosition.Value = transform.position;
    }

    [Rpc(SendTo.Server)]
    public void RequestMoveToStartServerRpc()
    {
        // El servidor ejecuta la acción para el cliente que la solicitó
        MoveToStart();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void UpdateMaxPlayersRpc(int newValue)
    {
        CodeManager.MaxPlayersPerTeam = newValue;
    }

    private void SimulateMovement(Vector2 input, ref bool jumpRequested, ref float verticalVelocity)
    {
        if (IsAnyTeamFull() && !AmIInTheFullTeam())
        {
            return; // Detiene la ejecución del movimiento inmediatamente
        }

        currentSpeed = m_BaseSpeed;

        Vector3 moveDir = new Vector3(input.x, 0, input.y).normalized;
        transform.Translate(moveDir * (currentSpeed * Time.deltaTime), Space.World);

        if (transform.position.y > 1f || jumpRequested)
        {
            if (jumpRequested && transform.position.y <= 1.05f)
            {
                verticalVelocity = 5f;
                jumpRequested = false;
            }
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            transform.Translate(Vector3.up * (verticalVelocity * Time.deltaTime), Space.World);

            if (transform.position.y < 1f)
            {
                transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
                verticalVelocity = 0f;
            }
        }
        else
        {
            jumpRequested = false;
        }

        // Límites del plano
        float clampedX = Mathf.Clamp(transform.position.x, -mapLimitX, mapLimitX);
        float clampedZ = Mathf.Clamp(transform.position.z, -mapLimitZ, mapLimitZ);
        transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
    }

    public void MoveToStart()
    {
        // Posición aleatoria en la plataforma central blanca (Eje X)
        float randomX = Random.Range(-2f, 2f);
        float randomZ = Random.Range(-4f, 4f); 
        transform.position = new Vector3(randomX, 1f, randomZ);

        // Sincronizamos la posición oficial de red del código base
        CustomPosition.Value = transform.position;

        // Reseteamos las velocidades físicas para evitar tirones
        m_VerticalVelocity = 0f;
        m_ClientVerticalVelocity = 0f;
    }

    private void HandleTeamZonesServer()
    {
        // 1. Detectar zona según coordenada Z
        int detectedTeam = 0; // 0 = Centro (Plataforma Blanca)
        if (transform.position.x > 2.5f) detectedTeam = 1; // Plataforma Derecha (Roja)
        else if (transform.position.x < -2.5f) detectedTeam = 2; // Plataforma Izquierda (Azul)

        // 2. Si el jugador cambió de zona/equipo, recalculamos
        if (CurrentTeam.Value != detectedTeam)
        {
            CurrentTeam.Value = detectedTeam;

            if (detectedTeam == 0)
            {
                // Volver al centro => Blanco
                CurrentTeamColor.Value = Color.white;
            }
            else
            {
                // Averiguar qué colores ya están ocupados por otros jugadores en el mismo equipo
                System.Collections.Generic.HashSet<Color> occupiedColors = new System.Collections.Generic.HashSet<Color>();

                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    if (client.PlayerObject != null)
                    {
                        CodePlayer other = client.PlayerObject.GetComponent<CodePlayer>();
                        // Si es otro jugador y está en mi mismo equipo asignado, guardo su color
                        if (other != null && other != this && other.CurrentTeam.Value == detectedTeam)
                        {
                            occupiedColors.Add(other.CurrentTeamColor.Value);
                        }
                    }
                }

                // Elegir la paleta que le corresponde
                Color[] palette = (detectedTeam == 1) ? Team1Colors : Team2Colors;

                // Filtrar cuáles de esa paleta están libres
                System.Collections.Generic.List<Color> freeColors = new System.Collections.Generic.List<Color>();
                foreach (Color c in palette)
                {
                    if (!occupiedColors.Contains(c))
                    {
                        freeColors.Add(c);
                    }
                }

                // Asignar color aleatorio de entre los que queden libres
                if (freeColors.Count > 0)
                {
                    CurrentTeamColor.Value = freeColors[Random.Range(0, freeColors.Count)];
                }
                else
                {
                    // Salvaguarda por si el equipo se llena por encima de la paleta (repite uno al azar)
                    CurrentTeamColor.Value = palette[Random.Range(0, palette.Length)];
                }
            }
        }
    }

    private bool IsAnyTeamFull()
    {
        int t1Count = 0;
        int t2Count = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var p = client.PlayerObject.GetComponent<CodePlayer>();
                if (p != null)
                {
                    if (p.CurrentTeam.Value == 1) t1Count++;
                    if (p.CurrentTeam.Value == 2) t2Count++;
                }
            }
        }
        return (t1Count >= CodeManager.MaxPlayersPerTeam || t2Count >= CodeManager.MaxPlayersPerTeam);
    }

    private bool AmIInTheFullTeam()
    {
        int t1Count = 0;
        int t2Count = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var p = client.PlayerObject.GetComponent<CodePlayer>();
                if (p != null)
                {
                    if (p.CurrentTeam.Value == 1) t1Count++;
                    if (p.CurrentTeam.Value == 2) t2Count++;
                }
            }
        }
        if (t1Count >= CodeManager.MaxPlayersPerTeam && CurrentTeam.Value == 1) return true;
        if (t2Count >= CodeManager.MaxPlayersPerTeam && CurrentTeam.Value == 2) return true;
        return false;
    }

    public void CheckTeamLimitsAndNotify()
    {
        int t1Count = 0;
        int t2Count = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var p = client.PlayerObject.GetComponent<CodePlayer>();
                if (p != null)
                {
                    if (p.CurrentTeam.Value == 1) t1Count++;
                    if (p.CurrentTeam.Value == 2) t2Count++;
                }
            }
        }

        if (t1Count >= CodeManager.MaxPlayersPerTeam)
            NotifyFreezeClientRpc("¡Equipo 1 Lleno! Los demás jugadores han sido congelados.");
        else if (t2Count >= CodeManager.MaxPlayersPerTeam)
            NotifyFreezeClientRpc("¡Equipo 2 Lleno! Los demás jugadores han sido congelados.");
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyFreezeClientRpc(string message)
    {
        // Muestra el aviso en la consola de todos los usuarios afectados
        Debug.LogWarning(message);
    }

    private void Update()
    {
        // Activamos NetworkTransform solo en el Modo 0 (Server Auth estándar)
        if (m_NetTransform != null)
        {
            m_NetTransform.enabled = (CurrentMode.Value == 0);
        }

        // 1. EL DUEÑO (Owner) procesa inputs y predicciones locales
        if (IsOwner)
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            bool jump = Input.GetButtonDown("Jump");

            if (Input.GetKeyDown(KeyCode.M))
            {
                if (IsServer)
                {
                    // Si la pulsa el Servidor/Host, mueve a todo el mundo
                    foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                    {
                        if (client.PlayerObject != null)
                        {
                            var player = client.PlayerObject.GetComponent<CodePlayer>();
                            if (player != null) player.MoveToStart();
                        }
                    }
                }
                else
                {
                    // Si la pulsa un cliente, solo se lo pide para sí mismo
                    RequestMoveToStartServerRpc();
                }
            }

            if (IsServer)
            {
                m_ServerInput = input;
                if (jump) m_JumpRequested = true;
            }
            else if (input != m_LastInput || jump)
            {
                SendInputServerRpc(input, jump);
                m_LastInput = input;
            }

            // MODO 1: Autoridad en Servidor con Rewind (Predicción en Cliente)
            if (CurrentMode.Value == 1)
            {
                if (jump) m_ClientJumpRequested = true;
                SimulateMovement(input, ref m_ClientJumpRequested, ref m_ClientVerticalVelocity);

                // REWIND / RECONCILIATION: Si nos alejamos demasiado de la verdad del servidor, rebobinamos
                if (Vector3.Distance(transform.position, CustomPosition.Value) > 0.3f)
                {
                    transform.position = CustomPosition.Value;
                }
            }
            // MODO 2: Autoridad en el Cliente
            else if (CurrentMode.Value == 2)
            {
                if (jump) m_ClientJumpRequested = true;
                SimulateMovement(input, ref m_ClientJumpRequested, ref m_ClientVerticalVelocity);
                UpdateClientAuthorityPositionServerRpc(transform.position); // El cliente impone su posición
            }
        }

        // 2. EL SERVIDOR procesa la autoridad para los Modos 0 y 1
        if (IsServer)
        {
            if (CurrentMode.Value == 0 || CurrentMode.Value == 1)
            {
                SimulateMovement(m_ServerInput, ref m_JumpRequested, ref m_VerticalVelocity);
                CustomPosition.Value = transform.position; // Guardamos la posición oficial
            }

            HandleTeamZonesServer();

            if (CurrentMode.Value == 0 || CurrentMode.Value == 1)
            {
            }

            // 3. REMOTOS (Visualización de movimientos en Modos 1 y 2 para otros jugadores)
            if (!IsOwner && CurrentMode.Value != 0)
            {
                transform.position = CustomPosition.Value;
            }
        }
    }
}
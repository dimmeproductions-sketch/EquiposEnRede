# Informe

### 2. 

Creación de la escena 3D con un plano y el prefab del jugador en forma de cápsula. El movimiento se desarrolla mediante el método SimulateMovement, calculando la dirección con los ejes verticales/horizontales y aplicando gravedad para el salto. La sincronización de posiciones se realiza de forma óptima con el componente de Unity NetworkTransform, activo en el modo estándar del juego.

# Código de referencia:
CodePlayer.cs ➔ Líneas 98 a 135 (Método SimulateMovement) y Líneas 286 a 290 (Control de componente).

# Captura
![ ](./Assets/Screenshots/1.png)

### 3. 

Implementación del método MoveToStart, que posiciona al jugador en coordenadas aleatorias de la plataforma central y resetea sus velocidades físicas. Siguiendo las directrices de Netcode, si la acción la ejecuta el Servidor (vía UI o tecla 'M'), se fuerza el reposicionamiento de todos los clientes conectados de manera directa; si la ejecuta un Cliente, este realiza una petición segura enviando un RequestMoveToStartServerRpc para desplazarse solo a sí mismo.

# Código de referencia:
CodeManager.cs ➔ Líneas 30 a 53 (Lógica del botón en OnGUI).  
CodePlayer.cs ➔ Líneas 138 a 151 (Método MoveToStart) y Líneas 299 a 318 (Detección de la tecla 'M').

# Capturas
La captura 1 es el estado de ejemplo
Captura 2, si se pulsa la M o el botón de Mover a inicio desde el *servidor*, se moveran a la zona inicial todos los jugadores
Captura 2, si se pulsa la M o el botón de Mover a inicio desde el *cliente*, solo se moverá a si mismo, como en la pestaña superpuesta
![ ](./Assets/Screenshots/2.png) ![ ](./Assets/Screenshots/3.png)![ ](./Assets/Screenshots/4.png)

### 4. 

El servidor monitoriza de forma constante la posición del jugador en el plano mediante la función HandleTeamZonesServer. Al iniciar la simulación o regresar al centro, el jugador se vincula al equipo 0 (color blanco). Si la coordenada X supera los umbrales laterales (X > 2.5 o X < -2.5), el servidor detecta el cambio de zona, actualiza la NetworkVariable del equipo y propaga de forma automática la actualización de material a todos los clientes a través del callback OnValueChanged.

# Código de referencia:
CodePlayer.cs ➔ Líneas 37 a 46 (Suscripción en OnNetworkSpawn), Líneas 53 a 64 (Métodos de aplicación de color) y Líneas 153 a 170 (Detección de zona central y colores base).

# Captura
![ ](./Assets/Screenshots/5.png) ![ ](./Assets/Screenshots/6.png)

### 5. 

Control dinámico del aforo modular utilizando la variable estática MaxPlayersPerTeam de forma escalable. Dentro del método SimulateMovement, se realiza una comprobación previa mediante IsAnyTeamFull() y AmIInTheFullTeam(). Si un equipo ha alcanzado su límite de capacidad, se ejecuta un early return que detiene por completo las físicas y el movimiento de cualquier jugador que intente acceder desde el exterior, alertándolo además a través de la consola mediante un ClientRpc.

# Código de referencia:
CodePlayer.cs ➔ Líneas 100 a 104 (Restricción en SimulateMovement), Líneas 215 a 253 (Métodos de validación de aforo) y Líneas 278 a 283 (NotifyFreezeClientRpc).

### 6. 

Definición de arrays estáticos con las paletas de colores obligatorias asignadas a cada equipo en las variables Team1Colors y Team2Colors. Al ingresar a un extremo del plano, el servidor crea una estructura de tipo HashSet para examinar qué tonalidades de la paleta ya están asignadas a los compañeros actuales. El algoritmo descarta las ocupadas y asigna un color aleatorio entre las opciones libres remanentes, garantizando que no se repitan colores dentro del mismo equipo.

# Código de referencia:
CodePlayer.cs ➔ Líneas 28 a 29 (Declaración de paletas) y Líneas 171 a 211 (Filtrado y asignación aleatoria con HashSet dentro de HandleTeamZonesServer).

### 7. 

Diseño de la interfaz gráfica administrativa a través del ciclo OnGUI. Si la instancia local actúa con el rol de Servidor o Host, se renderizan de manera exclusiva los botones interactivos '+' y '-' junto al indicador de aforo. Al modificar este límite, se desencadena el método SyncMaxPlayersValue, el cual recorre la lista completa de clientes conectados e invoca un RPC para sincronizar y actualizar el nuevo parámetro global en tiempo real en todas las simulaciones de red.

# Código de referencia:
CodeManager.cs ➔ Líneas 76 a 95 (Dibujado de botones en StatusLabels) y Líneas 98 a 108 (Método SyncMaxPlayersValue).  
CodePlayer.cs ➔ Líneas 93 a 97 (Función remota UpdateMaxPlayersRpc).

# Captura
La captura muestra Unity de fondo actuando en modo servidor donde se puede observar los botones de limitacion de equipo mientras que el cliente (ventana flotante delante) soo muestra la opcion de mover a inicio
![ ](./Assets/Screenshots/7.png)
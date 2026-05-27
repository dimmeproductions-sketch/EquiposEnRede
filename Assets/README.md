# E2.0 Exame: equipos en rede

(1 punto) Uso axeitado de variables, métodos, orde, eficiencia e calidade do código. Realización dun pequeno informe con capturas dos pasos e brevísimas explicacións para cada apartado.
(2 puntos) Crea desde cero un xogo 3D semellante ao getting started do manual de netcode. Isto é, un xogo no que hai un plano e sobre él os playeres, que serán unha cápsula que se move nas catro direccións sobre o plano utilizando teclas (frechas, ASDW ou ambas). Debes usar Network transform.
(1 punto) Fai que cando se spawnea apareza nun punto aleatorio da parte central do plano (a que está etiquetada como "sen equipo" no esquema de abaixo) e de cor branca. Crea un botón chamado "mover a inicio" que leve ao player á parte central do plano, fai que a tecla "m" teña a mesma acción. Se estamos executando en servidor esa acción debe ser feita por todos os players.
(1 punto) Os players sen equipo (os que están na parte central do plano) teñen cor branca. Se o player se move para a zona do Equipo 1 (ver esquema abaixo) poñerase de cor vermello e os players que se poñan no Equipo 2 collerán cor azul. Se volven á parte central volverán a estar sen equipo e por tanto volverán ser de cor branca.
(2 puntos) Fai que en cada equipo só poida haber como máximo dous players (os sen equipo non teñen limitación). E en caso de que un equipo esté cheo, só se poderán mover os players dese equipo e os outros so poderán moverse de novo cando o equipo non esté cheo (utiliza ClientRPC para avisalos). Prográmao de tal xeito que poidas mudar facilmente o número máximo de players nun equipo no futuro. 
(2 puntos) Fai que o equipo 1 teña as cores vermella, laranxa e rosa, e o equipo 2 teñas as cores azul escuro, violeta e azul claro. Cando un xogador entre nun equipo collerá unha das cores aleatorias que non estén collidas. 
(1 punto) Fai que no servidor apareza unha interface para seleccionar o número de players máximos de cada equipo.
Entregables:

Documento do informe
Código do Player e do Manager
Link ao repositorio co código completo
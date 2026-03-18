# Game Jam Colombia 5.0 - 2026
### Inserte nombre epico del juego

#### Creado por: Inserte nombre épico del equipo
Juan S Marin
Daniel Betancourt
Nicole Livingston
Sofia Caraballo
Juliana Monroy
Jacobo Rodríguez

🎮 Sistema de Minijuegos – Diseño General
🧩 Descripción General

El juego consiste en una serie de minijuegos cortos conectados entre sí mediante una progresión lineal (modo historia) o repetitiva con dificultad creciente (modo infinito).

Todos los minijuegos comparten las siguientes características:

⏱ Tienen un límite de tiempo

✅ Se pueden ganar o perder

💸 Perder implica una penalización económica, variable según el minijuego

🎬 Incluyen una animación corta de transición antes de pasar al siguiente

🖱 Interacciones simples:

Click

Click sostenido

Movimiento básico

🎮 Modos de Juego
🧭 Modo Historia

Secuencia lineal de minijuegos

Cada minijuego se juega una única vez

Progresión narrativa definida

♾️ Modo Infinito

Repetición de los minijuegos en el mismo orden

Incremento progresivo de la velocidad/dificultad

Basado en la justificación narrativa:

🚢 El jugador está en un crucero (hotel móvil) que viaja entre costas.
Si falla, el crucero puede continuar sin él → pérdida.

🧳 Sistema de Progresión

Al finalizar la partida:

🛍 Se accede a una tienda

Se puede gastar el dinero restante del viaje

Permite reintentar con un personaje personalizado

🎯 Minijuegos
🚗 1. Carretera Infinita

Conducción en carretera infinita

Objetivo: sobrevivir hasta que termine el timer

Transición:

Llegada a aeropuerto (fade in)

Animación del personaje saludando

✈️ 2. Aeropuerto (Salida)

Personaje corre en un entorno infinito

Mecánica:

Saltar obstáculos (maletas)

Input: click o tecla

Objetivo: no perder el vuelo

Final:

Animación de despegue del avión

🛄 3. Equipaje (Llegada)

Seleccionar tu maleta entre varias en movimiento

Algunas maletas son similares

Mecánica:

Observación + memoria visual

🏨 4. Identificar al Host del Hotel

El jugador memoriza un logo/imagen del host

Luego debe identificarlo entre varias personas

Variantes:

👁 “Encuentra a Wally” (selección directa)

🖐 Drag & Drop (recomendado):

Mover personajes/objetos

El host está oculto detrás

🛎 5. Check-in del Hotel

El recepcionista se distrae con el celular

Mecánica:

Presionar la campana cuando se distrae

Timing-based

🌴 Minijuegos Opcionales / Aleatorios (Playa)
🛍 6. Compras en la Playa

Vendedores ofrecen productos:

Sombrillas

Cocos

Gafas

Souvenirs

Mecánica:

Comparación de precios

Elegir la opción más barata

⚠️ Riesgos:

Sed

Insolación

Distracción

Masajes → Game Over

☀️ 7. Evitar el Sol

El jugador debe moverse entre zonas de sombra

Restricción:

No permanecer más de X segundos al sol

Mecánica:

Movimiento simple

🔁 Flujo General del Juego

Inicio

Secuencia de minijuegos

Transiciones animadas entre cada uno

Resultado final:

Dinero restante

Acceso a tienda

Reintento (loop)

🧠 Notas de Diseño

Se prioriza:

Simplicidad mecánica

Ritmo rápido

Claridad visual

Ideal para:

Experiencias casuales

Gameplay tipo WarioWare

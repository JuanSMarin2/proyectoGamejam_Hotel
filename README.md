# Game Jam Colombia 5.0 - 2026
### Inserte nombre epico del juego

#### Creado por: Inserte nombre épico del equipo
Juan S Marin
Daniel Betancourt
Nicole Livingston
Sofia Caraballo
Juliana Monroy
Jacobo Rodríguez

# 🎮 Sistema de Minijuegos – Diseño General

## 🧩 Descripción General

El juego consiste en una serie de minijuegos cortos conectados entre sí mediante una progresión lineal (modo historia) o repetitiva con dificultad creciente (modo infinito).

Todos los minijuegos comparten las siguientes características:

- ⏱ Tienen un **límite de tiempo**
- ✅ Se pueden **ganar o perder**
- 💸 Perder implica una **penalización económica**, variable según el minijuego
- 🎬 Incluyen una **animación corta de transición** antes de pasar al siguiente
- 🖱 Interacciones simples:
  - Click
  - Click sostenido
  - Movimiento básico

---

## 🎮 Modos de Juego

### 🧭 Modo Historia
- Secuencia lineal de minijuegos
- Cada minijuego se juega **una única vez**
- Progresión narrativa definida

### ♾️ Modo Infinito
- Repetición de los minijuegos en el mismo orden
- Incremento progresivo de la **velocidad/dificultad**

**Justificación narrativa:**

> 🚢 El jugador está en un **crucero (hotel móvil)** que viaja entre costas.  
> Si falla, el crucero puede continuar sin él → pérdida.

---

## 🧳 Sistema de Progresión

- Al finalizar la partida:
  - 🛍 Se accede a una **tienda**
  - Se puede gastar el dinero restante del viaje
  - Permite **reintentar con un personaje personalizado**

---

## 🎯 Minijuegos

### 🚗 1. Carretera Infinita
- Conducción en carretera infinita
- Objetivo: sobrevivir hasta que termine el timer

**Transición:**
- Llegada a aeropuerto (fade in)
- Animación del personaje saludando

---

### ✈️ 2. Aeropuerto (Salida)
- Personaje corre en un entorno infinito

**Mecánica:**
- Saltar obstáculos (maletas)
- Input: click o tecla

**Objetivo:**
- No perder el vuelo

**Final:**
- Animación de despegue del avión

---

### 🛄 3. Equipaje (Llegada)
- Seleccionar tu maleta entre varias en movimiento
- Algunas maletas son similares

**Mecánica:**
- Observación + memoria visual

---

### 🏨 4. Identificar al Host del Hotel
- El jugador memoriza un logo/imagen del host
- Luego debe identificarlo entre varias personas

**Variantes:**
- 👁 “Encuentra a Wally” (selección directa)
- 🖐 Drag & Drop (recomendado):
  - Mover personajes/objetos
  - El host está oculto detrás

---

### 🛎 5. Check-in del Hotel
- El recepcionista se distrae con el celular

**Mecánica:**
- Presionar la campana cuando se distrae

**Tipo:**
- Timing-based

---

## 🌴 Minijuegos Opcionales / Aleatorios (Playa)

### 🛍 6. Compras en la Playa
- Vendedores ofrecen productos:
  - Sombrillas
  - Cocos
  - Gafas
  - Souvenirs

**Mecánica:**
- Comparación de precios
- Elegir la opción más barata

⚠️ **Riesgos:**
- Sed
- Insolación
- Distracción
- Masajes → Game Over

---

### ☀️ 7. Evitar el Sol
- El jugador debe moverse entre zonas de sombra

**Restricción:**
- No permanecer más de **X segundos al sol**

**Mecánica:**
- Movimiento simple

---

## 🔁 Flujo General del Juego

1. Inicio  
2. Secuencia de minijuegos  
3. Transiciones animadas entre cada uno  
4. Resultado final:
   - Dinero restante
   - Acceso a tienda  
5. Reintento (loop)

---

## 🧠 Notas de Diseño

- Se prioriza:
  - Simplicidad mecánica
  - Ritmo rápido
  - Claridad visual

- Referencia:
  - Gameplay tipo *WarioWare*

---

## 🔍 Feedback y Validación

### 📌 Estructura aplicada
- Agrupación por sistemas (modos, mecánicas, minijuegos)
- Separación entre intención y ejecución
- Jerarquía de información clara

### ⚠️ Supuestos
- Todos los minijuegos comparten sistema económico
- El orden de minijuegos es fijo
- Las animaciones son transiciones, no gameplay activo

### 🚨 Riesgos de diseño
- Monotonía en modo infinito si solo aumenta velocidad
- Desbalance económico (castigos excesivos)
- Minijuegos de memoria percibidos como injustos

### 💡 Recomendaciones
- Añadir variaciones dentro de cada minijuego
- Mejorar feedback visual/sonoro
- Ajustar curva de dificultad progresiva

### 🧪 Validación
- Prototipo rápido en Unity (graybox)
- Playtesting con 3–5 usuarios

**Métricas clave:**
- % de éxito por minijuego
- Tiempo de reacción promedio
- Puntos de abandono del jugador

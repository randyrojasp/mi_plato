# Mi Plato Tico

App web para una dinamica de clase: estudiantes compran alimentos con presupuesto en colones, arman un plato y reciben puntaje segun salud, balance y nivel de procesados.

## Stack

- Frontend: Vue + Tailwind + Vite
- Backend: ASP.NET Core Minimal API
- Base de datos: SQLite
- Despliegue: Docker Compose + Nginx

## Ejecutar En Desarrollo

Backend:

```bash
dotnet run --project backend/Api --urls http://localhost:5080
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```

Abre `http://localhost:5173`.

## Ejecutar Con Docker

```bash
docker compose up --build
```

Abre `http://localhost:8081`.

La base SQLite queda guardada en el volumen `plato-data`.

## Flujo De Uso

1. El profesor crea una sala.
2. Comparte el codigo de sala.
3. Cada estudiante entra con su nombre y el codigo.
4. Compra alimentos y evalua su plato.
5. El ranking muestra puntaje, rondas y monedas en colones.

## Endpoints Principales

- `POST /api/rooms`: crea una sala.
- `GET /api/rooms/{code}`: obtiene sala.
- `POST /api/players`: registra jugador.
- `GET /api/products`: lista alimentos.
- `POST /api/games/submit`: evalua plato.
- `GET /api/rooms/{code}/ranking`: ranking de la sala.
- `POST /api/rooms/{code}/reset`: reinicia jugadores y partidas de una sala.

## VPS

En una VPS, puedes poner Nginx o Caddy delante del puerto `8081` y activar HTTPS con tu dominio.

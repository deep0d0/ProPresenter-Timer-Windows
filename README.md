# ProPresenter & Resolume Countdown App — Windows

A native Windows app that displays live countdown timers from **ProPresenter** and/or **Resolume** on a fullscreen display.

## Features

- 🎨 **Liquid glass-like UI** — frosted glass timer cards on a dark/light gradient background
- ⏱ **Animated countdown digits** — smooth rolling number transitions
- 🔴 **Urgency colours**
  - White → normal
  - 🟡 Amber → last 30 seconds
  - 🔴 Red + pulsing glow → last 10 seconds
- ⚙️ **Settings sheet** — opens on first launch; always accessible via the gear button
- 🌗 **Dark & Light mode** — respects system appearance

## Requirements

- Windows 11

## Configuration

On first launch (or when the gear ⚙ button is clicked):

| Field | Description |
|---|---|
| Center Screen IP | Base URL of ProPresenter or Resolume, e.g. `http://192.168.1.105:8091` |
| Center Screen Source | `ProPresenter` or `Resolume` |
| Side Screen IP | Base URL of ProPresenter, e.g. `http://192.168.1.105:1781` |
| Timeout (ms) | HTTP request timeout (default: 1000ms) |

Settings are saved automatically via `UserDefaults`.

## API Endpoints Used

| App | Endpoint |
|---|---|
| ProPresenter | `GET /v1/timer/video_countdown` |
| Resolume | `GET /api/v1/composition/clips/selected` |

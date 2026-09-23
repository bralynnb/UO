# The Strangest Flea Market — Unity browser game

Repository: [bralynnb/UO](https://github.com/bralynnb/UO).

## Upload status

This repository currently contains the Unity build setup, browser connection infrastructure, package configuration, and deployment scaffolding. The complete game source and market content are prepared separately and await approval for publication in this public repository.

**This is not yet a complete or playable checkout.** The city scene, gameplay server, interface, and content data have not been uploaded. There is no public game URL.

## Prepared infrastructure

- Unity 6000.0.62f1 project configuration and Web build command.
- Browser WebSocket bridge and Unity connection client.
- Node.js 24 package configuration.
- Manual GitHub Actions Unity build workflow.
- Docker and HTTPS deployment configuration.

The build workflow requires the complete source and Unity activation secrets before it can succeed. Deployment instructions are in [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

## Intended game

The requested game is a browser multiplayer prototype with a New York city block, third-person exploration, NPC interactions, inventory, and guest entry. Implementation and testing status will be documented when the complete source is published.

# VrisingQoL

A BepInEx plugin for V Rising that adds quality-of-life features, including advanced respawn point management and blood mixer improvements.

## Features

- **Blood Mixer Extra Bottle**: Optionally receive a glass bottle back when mixing two blood potions.
- **Custom Respawn Points**: Admins (and optionally non-admins) can spawn and set custom respawn points.
- **Predefined Respawn Points**: Configure global respawn points via a JSON file.
- **Respawn Point Breakage**: Respawn points can be set to break after use.
- **Respawn Point Cost & Limits**: Control the cost and limit for non-admin respawn point creation.

## Configuration

All settings are available in the BepInEx config file:

| Setting | Description | Default |
|--------|-------------|---------|
| `enableBloodmixerExtraBottle` | Gives a glass bottle back when mixing 2 blood potions. | `true` |
| `enableRespawnPoint` | Enables admin respawn point spawning and setting. | `true` |
| `enableNonAdminRespawnPointSpawning` | Allows non-admins to spawn respawn points (limited). | `false` |
| `enablePredefinedRespawnPoints` | Enables predefined respawn points from a JSON file. | `true` |
| `enableRespawnPointBreakage` | Respawn points break after 1 use. | `false` |
| `respawnPointLimit` | Limit of respawn points for non-admins (`0` = unlimited). | `2` |
| `respawnPointCostAmount` | Amount of item required to spawn a respawn point. | `1` |
| `respawnPointCostItemPrefab` | Prefab ID of the required item. | `271594022` |
| `respawnPointPrefab` | Prefab ID of the respawn point. | `-55079755` |
| `respawnAnimationPrefab` | Prefab ID of the respawn animation. | `1290990039` |

## Predefined Respawn Points

If enabled, edit the file at  
`BepInEx\config\Respawns\respawns.json`  
to add global respawn point coordinates and rotations.

## Commands

- `.rps sp`  — Admin/User(If enabled in settings): Spawn a custom respawn point. 
- `.rsp set` — User: Set the nearest respawn point as your own if you own it.
- `.rsp rem` — User: Remove the nearest respawn point if you own it.

## Notes

- Make sure the respawn point prefab has a `BlueprintData` component.
- Avoid placing respawn points too close if breakage is enabled, as they may destroy each other.

## Requirements

- Required [BepInEx](https://thunderstore.io/c/v-rising/p/BepInEx/BepInExPack_V_Rising/)
- Optional [VampireCommandFramework](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/)
- Required [VAMP](https://thunderstore.io/c/v-rising/p/skytech6/VAMP/)

---

For more details, see the descriptions in the `VrisingQoL.cfg` file.
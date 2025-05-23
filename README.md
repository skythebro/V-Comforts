# V-Comforts

A V-Rising mod adding quality-of-life features like auto fishing, inventory stack size and per level stat bonuses, respawn point management and carriage tracking.

> **Disclaimer:** Some features in this mod are experimental or not thoroughly tested and may have bugs. Feedback and bug reports are highly appreciated! (suggestions are also welcome!)

## Features

- **Blood Mixer Extra Bottle**: Optionally receive a glass bottle back when mixing two blood potions.
- **Auto Fishing**: Automatically catch fish when a splash happens.
- **Level Bonus**: Gain stat bonuses based on your level.
- **Inventory Bonus**: Increase inventory stack size based on your equipped bag.
- **Custom Respawn Points**: Admins (and optionally non-admins) can spawn and set custom respawn points. (Very WIP only tested solo, so there can be team issues and such)
- **Predefined Respawn Points**: Configure global respawn points via a JSON file.
- **Respawn Point Breakage**: Respawn points can be set to break after use.
- **Respawn Point Cost & Limits**: Control the cost and limit for non-admin respawn point creation.
- **Carriage Tracking**: Carriages are tracked and shown on the map. (WIP, only works when a player gets close first, I will find a way around it, it will just take time)

## Configuration

All settings are available in the BepInEx config file:

| Section        | Setting                              | Description                                                                                            | Default                         |
|----------------|--------------------------------------|--------------------------------------------------------------------------------------------------------|---------------------------------|
| BloodMixer     | `enableBloodmixerExtraBottle`        | Gives a glass bottle back when mixing 2 blood potions.                                                 | `false`                         |
| Fishing        | `enableAutoFish`                     | Fish will automatically be caught whenever a splash happens.                                           | `false`                         |
| InventoryBonus | `enableInventoryBonus`               | Bonus to inventory stack size based on equipped bag.                                                   | `false`                         |
| InventoryBonus | `bagInventoryBonusMultiplier`        | Inventory stack size bonus multipliers per bag tier (max stack size clamped to 4095).                  | `1.05,1.10,1.15,1.20,1.25,1.30` |
| LevelBonus     | `enableLevelBonus`                   | Bonus to stats based on your level.                                                                    | `false`                         |
| LevelBonus     | `levelBonusMultiplier`               | Level bonus addition per level per stat: resourceYieldBonus, moveSpeedBonus, shapeshiftMoveSpeedBonus. | `0.005,0.003,0.0035`            |
| Respawn        | `enableRespawnPoint`                 | Admins can spawn and set custom respawn points.                                                        | `false`                         |
| Respawn        | `enableNonAdminRespawnPointSpawning` | Non-admins can also spawn respawn points (limited).                                                    | `false`                         |
| Respawn        | `enablePredefinedRespawnPoints`      | Enables predefined respawn points from a JSON file.                                                    | `false`                         |
| Respawn        | `enableRespawnPointBreakage`         | Respawn points break after 1 use.                                                                      | `false`                         |
| Respawn        | `respawnPointLimit`                  | Limit of respawn points for non-admins (`0` = unlimited).                                              | `1`                             |
| Respawn        | `respawnPointCostAmount`             | Amount of item required to spawn a respawn point.                                                      | `1`                             |
| Respawn        | `respawnPointCostItemPrefab`         | Prefab ID of the required item.                                                                        | `271594022`                     |
| Respawn        | `respawnPointPrefab`                 | Prefab ID of the respawn point.                                                                        | `-55079755`                     |
| Respawn        | `respawnAnimationPrefab`             | Prefab ID of the respawn animation.                                                                    | `1290990039`                    |
| Carriage       | `enableCarriageTracking`             | Carriages will be tracked and shown on the map.                                                        | `false`                         |

## Predefined Respawn Points

If enabled, edit the file at  
`BepInEx\config\Respawns\respawns.json`  
to add global respawn point coordinates and rotations.

## Commands

- `.rps sp`  — Admin/User(If enabled in settings): Spawn a custom respawn point.
- `.rsp set` — User: Set the nearest respawn point as your own if you own it.
- `.rsp rem` — User: Remove the nearest respawn point if you own it.

## Notes

- Make sure the respawn point prefab has a `BlueprintData` component. (causes error on client otherwise)
- Avoid placing respawn points too close if breakage is enabled, as they may destroy each other.
- Be sure to enable the features you want in the config!
## Requirements

- Required [BepInEx](https://thunderstore.io/c/v-rising/p/BepInEx/BepInExPack_V_Rising/)
- Optional [VampireCommandFramework](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/)
- Required [VAMP](https://thunderstore.io/c/v-rising/p/skytech6/VAMP/)

---

## Credits
Special thanks to:  
- [skytech6](https://ko-fi.com/skytech6) Creator of VAMP, whose framework made making this mod easier.
- [Odjit](https://github.com/odjit/) For Kindred Extract and other mods that inspired and helped development.
- [Vrising modding community](https://discord.gg/v-rising-mod-community-978094827830915092) — For the support and resources available.
---
### Issues
* If you encounter any issues, please report them on the [GitHub repository](https://github.com/skythebro/V-Comforts/issues)
* For more details, see the descriptions in the `VComforts.cfg` file.

### Extra features / todos?
* Servant item pickup?  
* check spawnpoint positions so they cannot be placed at bad positions?
* add customizable name to mapicon of respawnpoint?
* more customization for level stat increases?
* add more bonuses to bags?
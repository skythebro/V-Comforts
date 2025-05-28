# Changelog

## [0.1.0] - 2025-05-28
- Fixed potion stacking and moving issues.
- Fixed inventory stacking and splitting issues.
- Fixed missing enabled checks for certain options.
- Fixed level checks and streamlined inventory and level checks.
- Fixed values not resetting when disabling level or inventory mod.
- Fixed fishing issue? (I'm hoping this did it...)


## [0.0.4] - 2025-05-27

### Added
- Added custom blood potion sorting feature with two modes:
    - Primary type, then quality (default)
    - Primary only first, then secondary (by type, secondary type, and both qualities)
- Added option for players inventory to also have the custom blood potion sorting.
- Added configuration options for blood potion sorting.
- Added missing RESPAWN_POINT_LIMIT to commands, now limits will actually be adhered to.

### Changed
- Updated README with detailed explanation and images for blood potion sorting.

### Fixed
- Fixed bug where some consumables would not stack correctly to their new max stack size.

## [0.0.3] - 2025-05-26

### Fixed
- Fixed bug locking consumable stacking to 1.

## [0.0.2] - 2025-05-25

### Fixed
- Fixed fishing bug.
- Fixed blood potions stacking causing data to be deleted.
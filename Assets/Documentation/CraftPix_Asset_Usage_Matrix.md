# Echoes of Ember - CraftPix Asset Usage Matrix

This project uses every production-ready CraftPix pack supplied for the final assignment. Source duplicates (PSD/Tiled copies), coupons and store-preview images are deliberately excluded from runtime builds.

| CraftPix pack | Integrated role |
|---|---|
| Free Knight Character Sprites | Kael player; idle, walk, run, jump, three attacks, defend, hurt and death |
| Dungeon Platformer Tileset | Level 01 geometry, bridges, lava, door and chest visuals |
| Medieval Tileset | Campaign platforms and architectural variation |
| Crystal Cave Backgrounds | Level 02 Crystal Depths atmosphere |
| Fantasy 2D Battlegrounds | Unique layered atmosphere for Levels 01, 03, 04 and 05 |
| Skeleton Sprite Sheets | Warrior, archer and spearman enemy variants |
| Ghost Sprite Sheets | Onre, Yurei and Gotoku enemy variants |
| Fantasy Enemies | Skeleton, Plent and Fire Spirit variants; Ember Throne boss mix |
| Pixel Magic Effects | Animated visible Fireball, cast spark, trail and impact |
| Basic Pixel RPG UI | HUD, hotbar, modal frames, buttons, inventory and equipment presentation |
| Animated Magic Book | Spell Book and Quest Book presentation |
| 40 Loot Icons | Chests, Ember, equipment, potion and relic rewards |
| RPG Skill/Splash Icons | Skill and status presentation only, never used as projectiles |
| Dungeon Objects | Supplies, pedestals, traps and non-blocking environmental storytelling across all levels |

## Campaign progression
Level 01 Ember Ruins -> Level 02 Crystal Depths -> Level 03 Ashen Forge -> Level 04 Shadow Citadel -> Level 05 Ember Throne. Enemy health, speed and detection increase by stage; the final enemy is an 8 HP boss.

## Runtime exclusions
`COUPON.png`, previews, PSD duplicates and Tiled-source duplicates are documentation/source material and are not loaded at runtime. This prevents duplicated textures and unnecessary build size while retaining all actual game art.

# FoodScript - Food System & Inventory Mod

## Description

**FoodScript** is a comprehensive mod for GTA (GTA4/TLAD/TBOGT) that provides a complete inventory management system focused on storing and using various food items, valuables, and equipment. The mod includes an NPC looting system, a full-featured shop, crafting/combination mechanics, and management of different ability cores (Health Core, Deadeye Core, Gold Core).

With an intuitive menu interface, players can easily manage resources, upgrade backpacks, sell valuable items, and improve their character.

---

## Installation Guide

### Requirements
- GTA 4 (only tested on steam complete edition, but it should work on any version of GTA IV)
- GTA 4 .Net ScriptHook

### Installation Steps

1. **Install GTA 4 .Net ScriptHook**
   - Download + installation link (this is not my work): https://hazardx.com/files/gta4_net_scripthook-83

2. **Download and extract GTAIV_InventoryMod.rar**
   - Download GTAIV_InventoryMod.rar from release
   - Extract the file using winrar

3. **Put extracted folder into your GTA IV directory**
   - Right click on extracted folder, choose copy/cut
   - Go to your GTA IV, right click and choose paste

4. **Launch the game**
   - Run the game and the mod will load automatically
   - Use the default keybinds to open your inventory (see Features section)

---

## Features

### 1. **Comprehensive Inventory System**
   - Display all items organized by category
   - Upgrade inventory capacity with different backpack types (10 → 100 items)

### 2. **Item Categories**
   - **Consumables**: Drinks, sweets, meals, soups, etc. to restore health core
   - **Liquors/Cigarettes**: Beer, rum... to restore deadeye core, but decrease health core.
   - **Medkits**: Health restoration items
   - **Armor**: Body armor to increase character protection
   - **Valuables**: Gold, jewelry, watches, phones, etc.
   - **Backpacks**: Upgrade inventory capacity

### 3. **Looting System**
   - Loot items from dead NPC bodies
   - Different loot rates for all item category based on NPC type (Cops, Civilians, Paramedics, etc.)
   - Loot pickable items and world items

### 4. **Shop System**
   - Buy all types of items from the shop
   - Checkout with shopping cart and shipping fees
   - Sell valuable items to earn more money
   - Sell scrap materials

### 5. **Crafting/Combination System**
   - Combine items to create special food combos
   - Craft upgraded backpacks from materials
   - Display ingredient requirements and product effects

### 6. **Ability Cores System**
   - **Health Core** (0-100): Health regeneration overtime ability, divided into status, you get more health regeneration with higher status
   - **Deadeye Core** (0-100): Bullet time
   - **Gold Core** (special): Activated only when use combine consumable combos, prevent health core's drain
   - Penalty: Upon your character's death, both Health core and Deadeye core is decreased by 50

### 7. **Item Usage**
   - Each item has unique animations
   - Automatically restores Health/Armor/Deadeye
   - Support for complex animations with 3D item attachments
   - Cancel item use by attacking or jumping

### 8. **Auto-Save System**
   - Inventory data automatically saved to `.ini` files
   - Data reloads on game startup
   - Seperate save file for Gta 4, TBoGT, TLAD

### 9. **User Interface**
   - Simple text-based menu, easy to navigate
   - Detailed information display: price, quantity, type
   - Item status notifications (success, error, warning)
   - Color-coded system for categories

---

## Keybinds

 **Action keys**
| Key | Function |
|-----|----------|
| `X` | Loot NPC/take holding item |
| `H` | Hide/show health core and deadeye core bar|
| `Middle mouse button` | Activate deadeye |
| `L` | Search for lootable world items/Loot world items |

 **Inventory menu keys**
| Key | Function |
|-----|----------|
| `I` | Toggle inventory menu |
| `1-9` | Navigate menu options/Use items |
| `0` | Go back |
| `Left/Right arrow key` | Previous/next item page|

 **Shop menu keys**
| Key | Function |
|-----|----------|
| `B` | Toggle shop menu |
| `1-9` | Add/Remove items |
| `0` | Go back |
| `Backspace` | Toggle add/remove mode or sell valuables safely |
| `Enter` | Press at the main menu to confirm order | 

---

## Usage Guide

### Health core/Deadeye core
Health core and Deadeye core are two new player stats added by this mod

Health core determines how much health is restored every 3 seconds depending on the stat’s status, this stat decreases over time.. It has 5 status: Well fed (10HP), good (8HP), fair (5HP), hungry (3HP), starving (0HP) and 1 special status: golden health core prevent health core's draining. 

Deadeye core allows the player to use a special ability that slows down the game, making it easier to aim and shoot. This stat decreases over time while the ability is being used

Death penalty: Upon player's death, both stat decreases 50

### Inventory
Press `I` to toggle the inventory menu, use numberic buttons to navigate, the menu will pause the game. Inventory menu has nultiple pages for consumables, deadeye items, armor, medkits, valuables, backpacks, and crafting. By default, you have maximum 10 slots for each item, but you can upgrade for more capacity. 

#### Consumables
Consumables are foods/drinks that restore the player’s health core after used. Consumables are organized into several pages and have different restore values and prices. Several actions will cancel item's effect like aiming, shooting, jumping...

#### Liquors/cigarettes
Liquors/cigarettes is similar to consumables, items in this category restore your deadeye core but reduce player's health core. 

#### Armor 
Armor restore your armor amount, there are 3 type of armor: Full armor (100 armor), heavy armor (50 armor), light armor (20 armor). Armor has long use time and can be canceled

#### Medkit 
Medkits restore your HP, there are 2 type of medkit: used medkit (50 HP), Medkit (100 HP). Medkit has short use time and can be canceled

Medkits and armor can be crafted via the combine system.
Valuables and Selling
The script defines many valuables like gold rings, watches, phones, cameras, scrap, and more.
Valuables have price values and are used in shop selling menus.
There is also scrap and backpack material tracked as loot items.
Backpacks
Backpacks are actual inventory upgrades with item keys like small_backpack, normal_backpack, big_backpack, and magic_backpack.
Backpack materials can be collected and used to craft better backpacks.
Equipping a backpack increases capacity and is persistent across saves.
Crafting / Combining
Crafting recipes are stored in combineDatabase.
Food crafting combines consumables into more powerful meals like:
energy_pack, snack_duo, light_bite, hearty_meal, comfort_soup, etc.
Some crafted meals grant a temporary Gold Core effect, which changes health status and slows core drain.
Crafting also supports item upgrades:
combine partial armors into full armor
combine used medkits into full medkits
craft backpack items from backpack materials
Looting and World Interaction
The script scans nearby lootable objects and NPCs.
It shows a prompt to loot or rob when eligible.
Looted objects and NPCs can yield items, valuables, scrap, or backpack material.
There is a wanted/heat mechanic: crime witnessed may trigger wanted level later.
HUD and Status
The script draws:
health core bar
deadeye core bar
status text such as “Well Fed”, “Hungry”, “Starving”
current backpack name
It shows temporary messages, loot prompts, and item hash debug info.

---

## 📦 File Structure

```
food/
├── source/
│   └── FoodScript.cs          # Main mod source code
├── scripts/
│   ├── FoodScriptData.ini
│   ├── FoodScriptDataGta4.ini
│   ├── FoodScriptDataTLAD.ini
│   └── FoodScriptDataTbogt.ini
└── README.md                   # This file
```

---

## 🐛 Troubleshooting

**Error: Inventory won't open**
- Check that `I` key is not conflicting with other binds
- Ensure game is not in a cut-scene

**Error: Animation won't play**
- Check character is not in a vehicle
- Ensure animation set is loaded correctly

**Error: Items won't loot**
- NPC may have already been looted
- Check inventory is not full

---

## 📝 Author & License

**Developed by:** [Author Name]  
**License:** [Specify your license - MIT, GPL, etc.]

---

## 🤝 Contributing

All contributions, bug reports, and feature suggestions are welcome!
Please create an issue or pull request on GitHub.

---

## ⚠️ Important Notes

- Mod only works with supported game versions
- Backup your game data before installing the mod
- May not be compatible with some other mods - check for conflicts
- Use the mod at your own risk

---

**Enjoy the mod! 🎮**

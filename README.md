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

### Loot system
There are 3 ways players can loot item:
- From dead/incap npc: you can approach dead/incap npc and press loot button to start looting, you can only loot each npc once and different NPC types drop different loot. Ex: cops are more likely to drop armors, civilians drop more consumables
- From pickable item: when you're holding a pickable item, you can press loot button, if the item is valid and there is still space left in inventory, your character will put it in inventory
- From world object: to start, press loot world item button, first your character will search for lootable item nearby, after that you can press loot world item button again to start looting, there will be a prompt to let you know which item you will loot. If you go out of loot zone, your character will stop looting and you have to search again (I have to make this complicated for optimization)

### Inventory
The mod adds a full inventory system with multiple item categories:
- Consumables
- Deadeye Items
- Armor
- Medkits
- Valuables
- Backpack Materials
- Backpacks
- Crafted Items
Inventory data is automatically saved and loaded

### Health Core 
A new stat called Health Core is added. Health Core ranges from 0 to 100, Health Core affects player's health regeneration per 6 seconds. Health core has 6 status levels:
- Golden: Special temporary boosted state, prevents normal Health Core decay and Health Core reduce from using liquors/cigarettes. Golden Health Core status worn off after 10 minutes since the last time you get the effect. This status can only achieved with Consumables combo
- Well Fed: Health Core > 80, regen 10 HP
- Good: Health Core > 60, regen 8 HP
- Satisfied: Health Core > 40, regen 5 HP
- Hungry: Health Core > 20, regen 3 HP
- Starving: Health Core ≤ 20, no health regen

### Focus Core 
Deadeye allows temporary slow motion combat help you to take aim. When activated, game goes into slow motion mode, Focus Core is consumed over time and will be disabled when empty. Focus can only be activated while using valid firearms.

### Consumables
Consumables are foods/drinks that restore the player’s health core after used. Consumables are organized into several pages and have different restore values and prices. 

### Liquors/cigarettes
Liquors/cigarettes is similar to consumables, items in this category restore your deadeye core but reduce player's health core. 

### Armor 
Armor restore your armor amount, there are 3 type of armor: Full armor (100 armor), heavy armor (50 armor), light armor (20 armor). Armor has long use time 

### Medkit 
Medkits restore your HP, there are 2 type of medkit: used medkit (50 HP), Medkit (100 HP). Medkit has short use time so it can be used in dangerous situation

### Valuables 
Valuables are added by this mod, you can sell valuables for money in shop menu. You can view all valuable you currently have in valuables option of inventory menu

### Backpacks
Backpack determine maximum number for each item you can carry at once. You can buy backpack from shop menu or craft from backpack meterial. You can check your current backpack and capacity in backpack option from inventory menu

### Combine
You can craft/combine items from this menu
- For consumables: You can use consumables combo to get bonus health core and some combos grant you golden health core. When using a consumable combo, your character will do animation for each item, combo use can be canceled
- For armor and medkit: You can craft lower tier items for higher tier items, for example you can craft 5 light armor for a full armor
- For backpack: You can craft backpack to upgrade inventory capacity using backpack material. You get backpack material from looting npc(small chance) or looting some world items(fashion bags from npc)

### Shop Menu
Shop menu has multiple pages allow you to buy any usable item, backpack and sell valuables/scraps
- Buying Items: When browsing the buy pages, you can navigate through the menu and add what you need to your cart, you can toggle add or remove mode using backspace button (default). The cart shows all necessary information like price, your currently have amount of each item the total value of the order to help you decide before confirming.
- Selling Items: Selling is split into two different styles:
   + Sell Valuables: Sell jewelry, gold, electronics, watches, and other valuables for cash, direct selling gives full payout while increases Heat (chance of getting wanted 4 stars, Heat reset after 24h ingame since the last time Heat increase). Safe selling is available, but it reduces the payout by 30%
   + Sell Scraps: Sell low-value scrap materials separately, making extra money from junk items.

---

## Important notes
-  All item use can be canceled by several actions like jump, shoot, ...
-  Looting Can Attract Police Attention: You get 1 star wanted level when looting npc near a cop
-  Inventory Limits Matter: Even if you find loot, items cannot be collected if inventory is full
-  Death Penalties: Death reduces 50 health core and focus core, also remove golden health core effect
-  Episode-Specific Save Files: Inventory data is stored separately for gta IV, TLAD and TBOGT
  
---

## Recommend mod
- Various Pedestrian Actions: This mod gives ped variety of food and drink that can be looted like soda, wine, burger... meanwhile in original game ped can only drink coffee or smoke. For that this mod goes well with my mod (this mod is not my work)
- Download link: https://www.nexusmods.com/gta4/mods/843

---


## Bugs & problems
Major problems:
- Sometimes when you try to loot pickable item the game might crash, this only happens to object on the ground but ped's object doesn't cause this issue, it's unclear why this happens but it could be because the looted object is not supposed to be deleted. Best fix for now is to avoid looting object causes game's crash
Minor bugs:
- Idle animations might interrupt item use animation, but this will not cancel item use
- The mod was developed in 800x600 resolution so the menu may not in intended position (but still visible because the menu's location is calculated to screen resolution)

---

**Enjoy the mod!**

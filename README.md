# FoodScript - Food System & Inventory Mod

## 📝 Description

**FoodScript** is a comprehensive mod for GTA (GTA4/TLAD/TBOGT) that provides a complete inventory management system focused on storing and using various food items, valuables, and equipment. The mod includes an NPC looting system, a full-featured shop, crafting/combination mechanics, and management of different ability cores (Health Core, Deadeye Core, Gold Core).

With an intuitive menu interface, players can easily manage resources, upgrade backpacks, sell valuable items, and improve their character.

---

## 🛠️ Installation Guide

### Requirements
- GTA4, GTA: Chinatown Wars, GTA: Ballad of Gay Tony, or GTA: The Ballad of Gay Tony
- GTA.NET Framework or similar
- Visual Studio or C# compiler

### Installation Steps

1. **Clone or download the mod files**
   ```bash
   git clone [repository-link]
   cd food
   ```

2. **Copy files to the scripts folder**
   - Copy `FoodScript.cs` to the game's `scripts\` directory
   - Copy data files (`FoodScriptData.ini`, `FoodScriptDataGta4.ini`, etc.) to the `scripts\` directory

3. **Configure data files**
   - Edit `.ini` files to customize items, prices, and crafting recipes as desired

4. **Launch the game**
   - Run the game and the mod will load automatically
   - Use the default keybinds to open your inventory (see Features section)

---

## ⭐ Features

### 1. **Comprehensive Inventory System**
   - Open inventory with `I` key
   - Display all items organized by category
   - Support for 4 pages of consumables and 3 pages of valuables
   - Upgrade inventory capacity with different backpack types (10 → 100+ items)

### 2. **Item Categories**
   - **Consumables**: Drinks, sweets, meals, soups, etc.
   - **Medkits**: Health restoration items and medical kits
   - **Armor**: Body armor to increase character protection
   - **Valuables**: Gold, jewelry, watches, phones, etc.
   - **Backpacks**: Upgrade inventory capacity (Magic Backpack with 100 items)

### 3. **Looting & Robbery System**
   - Loot items from dead NPC bodies
   - Rob frightened NPCs (when aiming)
   - Different loot rates based on NPC type (Cops, Civilians, Paramedics, etc.)
   - Unlock valuable items when backpack is large enough

### 4. **Shop System**
   - Buy all types of items from the shop
   - Checkout with shopping cart and shipping fees
   - Sell individual valuables at market prices
   - Sell scrap materials

### 5. **Crafting/Combination System**
   - Combine items to create special food combos
   - Craft upgraded backpacks from materials
   - Crafting recipes stored in `combineDatabase`
   - Display ingredient requirements and product effects

### 6. **Ability Cores System**
   - **Health Core** (0-100): Track overall health status
   - **Deadeye Core** (0-100): Aiming ability, slowly regenerates
   - **Gold Core** (special): Activated when using special items, prevents negative healing effects

### 7. **Item Usage**
   - Each item has unique animations
   - Automatically restores Health/Armor/Deadeye
   - Support for complex animations with 3D item attachments
   - Cancel animations by attacking or jumping

### 8. **Auto-Save System**
   - Inventory data automatically saved to `.ini` files
   - Data reloads on game startup
   - Support for multiple game mode saves

### 9. **User Interface**
   - Simple text-based menu, easy to navigate
   - Detailed information display: price, quantity, type
   - Item status notifications (success, error, warning)
   - Color-coded system for categories

### 10. **Time Management & Special Features**
   - Track playtime for core recovery calculations
   - Support for Gold Core with limited duration
   - Manage looted NPC list to prevent duplicate looting

---

## 📋 Default Keybinds

| Key | Function |
|-----|----------|
| `I` | Open/Close inventory |
| `0` / `NumPad0` | Back to previous menu |
| `1-9` | Select item in menu |
| `Left/Right` | Switch pages (valuables & consumables) |
| `Enter` | Confirm order (shop) |
| `Back` | Return to category |

---

## 🎮 Usage Guide

### Opening Inventory
Press `I` to toggle the inventory menu. The menu will pause the game.

### Using Items
1. Open inventory (`I`)
2. Select a category (1-7 from main menu)
3. Select an item (1-9)
4. Confirm usage - animation will play automatically

### Looting from NPCs
- Approach a dead NPC
- Press `E` or corresponding button to loot
- Items are automatically added to your inventory

### Crafting & Combinations
1. From main menu, select "Combine Items" (7)
2. Choose the category to craft from
3. Select a recipe from the list
4. Item is created if you have enough ingredients

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

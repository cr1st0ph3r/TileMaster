using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TileMaster.Entity.Enums;

namespace TileMaster.Entity
{
    [Serializable]
    public class Item
    {
        public int Id { get; set; }
        public int TileId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TextureName { get; set; }
        public string UIIcon { get; set; }
        public string LightColorName { get; set; }
        public int StackSize { get; set; } = 1000;
        public bool IsTile { get; set; }
        /// <summary>
        /// Whether the item is a tool (e.g., pickaxe, axe).
        /// </summary>
        public bool IsTool { get; set; }
        /// <summary>
        /// Indicates if the item functions as a container (e.g., chest).
        /// </summary>
        public bool IsContainer { get; set; }
        /// <summary>
        /// Identifies whether the item is a weapon.
        /// </summary>
        public bool IsWeapon { get; set; }
        public int WeaponDamage { get; set; }
        public int WeaponKnockback { get; set; }
        /// <summary>
        /// Indicates if the weapon is ranged (e.g., bow, gun).
        /// </summary>
        public bool IsRangedWeapon { get; set; }
        /// <summary>
        /// Wether this weapon requires ammunition.
        /// </summary>
        public bool RequiresAmmo { get; set; }
        /// <summary>
        /// Th type of ammunition required by this weapon.
        /// </summary>
        public AmmoType RequiredAmmoType { get; set; }
        /// <summary>
        /// Defines how far the projectiles fired from this weapon can travel.
        /// </summary>
        public int RangedDistance { get; set; }
        /// <summary>
        /// Defines how fast the projectiles fired from this weapon travel.
        /// </summary>
        public int RangedVelocity { get; set; }
        /// <summary>
        /// Whether this item is ammunition (e.g., arrows, bullets).
        /// </summary>
        public bool IsAmmo { get; set; }
        /// <summary>
        /// Should the ammo be affected by gravity when fired.
        /// </summary>
        public bool AffectedByGravity { get; set; } = true;
        /// <summary>
        /// Defines the type of ammunition this item is (if IsAmmo is true).
        /// </summary>
        public AmmoType AmmoType { get; set; }
        /// <summary>
        /// The action performed by the tool (if IsTool is true).
        /// </summary>
        public ToolAction ToolAction { get; set; }
        /// <summary>
        /// The time (in milliseconds) it takes to use the item (tools).
        /// </summary>
        public int UseTime { get; set; } = 200;
        /// <summary>
        /// For placeable items, this value should state how many tiles the item occupies in width.
        /// </summary>
        public int Width { get; set; } = 1;
        /// <summary>
        /// For placeable items, this value should state how many tiles the item occupies in height.
        /// </summary>
        public int Height { get; set; } = 1;

        [NonSerialized]
        public Texture2D Texture;

        /// <summary>
        /// Whether the item can be placed in the game world as a block (Ex. Anvil).
        /// </summary>
        public bool IsPlaceable { get; set; }
        public bool PlaceableOnBackground { get; set; }

        /// <summary>
        /// Whether the item can be interacted with when placed (e.g., Anvil, Chest).
        /// </summary>
        public bool IsInteractive { get; set; }
        /// <summary>
        /// The type of interaction (e.g., "Crafting", "Container").
        /// </summary>
        public InteractionType InteractionType { get; set; }

        // Lighting properties
        public bool IsLightSource { get; set; }
        public bool IsFlickeringLight { get; set; }
        public Color? LightColor { get; set; } = Color.White;
        public float LightIntensity { get; set; } = 0f;
        public float LightRadius { get; set; } = 0f; // Could be used for gradient logic later



        public Item()
        {
        }
    }
}

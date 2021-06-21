using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        Dictionary<string, string> ingotAbbreviations_ = new Dictionary<string, string>();
        Dictionary<string, string> oreAbbreviations_ = new Dictionary<string, string>();
        Dictionary<string, string> compAbbreviations_ = new Dictionary<string, string>();
        int lineHeight_;
        List<long> knownPanelIds_ = new List<long>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            lineHeight_ = 64;

            InitAbbreviations();
        }

        public void InitAbbreviations()
        {
            ingotAbbreviations_["Cobalt"] = "Co";
            ingotAbbreviations_["Gold"] = "Au";
            ingotAbbreviations_["Iron"] = "Fe";
            ingotAbbreviations_["Magnesium"] = "Mg";
            ingotAbbreviations_["Nickel"] = "Ni";
            ingotAbbreviations_["Platinum"] = "Pt";
            ingotAbbreviations_["Silicon"] = "Si";
            ingotAbbreviations_["Silver"] = "Ag";
            ingotAbbreviations_["Uranium"] = "U";
            ingotAbbreviations_["Stone"] = "Gr";
            ingotAbbreviations_["Scrap"] = "Scr";

            foreach (var ingot in ingotAbbreviations_)
                oreAbbreviations_.Add(ingot.Key, ingot.Value);
            oreAbbreviations_["Ice"] = "Ice";
            oreAbbreviations_["Organic"] = "Org";
            oreAbbreviations_["Stone"] = "Sto";

            compAbbreviations_["BulletproofGlass"] = "Gls";
            compAbbreviations_["Canvas"] = "Canv";
            compAbbreviations_["Computer"] = "PC";
            compAbbreviations_["Construction"] = "Con";
            compAbbreviations_["Detector"] = "Det";
            compAbbreviations_["Display"] = "Disp";
            compAbbreviations_["Explosives"] = "Exp";
            compAbbreviations_["Girder"] = "Gird";
            compAbbreviations_["GravityGenerator"] = "Grav";
            compAbbreviations_["InteriorPlate"] = "InPl";
            compAbbreviations_["LargeTube"] = "LTub";
            compAbbreviations_["Medical"] = "Med";
            compAbbreviations_["MetalGrid"] = "Grid";
            compAbbreviations_["Motor"] = "Mtr";
            compAbbreviations_["PowerCell"] = "Pwr";
            compAbbreviations_["RadioCommunication"] = "Rdio";
            compAbbreviations_["Reactor"] = "Reac";
            compAbbreviations_["SmallTube"] = "STub";
            compAbbreviations_["SolarCell"] = "Sol";
            compAbbreviations_["SteelPlate"] = "StPl";
            compAbbreviations_["Superconductor"] = "Cond";
            compAbbreviations_["Thrust"] = "Thr";
            compAbbreviations_["ZoneChip"] = "Zone";
        }

        public void InitPanel(IMyTextPanel panel)
        {
            panel.ContentType = ContentType.SCRIPT;
            panel.Script = "";
            panel.ScriptBackgroundColor = Color.Black;
            panel.ScriptForegroundColor = Color.White;
        }

        public void Save()
        {
        }

        public void DrawSprites(ref MySpriteDrawFrame frame, Dictionary<string, int> items, RectangleF viewport, string category)
        {
            var pos = new Vector2(20, 20) + viewport.Position;

            foreach (var item in items)
            {
                var sprite = new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = $"MyObjectBuilder_{category}/{item.Key}",
                    Position = pos + new Vector2(0, lineHeight_ / 2),
                    Size = new Vector2(lineHeight_, lineHeight_),
                    Color = Color.White,
                    Alignment = TextAlignment.LEFT,
                };
                frame.Add(sprite);

                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = $"{getAbbreviation(item.Key, category)}",
                    Position = pos + new Vector2(lineHeight_, 0),
                    RotationOrScale = 2f,
                    Color = Color.White,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                };
                frame.Add(sprite);

                sprite = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = printNumber(item.Value),
                    Position = pos + new Vector2(lineHeight_ * 3, 0),
                    RotationOrScale = 2f,
                    Color = Color.White,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                };
                frame.Add(sprite);

                pos += new Vector2(0, lineHeight_);
            }
        }

        public string printNumber(int amount)
        {
            if (amount < 1000)
                return $"{amount}";
            if (amount < 1000000)
                return $"{amount / 1000f:F2}K";
            else
                return $"{amount / 1000000f:F2}M";
        }

        public string getAbbreviation(string item, string type)
        {
            if (type == "Ingot")
            {
                return ingotAbbreviations_[item];
            }
            if (type == "Ore")
            {
                return oreAbbreviations_[item];
            }
            if (type == "Component")
            {
                return compAbbreviations_[item];
            }

            return item.Substring(0, 3);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var inventories = new List<IMyInventory>();
            GetInventories(inventories);
            var ingots = new Dictionary<string, int>();
            GetItemsOfCategory(ingots, "Ingot", inventories);
            var ores = new Dictionary<string, int>();
            GetItemsOfCategory(ores, "Ore", inventories);
            var comps = new Dictionary<string, int>();
            GetItemsOfCategory(comps, "Component", inventories);

            var panels = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName("Inv", panels, panel => panel is IMyTextPanel && panel.IsSameConstructAs(Me));

            foreach (var panelBlock in panels)
            {
                var items = ingots;
                var type = "Ingot";
                if (panelBlock.CustomName.Contains("Ore"))
                {
                    items = ores;
                    type = "Ore";
                }
                else if (panelBlock.CustomName.Contains("Comp"))
                {
                    items = comps;
                    type = "Component";
                }

                var panel = panelBlock as IMyTextPanel;

                if (!knownPanelIds_.Contains(panel.EntityId))
                {
                    InitPanel(panel);
                    knownPanelIds_.Add(panel.EntityId);
                }

                var frame = panel.DrawFrame();

                RectangleF viewport = new RectangleF(
                    (panel.TextureSize - panel.SurfaceSize) / 2f,
                    panel.SurfaceSize);

                DrawSprites(ref frame, items, viewport, type);

                frame.Dispose();
            }
        }

        public void GetItemsOfCategory(Dictionary<string, int> items, string category, List<IMyInventory> inventories)
        {
            foreach(var inv in inventories)
            {
                var itemsInInv = new List<MyInventoryItem>();
                inv.GetItems(itemsInInv, item => item.Type.TypeId == $"MyObjectBuilder_{category}");

                foreach (var item in itemsInInv)
                {
                    if (!items.ContainsKey(item.Type.SubtypeId))
                    {
                        items.Add(item.Type.SubtypeId, 0);
                    }
                    items[item.Type.SubtypeId] += item.Amount.ToIntSafe();
                }
            }
        }

        public void GetInventories(List<IMyInventory> inventories)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, block => block.HasInventory && block.IsSameConstructAs(Me));

            foreach(var block in blocks)
            {
                inventories.Add(block.GetInventory());
            }
        }
    }
}

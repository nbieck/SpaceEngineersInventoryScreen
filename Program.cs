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
        // *******
        // Parameters
        // *******
        const int lineHeight_ = 64;
        const float scrollWait_ = 2f;
        const float scrollSpeed_ = 32f;
        const int margin_ = 20;
        // *******


        const float textHeight_ = 32f;
        Dictionary<string, string> ingotAbbreviations_ = new Dictionary<string, string>();
        Dictionary<string, string> oreAbbreviations_ = new Dictionary<string, string>();
        Dictionary<string, string> compAbbreviations_ = new Dictionary<string, string>();
        List<long> knownPanelIds_ = new List<long>();

        public class ScrollData
        {
            public bool scrolling;
            public bool atTop;
            public float scrollDistance;
            public float waitTime;

            public ScrollData()
            {
                scrolling = false;
                atTop = true;
                scrollDistance = 0;
                waitTime = 0;
            }
        }

        Dictionary<long, ScrollData> current_scrolls_ = new Dictionary<long, ScrollData>();


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

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

        public void DrawSprites(ref MySpriteDrawFrame frame, Dictionary<string, int> items, RectangleF viewport, string category, long displayId)
        {
            float totalHeight = items.Count * lineHeight_;
            float scroll = 0;
            if (totalHeight > viewport.Height - 2 * margin_)
            {
                if (!current_scrolls_.ContainsKey(displayId))
                {
                    current_scrolls_[displayId] = new ScrollData();
                }
                else
                {
                    float maxScroll = totalHeight - (viewport.Height - 2 * margin_);
                    var scrollData = current_scrolls_[displayId];
                    if (scrollData.scrolling)
                    {
                        scrollData.scrollDistance += scrollSpeed_ * (float)Runtime.TimeSinceLastRun.TotalSeconds;
                        if (scrollData.scrollDistance > maxScroll)
                        {
                            scrollData.scrolling = false;
                            scrollData.atTop = false;
                            scrollData.waitTime = 0;
                            scroll = maxScroll;
                        }
                        else
                            scroll = scrollData.scrollDistance;
                    }
                    else
                    {
                        scrollData.waitTime += (float)Runtime.TimeSinceLastRun.TotalSeconds;
                        if (scrollData.waitTime > scrollWait_)
                        {
                            if (scrollData.atTop)
                            {
                                scrollData.scrolling = true;
                                scrollData.scrollDistance = 0;
                            }
                            else
                            {
                                scrollData.atTop = true;
                                scrollData.waitTime = 0;
                            }
                        }

                        if (scrollData.atTop)
                            scroll = 0;
                        else
                            scroll = maxScroll;
                    }

                    current_scrolls_[displayId] = scrollData;
                }
            }
            else if (current_scrolls_.ContainsKey(displayId))
            {
                current_scrolls_.Remove(displayId);
            }

            var pos = new Vector2(margin_, margin_ - scroll) + viewport.Position;

            using (frame.Clip(margin_, margin_, (int)viewport.Width - 2 * margin_, (int)viewport.Height - 2 * margin_))
            {
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
                        RotationOrScale = lineHeight_ / textHeight_,
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
                        RotationOrScale = lineHeight_ / textHeight_,
                        Color = Color.White,
                        Alignment = TextAlignment.LEFT,
                        FontId = "White"
                    };
                    frame.Add(sprite);

                    pos += new Vector2(0, lineHeight_);
                }
            }
        }

        public string printNumber(int amount)
        {
            if (amount < 1000)
                return $"{amount}";
            if (amount < 1000000)
                return $"{amount / 1000f:F2}K";
            if (amount < 1000000000)
                return $"{amount / 1000000f:F2}M";
            else
                return $"{amount / 1000000000f:F2}G";
        }

        public string getAbbreviation(string item, string type)
        {
            Dictionary<string, string> abbrevs = new Dictionary<string, string>();
            if (type == "Ingot")
            {
                abbrevs = ingotAbbreviations_;
            }
            if (type == "Ore")
            {
                abbrevs = oreAbbreviations_;
            }
            if (type == "Component")
            {
                abbrevs = compAbbreviations_;
            }

            if (abbrevs.ContainsKey(item))
                return abbrevs[item];

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

                DrawSprites(ref frame, items, viewport, type, panel.EntityId);

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

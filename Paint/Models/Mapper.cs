﻿using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Paint.Models.Shapes;
using Paint.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using static Paint.Models.Shapes.PropsN;

namespace Paint.Models
{
    public class Mapper
    {
        public string shapeName = "Линия 1";

        public string shapeColor = "Blue";
        public string shapeFillColor = "Yellow";
        public int shapeThickness = 2;

        public SafeNum shapeWidth;
        public SafeNum shapeHeight;
        public SafeNum shapeHorizDiagonal;
        public SafeNum shapeVertDiagonal;

        public SafePoint shapeStartDot;
        public SafePoint shapeEndDot;
        public SafePoint shapeCenterDot;

        public SafePoints shapeDots;

        public SafeGeometry shapeCommands;

        private readonly Action<object?>? UPD;
        private readonly object? INST;

        public readonly ObservableCollection<ShapeListBoxItem> shapes = new();
        private readonly Dictionary<string, ShapeListBoxItem> name2shape = new();

        public Mapper(Action<object?>? upd, object? inst)
        {
            shapeWidth = new(200, Update, this);
            shapeHeight = new(100, Update, this);
            shapeHorizDiagonal = new(100, Update, this);
            shapeVertDiagonal = new(200, Update, this);

            shapeStartDot = new(50, 50, Update, this);
            shapeEndDot = new(100, 100, Update, this);
            shapeCenterDot = new(150, 150, Update, this);

            shapeDots = new("50,50 100,100 50,100 100,50", Update, this);

            shapeCommands = new("M 10 70 l 30,30 10,10 35,0 0,-35 m 50 0 l 0,-50 10,0 35,35 m 50 0 l 0,-50 10,0 35,35z m 70 0 l 0,30 30,0 5,-35z", Update, this);
            UPD = upd;
            INST = inst;
        }

        private static IShape[] Shapers => new IShape[] {
            new Shape1_Line(),
            new Shape2_BreakedLine(),
            new Shape3_Polygonal(),
            new Shape4_Rectangle(),
            new Shape5_Ellipse(),
            new Shape6_CompositeFigure(),
        };
        private static Dictionary<string, IShape> TShapers => new(Shapers.Select(shaper => new KeyValuePair<string, IShape>(shaper.Name, shaper)));

        private IShape cur_shaper = Shapers[0];
        private readonly Dictionary<string, Shape> shape_dict = new();
        public string? newName = null;
        public short select_shaper = -1;
        private bool update_name_lock = false;

        public void ChangeFigure(int n)
        {
            cur_shaper = Shapers[n];
            if (!update_name_lock) newName = GenName(cur_shaper.Name);
            Update();
        }

        internal object GetProp(PropsN num)
        {
            return num switch
            {
                PName => shapeName,
                PColor => shapeColor,
                PFillColor => shapeFillColor,
                PThickness => shapeThickness,
                PWidth => shapeWidth,
                PHeight => shapeHeight,
                PHorizDiagonal => shapeHorizDiagonal,
                PVertDiagonal => shapeVertDiagonal,
                PStartDot => shapeStartDot,
                PEndDot => shapeEndDot,
                PCenterDot => shapeCenterDot,
                PDots => shapeDots,
                PCommands => shapeCommands,
                _ => 0
            };
        }
        internal void SetProp(PropsN num, object obj)
        {
            switch (num)
            {
                case PName: shapeName = (string)obj; break;
                case PColor: shapeColor = (string)obj; break;
                case PFillColor: shapeFillColor = (string)obj; break;
                case PThickness: shapeThickness = (int)obj; break;
            };
        }

        public bool ValidInput()
        {
            foreach (PropsN num in cur_shaper.Props)
                if (GetProp(num) is ISafe @prop && !@prop.Valid) return false;
            return true;
        }
        public bool ValidName() => !shape_dict.ContainsKey(shapeName);

        private string GenName(string prefix)
        {
            prefix += " ";
            int n = 1;
            while (true)
            {
                string res = prefix + n;
                if (!shape_dict.ContainsKey(res)) return res;
                n += 1;
            }
        }
        public Shape? Create(bool preview)
        {
            Shape? newy = cur_shaper.Build(this);
            if (newy == null) return null;
            if (preview)
            {
                newy.Name = "marker";
                return newy;
            }

            if (name2shape.TryGetValue(shapeName, out var value)) Remove(value);

            shape_dict[shapeName] = newy;
            var item = new ShapeListBoxItem(shapeName, this);
            shapes.Add(item);
            name2shape[shapeName] = item;

            newName = GenName(cur_shaper.Name);
            return newy;
        }

        internal void Remove(ShapeListBoxItem item)
        {
            var Name = item.Name;
            if (!shape_dict.ContainsKey(Name)) return;

            var shape = shape_dict[Name];
            if (shape == null || shape.Parent is not Canvas @c) return;

            @c.Children.Remove(shape);
            shapes.Remove(item);
            name2shape.Remove(Name);
            shape_dict.Remove(Name);

            newName = GenName(cur_shaper.Name);
            Update();
        }

        public void Clear()
        {
            foreach (var item in shape_dict)
            {
                var shape = item.Value;
                if (shape == null || shape.Parent is not Canvas @c) continue;
                @c.Children.Clear();
            }
            shapes.Clear();
            name2shape.Clear();
            shape_dict.Clear();

            newName = GenName(cur_shaper.Name);
            Update();
        }

        public void Export(bool is_xml)
        {
            List<object> data = new();
            foreach (var item in shape_dict)
            {
                var shape = item.Value;
                foreach (var shaper in Shapers)
                {
                    var res = shaper.Export(shape);
                    if (res != null)
                    {
                        res["type"] = shaper.Name;
                        data.Add(res);
                        break;
                    }
                }
            }
            if (is_xml)
            {
                var xml = Utils.Obj2xml(data);
                if (xml == null) { return; }
                File.WriteAllText("../../../Export.xml", xml);
            }
            else
            {
                var json = Utils.Obj2json(data);
                if (json == null) { return; }
                File.WriteAllText("../../../Export.json", json);
            }
        }

        public Shape[]? Import(bool is_xml)
        {
            string name = is_xml ? "Export.xml" : "Export.json";
            if (!File.Exists("../../../" + name)) { return null; }

            var data = File.ReadAllText("../../../" + name);

            var json = is_xml ? Utils.Xml2obj(data) : Utils.Json2obj(data);
            if (json is not List<object?> @list) { return null; }

            List<Shape> res = new();
            Clear();

            foreach (object? item in @list)
            {
                if (item is not Dictionary<string, object?> @dict) { continue; }

                if (!@dict.ContainsKey("type") || @dict["type"] is not string @type) { continue; }
                if (!@dict.ContainsKey("name") || @dict["name"] is not string @shapeName) { continue; }
                if (!TShapers.ContainsKey(@type)) { continue; }

                var shaper = TShapers[@type];
                var newy = shaper.Import(@dict);
                if (newy == null) { continue; }

                shape_dict[shapeName] = newy;
                var itemm = new ShapeListBoxItem(shapeName, this);
                shapes.Add(itemm);
                name2shape[shapeName] = itemm;

                res.Add(newy);
            }

            newName = GenName(cur_shaper.Name);
            return res.ToArray();
        }

        public void Select(ShapeListBoxItem? shapeItem)
        {
            if (shapeItem == null) return;

            var shape = shape_dict[shapeItem.Name];
            bool yeah = false;
            short n = 0;
            foreach (var shaper in Shapers)
            {
                yeah = shaper.Load(this, shape);
                if (yeah)
                {
                    update_name_lock = true;
                    select_shaper = n;
                    Update();
                    update_name_lock = false;
                    break;
                }
                n++;
            }
        }

        public ShapeListBoxItem? ShapeTap(string name)
        {
            if (name.StartsWith("sn_")) name = name[3..];
            else if (name.StartsWith("sn|")) name = Utils.Base64Decode(name.Split('|')[1]);
            else return null;

            if (name2shape.TryGetValue(name, out var item))
            {
                Select(item);
                return item;
            }
            return null;
        }

        private void Update()
        {
            UPD?.Invoke(INST);
        }
        private static void Update(object? me)
        {
            if (me != null && me is Mapper @map) @map.Update();
        }
    }
}

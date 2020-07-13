using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace atri_composite
{
    class CompoundImage
    {
        public List<Layer> Layers { get; }

        public int Width { get; }

        public int Height { get; }

        public string Name { get; }

        public CompoundImage(string descPath)
        {
            Name = Path.GetFileNameWithoutExtension(descPath);
            var imagePrefix = Path.GetFullPath(descPath);
            imagePrefix = imagePrefix.Substring(0, imagePrefix.Length - 4) + "_";

            var jArr = Utils.LoadPBDFile(descPath, true);

            int i = 0;
            Width = (int)jArr[i]["width"];
            Height = (int)jArr[i]["height"];

            var flatLayers = new List<Layer>();
            for (i++; i < jArr.Count; i++)
            {
                Layer item = jArr[i].ToObject<Layer>();
                item.Path = imagePrefix + item.LayerID + ".png";
                flatLayers.Add(item);
            }

            flatLayers.Where(o => o.GroupLayerID != 0).ToList().ForEach(o => flatLayers.First(p => p.LayerID == o.GroupLayerID).Children.Add(o));

            Layers = flatLayers.Where(o => o.GroupLayerID == 0).ToList();
        }

        public Layer GetLayer(string query)
        {
            try
            {
                var blocks = query.Split('/');
                Layer prev;
                if (blocks.Length > 1) prev = Layers.First(o => o.LayerType == LayerType.Folder && o.Name == blocks[0]);
                else return Layers.First(o => o.LayerType == LayerType.Normal && o.Name == blocks[0]);
                for (var i = 1; i < blocks.Length - 1; i++) prev = prev.Children.First(o => o.LayerType == LayerType.Folder && o.Name == blocks[i]);
                return prev.Children.First(o => o.LayerType == LayerType.Normal && o.Name == blocks.Last());
            }
            catch { return null; }
        }

        public Bitmap Generate(params string[] layers)
        {
            var bitmap = new Bitmap(Width, Height);
            foreach (var s in layers)
            {
                if (s == "dummy") continue;
                var layer = GetLayer(s);
                if (layer == null) throw new ArgumentException();
                if (layer.Type != KrBlendMode.ltPsNormal) throw new NotSupportedException();

                using (var layerBitmap = new Bitmap(layer.Path))
                using (var g = Graphics.FromImage(bitmap))
                    g.DrawImage(layerBitmap, layer.Left, layer.Top, layer.Width, layer.Height);
            }
            return bitmap;
        }

        public enum KrBlendMode
        {
            ltBinder = 0,
            ltCoverRect = 1,
            ltOpaque = 1, // the same as ltCoverRect
            ltTransparent = 2, // alpha blend
            ltAlpha = 2, // the same as ltTransparent
            ltAdditive = 3,
            ltSubtractive = 4,
            ltMultiplicative = 5,
            ltEffect = 6,
            ltFilter = 7,
            ltDodge = 8,
            ltDarken = 9,
            ltLighten = 10,
            ltScreen = 11,
            ltAddAlpha = 12, // additive alpha blend
            ltPsNormal = 13,
            ltPsAdditive = 14,
            ltPsSubtractive = 15,
            ltPsMultiplicative = 16,
            ltPsScreen = 17,
            ltPsOverlay = 18,
            ltPsHardLight = 19,
            ltPsSoftLight = 20,
            ltPsColorDodge = 21,
            ltPsColorDodge5 = 22,
            ltPsColorBurn = 23,
            ltPsLighten = 24,
            ltPsDarken = 25,
            ltPsDifference = 26,
            ltPsDifference5 = 27,
            ltPsExclusion = 28
        }

        public enum LayerType
        {
            Normal = 0,
            Hidden = 1,
            Folder = 2,
            Adjust = 3,
            Fill = 4
        }

        public class Layer
        {
            public string Path { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("type")]
            public KrBlendMode Type { get; set; }

            [JsonProperty("layer_type")]
            public LayerType LayerType { get; set; }

            [JsonProperty("layer_id")]
            public int LayerID { get; set; }

            [JsonProperty("group_layer_id")]
            public int GroupLayerID { get; set; } = 0;

            [JsonProperty("width")]
            public int Width { get; set; }

            [JsonProperty("height")]
            public int Height { get; set; }

            [JsonProperty("left")]
            public int Left { get; set; }

            [JsonProperty("top")]
            public int Top { get; set; }

            [JsonProperty("visible")]
            public int Visible { get; set; }

            [JsonProperty("opacity")]
            public int Opacity { get; set; }

            public List<Layer> Children { get; } = new List<Layer>();
        }
    }
}

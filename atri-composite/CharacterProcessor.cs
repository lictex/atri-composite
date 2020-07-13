using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace atri_composite
{
    public class CharacterProcessor
    {
        public static List<Character> Load(string fgimageDir)
        {
            var characters = new List<Character>();
            var standFiles = Directory.GetFiles(fgimageDir, "*.stand");
            foreach (var file in standFiles)
            {
                var character = new Character() { Name = Path.GetFileNameWithoutExtension(file) };
                var rtxt = Regex.Matches(File.ReadAllText(file, Encoding.Unicode), "\"filename\"=>\"[a-zA-Z0-9]+\"");
                foreach (Match match in rtxt)
                {
                    var m = match.Value;
                    var name = m.Substring(13, m.Length - 14);

                    var pose = ProcessStandInfo(Path.Combine(fgimageDir, name + ".sinfo"));
                    pose.Sizes.AddRange(GetSizes(Path.Combine(fgimageDir, Path.GetFileNameWithoutExtension(file)), name));
                    pose.Name = name;
                    character.Poses.Add(pose);
                }
                characters.Add(character);
            }
            return characters;
        }

        private static Character.Pose ProcessStandInfo(string sInfoPath)
        {
            var sInfo = File.ReadAllText(sInfoPath);
            var pose = new Character.Pose();

            sInfo.Split('\n').Select(o => o.Trim()).ToList().ForEach(expression =>
            {
                var blocks = expression.Split('\t').Select(p => p.Trim()).ToList();

                var paramIndex = 0;
                switch (blocks[paramIndex++])
                {
                    case "dress":
                        var dressName = blocks[paramIndex++];
                        if (!pose.Dresses.Exists(o => o.Name == dressName)) pose.Dresses.Add(new Character.Pose.Dress() { Name = dressName });
                        var dress = pose.Dresses.First(o => o.Name == dressName);
                        switch (blocks[paramIndex++])
                        {
                            case "base":
                                dress.LayerPath = blocks[paramIndex++];
                                break;
                            case "diff":
                                dress.Additions.Add(new Character.Pose.Dress.Addition() { Name = blocks[paramIndex++], LayerPath = blocks[paramIndex++] });
                                break;
                        }
                        break;
                    case "facegroup":
                        pose.FaceComponents.Add(new Character.Pose.FaceComponent() { Name = blocks[paramIndex++] });
                        break;
                    case "fgname":
                        var fgname = blocks[paramIndex++];
                        pose.FaceComponents.First(y => fgname.StartsWith(y.Name)).Variants.Add(new Character.Pose.FaceComponent.Variant() { Name = fgname, LayerPath = blocks[paramIndex++] });
                        break;
                    case "fgalias":
                        var fgalias = blocks[paramIndex++];
                        var items = new List<KeyValuePair<string, Character.Pose.FaceComponent.Variant>>();
                        do
                        {
                            var k = blocks[paramIndex++];
                            var v = pose.FaceComponents.First(p => k.StartsWith(p.Name)).Variants.First(p => p.Name == k);
                            items.Add(new KeyValuePair<string, Character.Pose.FaceComponent.Variant>(k, v));
                        }
                        while (paramIndex < blocks.Count);
                        pose.Presets.Add(new Character.Pose.Preset() { Name = fgalias, Items = items.ToArray() });
                        break;
                }
            });
            return pose;
        }

        private static List<string> GetSizes(string dir, string name) => Directory.GetFiles(dir)
                .Select(o => Path.GetFileName(o))
                .Where(o => Regex.IsMatch(o, $@"{name}_[a-zA-Z0-9]+.pbd$"))
                .Select(o => o.Substring(name.Length + 1, o.Length - name.Length - 5))
                .ToList();
    }
}

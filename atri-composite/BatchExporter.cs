using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace atri_composite
{
    internal class BatchExporter
    {
        internal struct Limitation
        {
            public Character Character;
            public Character.Pose Pose;
            public string Size;
            public Character.Pose.Dress Dress;
            public Character.Pose.Dress.Addition Addition;
        }

        List<Character> Characters { get; }

        string WorkingDirectory { get; }

        string TargetDirectory { get; }

        public BatchExporter(List<Character> characters, string workingDirectory, string targetDirectory)
        {
            Characters = characters;
            WorkingDirectory = workingDirectory;
            TargetDirectory = targetDirectory;
        }

        public void Run(Limitation limit)
        {
            EnumerateVariants(limit).AsParallel().WithDegreeOfParallelism(Environment.ProcessorCount * 4).ForAll(_ =>
            {
                var (character, pose, dress, size, preset, addition) = _;
                var image = new CompoundImage(Path.Combine(WorkingDirectory, character.Name, $"{pose.Name}_{size}.pbd"));
                var layers = new List<string>();
                layers.Add(dress.LayerPath);
                layers.Add(addition.LayerPath);
                layers.AddRange(preset.Items.Reverse().Select(o => o.Value.LayerPath));

                var result = image.Generate(layers.ToArray()).Crop(true).ToBitmapSource(true);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(result));
                using (var file = File.Create(Path.Combine(TargetDirectory, $"{character}_{pose}_{size}_{dress}_{addition}_{preset}.png")))
                    encoder.Save(file);
            });
        }

        public IEnumerable<(Character, Character.Pose, Character.Pose.Dress, string, Character.Pose.Preset, Character.Pose.Dress.Addition)> EnumerateVariants(Limitation limit) =>
            (limit.Character != null ? new List<Character>() { limit.Character } : Characters).SelectMany(character =>
            (limit.Pose != null ? new List<Character.Pose>() { limit.Pose } : character.Poses).SelectMany(pose =>
            {
                var dresses = limit.Dress != null || limit.Addition != null ? new List<Character.Pose.Dress>() { limit.Dress } : pose.Dresses;
                var sizes = limit.Size != null ? new List<string>() { limit.Size } : pose.Sizes;
                var presets = pose.Presets;
                return dresses.SelectMany(dress =>
                    sizes.SelectMany(size =>
                    presets.SelectMany(preset =>
                    (limit.Addition != null ? new List<Character.Pose.Dress.Addition>() { limit.Addition } : dress.Additions).Select(addition =>
                        (character, pose, dress, size, preset, addition)
                    ))));
            }));
    }
}

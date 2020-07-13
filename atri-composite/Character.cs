using System.Collections.Generic;

namespace atri_composite
{
    public class Character
    {
        public string Name { get; set; }

        public List<Pose> Poses { get; } = new List<Pose>();

        public override string ToString() => Name;

        public class Pose
        {
            public string Name { get; set; }

            public List<FaceComponent> FaceComponents { get; } = new List<FaceComponent>();

            public List<Dress> Dresses { get; } = new List<Dress>();

            public List<Preset> Presets { get; } = new List<Preset>();

            public List<string> Sizes { get; } = new List<string>();

            public override string ToString() => Name;

            public class FaceComponent
            {
                public string Name { get; set; }

                public List<Variant> Variants { get; } = new List<Variant>();

                public override string ToString() => Name;

                public class Variant
                {
                    public string Name { get; set; }

                    public string LayerPath { get; set; }

                    public override string ToString() => Name;
                }
            }

            public class Dress
            {
                public string Name { get; set; }

                public string LayerPath { get; set; } = "dummy";

                public List<Addition> Additions { get; } = new List<Addition>();

                public override string ToString() => Name;

                public class Addition
                {
                    public string Name { get; set; }

                    public string LayerPath { get; set; }

                    public override string ToString() => Name;
                }
            }

            public class Preset
            {
                public string Name { get; set; }

                public KeyValuePair<string, FaceComponent.Variant>[] Items { get; set; }

                public override string ToString() => Name;
            }
        }
    }
}

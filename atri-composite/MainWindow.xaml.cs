using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace atri_composite
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(object _ = null, [CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private BitmapSource _image;
        public BitmapSource Image
        {
            get => _image; set
            {
                OnPropertyChanged(_image = value);
                OnPropertyChanged(null, "ImageSize");
                GC.Collect();
            }
        }
        public string ImageSize => Image == null ? "" : $"{Image.PixelWidth}x{Image.PixelHeight}";

        private bool PauseGenerate { get; set; } = false;

        private List<Character> _characters;
        public List<Character> Characters { get => _characters; set => OnPropertyChanged(_characters = value); }

        private Character _selectedCharacter;
        public Character SelectedCharacter { get => _selectedCharacter; set => OnPropertyChanged(_selectedCharacter = value); }

        private Character.Pose _selectedPose;
        public Character.Pose SelectedPose { get => _selectedPose; set => OnPropertyChanged(_selectedPose = value); }

        private Character.Pose.Dress _selectedDress;
        public Character.Pose.Dress SelectedDress { get => _selectedDress; set => OnPropertyChanged(_selectedDress = value); }

        private Character.Pose.Dress.Addition _selectedAddition;
        public Character.Pose.Dress.Addition SelectedAddition { get => _selectedAddition; set => OnPropertyChanged(_selectedAddition = value); }

        private string _selectedSize;
        public string SelectedSize { get => _selectedSize; set => OnPropertyChanged(_selectedSize = value); }

        private Character.Pose.FaceComponent.Variant[] _selectedFaceComponents;
        public Character.Pose.FaceComponent.Variant[] SelectedFaceComponents { get => _selectedFaceComponents; set => OnPropertyChanged(_selectedFaceComponents = value); }

        private string WorkingDirectory { get; }

        public MainWindow()
        {
            var dialog = new CommonOpenFileDialog()
            {
                Title = "Locate the fgimage folder",
                DefaultDirectory = Environment.CurrentDirectory,
                IsFolderPicker = true,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureValidNames = true
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) Environment.Exit(0);

            WorkingDirectory = dialog.FileName;
            Characters = CharacterProcessor.Load(WorkingDirectory);

            InitializeComponent();
        }

        private void ReloadFaceComponents()
        {
            extraConfigPanel.Children.Clear();
            SelectedFaceComponents = null;

            if (SelectedPose == null) return;
            SelectedFaceComponents = new Character.Pose.FaceComponent.Variant[SelectedPose.FaceComponents.Count];

            for (int i = 0; i < SelectedPose.FaceComponents.Count; i++)
            {
                var component = SelectedPose.FaceComponents[i];
                var panel = new DockPanel();

                panel.Children.Add(new Label() { Content = component.Name, Width = 70 });

                ComboBox comboBox = new ComboBox()
                {
                    ItemsSource = component.Variants,
                    VerticalAlignment = VerticalAlignment.Center
                };
                comboBox.SetBinding(Selector.SelectedItemProperty, new Binding()
                {
                    Path = new PropertyPath($"SelectedFaceComponents[{i}]"),
                    Source = this,
                    Mode = BindingMode.TwoWay
                });
                comboBox.SelectionChanged += OnSelectionChanged;
                comboBox.SelectedIndex = 0;
                panel.Children.Add(comboBox);

                extraConfigPanel.Children.Add(panel);
            }
        }

        private void TryBuildImage()
        {
            if (SelectedCharacter != null && SelectedPose != null && SelectedDress != null && SelectedAddition != null && SelectedSize != null && SelectedFaceComponents != null && !PauseGenerate)
            {
                var pbdPath = Path.Combine(WorkingDirectory, SelectedCharacter.Name, $"{SelectedPose.Name}_{SelectedSize}.pbd");

                // also allow images to be placed in the data root
                if (!File.Exists(pbdPath))
                {
                    pbdPath = Path.Combine(Directory.GetParent(Path.GetDirectoryName(pbdPath)).FullName, Path.GetFileName(pbdPath));
                }

                var image = new CompoundImage(pbdPath);
                var layers = new List<string>();
                layers.Add(SelectedDress.LayerPath);
                layers.Add(SelectedAddition.LayerPath);
                layers.AddRange(SelectedFaceComponents.Reverse().Select(o => o.LayerPath));

                try
                {
                    Image = image.Generate(layers.ToArray()).Crop(true).ToBitmapSource(true);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    System.Diagnostics.Trace.TraceError(e.Message);
                    Image = null;
                }
            }
            else Image = null;
        }

        private void OnPoseSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReloadFaceComponents();
            TryBuildImage();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => TryBuildImage();

        private void OnFaceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var key = (sender as ComboBox).SelectedItem;
            if (key == null) return;

            PauseGenerate = true;
            foreach (var o in SelectedPose.Presets.First(o => o.Name == key.ToString()).Items)
            {
                var fGroup = SelectedPose.FaceComponents.First(p => o.Key.StartsWith(p.Name));
                SelectedFaceComponents[SelectedPose.FaceComponents.IndexOf(fGroup)] = o.Value;
            }
            SelectedFaceComponents = SelectedFaceComponents;
            PauseGenerate = false;

            TryBuildImage();
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonSaveFileDialog()
            {
                Title = "Export",
                DefaultDirectory = Environment.CurrentDirectory,
                EnsurePathExists = true,
                EnsureValidNames = true,
                DefaultExtension = "png",
                AlwaysAppendDefaultExtension = true
            };
            dialog.Filters.Add(new CommonFileDialogFilter("PNG Image", ".png"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(Image));
                using (var file = File.Create(dialog.FileName)) encoder.Save(file);
            }
        }

        private void OnExtraMenuClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            ContextMenu contextMenu = button.ContextMenu;
            contextMenu.PlacementTarget = button;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void OnBatchExportClick(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                Title = "Locate the target folder",
                DefaultDirectory = Environment.CurrentDirectory,
                IsFolderPicker = true,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureValidNames = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var limits = new BatchExporter.Limitation();
                foreach (var p in ((sender as FrameworkElement).Tag as string ?? "").Split(',')) switch (p)
                    {
                        case "Character": limits.Character = SelectedCharacter; break;
                        case "Pose": limits.Pose = SelectedPose; break;
                        case "Size": limits.Size = SelectedSize; break;
                        case "Dress": limits.Dress = SelectedDress; break;
                        case "Addition": limits.Addition = SelectedAddition; break;
                    }

                var exporter = new BatchExporter(Characters, WorkingDirectory, dialog.FileName);
                var count = exporter.EnumerateVariants(limits).Count();
                var result = MessageBox.Show($"{count} images will be saved to {dialog.FileName}.\nThis may take a long time!\nProceed?", "Notice", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    var errors = exporter.Run(limits);
                    MessageBox.Show($"{count - errors} images saved. {errors} failed.", "Notice", MessageBoxButton.OK);
                }
            }
        }
    }

    public class ObjectNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Format(parameter as string, values);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

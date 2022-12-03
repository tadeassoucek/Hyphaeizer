using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Hyphaeizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int MAX_IMAGE_SIZE = ushort.MaxValue;

        bool imgSizeChanged = false;
        int _imageWidth, _imageHeight;

        int ImageWidth
        {
            get => _imageWidth;
            set
            {
                imgSizeChanged = true;
                _imageWidth = value;
            }
        }

        int ImageHeight
        {
            get => _imageHeight;
            set
            {
                imgSizeChanged = true;
                _imageHeight = value;
            }
        }

        readonly Simulator sim;
        FastWriteableBitmap bmp;

        public MainWindow()
        {
            InitializeComponent();
            RenderOptions.SetBitmapScalingMode(viewportResult, BitmapScalingMode.NearestNeighbor);

            ImageWidth = 800;
            ImageHeight = 600;

            sim = new();
            bmp = new(ImageWidth, ImageHeight, viewportResult);
            imageWidthTextBox.Text = ImageWidth.ToString();
            imageHeightTextBox.Text = ImageHeight.ToString();
            iterationsTextBox.Text = sim.config.iterations.ToString();
            penIntensityTextBox.Text = sim.config.penIntensity.ToString();
            splitProbabilityTextBox.Text = sim.config.splitProbability.ToString();
            speedTextBox.Text = sim.config.speed.ToString();
            initialSporesTextBox.Text = sim.config.initialSpores.ToString();
            angleChangeModTextBox.Text = sim.config.angleChangeModifier.ToString();
        }

        private void generateButton_Click(object sender, RoutedEventArgs e)
        {
            if (imgSizeChanged)
            {
                bmp = new FastWriteableBitmap(ImageWidth, ImageHeight, viewportResult);
                imgSizeChanged = false;
            }

            sim.GenerateSingleImage(ImageWidth, ImageHeight).PutOnFastWriteableBitmap(bmp);
        }

        private void iterationsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (!int.TryParse(tb.Text, out sim.config.iterations))
                tb.Background = Brushes.Red;
            else
                tb.Background = Brushes.White;
        }

        private void penIntensityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (!float.TryParse(tb.Text, out sim.config.penIntensity))
                tb.Background = Brushes.Red;
            else
                tb.Background = Brushes.White;
        }

        private void splitProbabilityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (!double.TryParse(tb.Text, out sim.config.splitProbability))
                tb.Background = Brushes.Red;
            else
                tb.Background = Brushes.White;
        }

        private void speedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (!double.TryParse(tb.Text, out sim.config.speed))
                tb.Background = Brushes.Red;
            else
                tb.Background = Brushes.White;
        }

        private void initialSporesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (int.TryParse(tb.Text, out sim.config.initialSpores) && sim.config.initialSpores > 0)
                tb.Background = Brushes.White;
            else
                tb.Background = Brushes.Red;
        }

        private void angleChangeModTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (!double.TryParse(tb.Text, out sim.config.angleChangeModifier))
                tb.Background = Brushes.Red;
            else
                tb.Background = Brushes.White;
        }

        private void imageWidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (int.TryParse(tb.Text, out int w) && w > 0 && w < MAX_IMAGE_SIZE)
            {
                ImageWidth = w;
                tb.Background = Brushes.White;
            }
            else
                tb.Background = Brushes.Red;
        }

        private void imageHeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (int.TryParse(tb.Text, out int h) && h > 0 && h < MAX_IMAGE_SIZE)
            {
                ImageHeight = h;
                tb.Background = Brushes.White;
            }
            else
                tb.Background = Brushes.Red;
        }

        private void On_CommandSave(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "PNG files|*.png|All files|*.*",
                Title = "Select where to save image"
            };

            if (dialog.ShowDialog(this) == true)
            {
                using FileStream stream = new(dialog.FileName, FileMode.Create);
                PngBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(bmp.bitmap));
                encoder.Save(stream);
            }
        }
    }
}

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
        const int IMAGE_WIDTH = 1200, IMAGE_HEIGHT = 720;

        readonly Simulator sim;
        readonly FastWriteableBitmap bmp;

        public MainWindow()
        {
            InitializeComponent();
            RenderOptions.SetBitmapScalingMode(viewportResult, BitmapScalingMode.NearestNeighbor);

            sim = new();
            bmp = new(IMAGE_WIDTH, IMAGE_HEIGHT);
            iterationsTextBox.Text = sim.config.iterations.ToString();
            penIntensityTextBox.Text = sim.config.penIntensity.ToString();
            splitProbabilityTextBox.Text = sim.config.splitProbability.ToString();
            speedTextBox.Text = sim.config.speed.ToString();
            initialSporesTextBox.Text = sim.config.initialSpores.ToString();
            angleChangeModTextBox.Text = sim.config.angleChangeModifier.ToString();

            bmp.Attach(viewportResult);
        }

        private void generateButton_Click(object sender, RoutedEventArgs e)
        {
            sim.GenerateSingleImage(IMAGE_WIDTH, IMAGE_HEIGHT).PutOnFastWriteableBitmap(bmp);
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
            if (!int.TryParse(tb.Text, out sim.config.initialSpores))
                tb.Background = Brushes.Red;
            else
                tb.Background = Brushes.White;
        }

        private void angleChangeModTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (!double.TryParse(tb.Text, out sim.config.angleChangeModifier))
                tb.Background = Brushes.Red;
            else
                tb.Background = Brushes.White;
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

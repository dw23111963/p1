using System;
using System.Collections.Generic;
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
using System.Windows.Threading;
namespace p2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        // Поля класса

        private List<TrajectoryPoint> trajectory = new List<TrajectoryPoint>();
        private DispatcherTimer simulationTimer;
        private double simulationTime = 0;
        private List<Obstacle> obstacles = new List<Obstacle>();
        private List<Pig> pigs = new List<Pig>();
        private bool isSimulating = false;
        private double currentBoost = 1.0;
        private double airDensity = 1.225;
        private double dragCoefficient = 0.47;
        private Brush br = Brushes.Blue;
        private double m = 1;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSimulationTimer();
            InitializeSimulationObjects();

            //   canvasTrajectory.SizeChanged += (sender, e) => DrawTrajectory();

        }

        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb == Color)
                switch (Color.SelectedIndex)
                {
                    case 0: br = Brushes.Blue; break;
                    case 1: br = Brushes.Yellow; break;
                    case 2: br = Brushes.Red; break;
                    default: br = Brushes.Blue; break;

                }
            else
                switch (Mass1.SelectedIndex)
                {
                    case 0: m = 1.0; break;
                    case 1: m = 0.7; break;
                    case 2: m = 1.5; break;
                    default: m = 1.0; break;
                }
        }
    }
}

using HelixToolkit.Wpf;
using StardustLibrary.DataSource.Satellite;
using StardustLibrary.Node;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace StardustVisualization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Plot3DCoordinates();
        }


        private void Plot3DCoordinates()
        {
            // Create a collection of 3D points to plot (example coordinates)

            var satellites = (new TleSatelliteConstellationLoader().Load(File.OpenRead("starlink.tle"))).Result;
            List<GroundStation> groundStations =
            [
                new GroundStation("Vienna", 16.3738, 48.2082),           // Austria
                new GroundStation("Washington, D.C.", -77.0369, 38.9072), // USA
                new GroundStation("London", -0.1276, 51.5074),            // UK
                new GroundStation("Tokyo", 139.6917, 35.6895),            // Japan
                new GroundStation("Canberra", 149.1300, -35.2809),        // Australia
                new GroundStation("Ottawa", -75.6972, 45.4215),           // Canada
                new GroundStation("Paris", 2.3522, 48.8566),              // France
                new GroundStation("Beijing", 116.4074, 39.9042),          // China
                new GroundStation("Berlin", 13.4050, 52.5200),            // Germany
                new GroundStation("Brasília", -47.9292, -15.7801),        // Brazil
                new GroundStation("Buenos Aires", -58.3816, -34.6037),    // Argentina
                new GroundStation("Moscow", 37.6173, 55.7558),            // Russia
                new GroundStation("New Delhi", 77.2090, 28.6139),         // India
                new GroundStation("Cairo", 31.2357, 30.0444),             // Egypt
                new GroundStation("Mexico City", -99.1332, 19.4326)       // Mexico
            ];

            groundStations = new List<GroundStation>
            {
                new GroundStation("Istanbul", 41.0082, 28.9784),    // Turkey
                new GroundStation("Moscow", 55.7558, 37.6173),      // Russia
                new GroundStation("London", 51.5074, -0.1276),      // UK
                new GroundStation("Saint Petersburg", 59.9343, 30.3351), // Russia
                new GroundStation("Berlin", 52.5200, 13.4050),      // Germany
                new GroundStation("Madrid", 40.4168, -3.7038),      // Spain
                new GroundStation("Kyiv", 50.4501, 30.5234),        // Ukraine
                new GroundStation("Rome", 41.9028, 12.4964),        // Italy
                new GroundStation("Bucharest", 44.4268, 26.1025),   // Romania
                new GroundStation("Paris", 48.8566, 2.3522),        // France
                new GroundStation("Vienna", 48.2082, 16.3738),      // Austria
                new GroundStation("Warsaw", 52.2297, 21.0122),      // Poland
                new GroundStation("Hamburg", 53.5511, 9.9937),      // Germany
                new GroundStation("Budapest", 47.4979, 19.0402),    // Hungary
                new GroundStation("Belgrade", 44.7866, 20.4489),    // Serbia
                new GroundStation("Barcelona", 41.3851, 2.1734),    // Spain
                new GroundStation("Munich", 48.1351, 11.5820),      // Germany
                new GroundStation("Kharkiv", 49.9935, 36.2304),     // Ukraine
                new GroundStation("Milan", 45.4642, 9.1900),        // Italy
                new GroundStation("Sofia", 42.6977, 23.3219),       // Bulgaria
                new GroundStation("Prague", 50.0755, 14.4378),      // Czech Republic
                new GroundStation("Cologne", 50.9375, 6.9603),      // Germany
                new GroundStation("Stockholm", 59.3293, 18.0686),   // Sweden
                new GroundStation("Naples", 40.8518, 14.2681),      // Italy
                new GroundStation("Amsterdam", 52.3676, 4.9041),    // Netherlands
                new GroundStation("Marseille", 43.2965, 5.3698),    // France
                new GroundStation("Turin", 45.0703, 7.6869),        // Italy
                new GroundStation("Kraków", 50.0647, 19.9445),      // Poland
                new GroundStation("Valencia", 39.4699, -0.3757),    // Spain
                new GroundStation("Zagreb", 45.8150, 15.9819),      // Croatia
                new GroundStation("Frankfurt", 50.1109, 8.6821),    // Germany
                new GroundStation("Seville", 37.3891, -5.9845),     // Spain
                new GroundStation("Łódź", 51.7592, 19.4550),        // Poland
                new GroundStation("Helsinki", 60.1695, 24.9410),    // Finland
                new GroundStation("Copenhagen", 55.6761, 12.5683),  // Denmark
                new GroundStation("Washington, D.C.", 38.9072, -77.0369), // USA
            };


            var satPoints = new Point3DCollection();
            var groundPoints = new Point3DCollection();
            var earth = new Point3DCollection
            {
                new Point3D(0, 0, 0)
            };

            foreach (var groundStation in groundStations)
            {
                groundPoints.Add(new Point3D(groundStation.Position.X, groundStation.Position.Y, groundStation.Position.Z));
                //MessageBox.Show($"Ground Station: {groundStation.Name} {groundStation.Position.X}, {groundStation.Position.Y}, {groundStation.Position.Z}");
            }

            // Plot ground stations in green
            PlotPoints(groundPoints, Colors.Green, 100_000);
            PlotPoints(earth, Colors.Gray, Physics.EARTH_RADIUS);

            DateTime simTime = DateTime.UtcNow;
            Task.Run(async () =>
            {
                int steps = 1;
                for (int i = 0; i < steps; i++)
                {
                    foreach (var sat in satellites)
                    {
                        await sat.UpdatePosition(simTime);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        foreach (var sat in satellites)
                        {
                            satPoints.Add(new Point3D(sat.Position.X, sat.Position.Y, sat.Position.Z));
                            //MessageBox.Show($"Satellite: {sat.Position.X}, {sat.Position.Y}, {sat.Position.Z}");
                        }

                        // Plot satellites in blue
                        PlotPoints(satPoints, Colors.Blue, 30_000);
                        simTime = simTime.AddSeconds(3 * 60);
                    });

                    await Task.Delay(3_000);
                }
            });
        }

        private void PlotPoints(Point3DCollection points, Color color, double size)
        {
            MeshBuilder meshBuilder = new MeshBuilder();
            foreach (var point in points)
            {
                meshBuilder.AddSphere(point, size); // Adjust size of spheres for satellites/ground stations
            }

            var mesh = meshBuilder.ToMesh();
            var material = MaterialHelper.CreateMaterial(color);

            var model = new GeometryModel3D(mesh, material);
            var modelVisual= new ModelVisual3D { Content = model };

            // Add the model to the viewport
            helixView.Children.Add(modelVisual);
            return;

            var linesVisual = new LinesVisual3D
            {
                Color = color,
                Thickness = 2
            };
            for (int i = 0; i < points.Count - 1; i++)
            {
                linesVisual.Points.Add(points[i]);
                linesVisual.Points.Add(points[i + 1]);
            }
            helixView.Children.Add(linesVisual);
        }
    }
}
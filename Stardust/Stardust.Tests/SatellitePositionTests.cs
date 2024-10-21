using Microsoft.Extensions.Logging;
using Moq;
using Stardust.Abstraction.Links;
using StardustLibrary.DataSource.Satellite;

namespace Stardust.Tests
{
    [TestClass]
    public class SatellitePositionTests
    {
        private Mock<ILogger<SatelliteConstellationLoader>> mockLogger = new();

        private SatelliteConstellationLoader loader1;

        [TestInitialize]
        public void Initialize()
        {
            loader1 = new SatelliteConstellationLoader(mockLogger.Object);
            var tleLoader = new TleSatelliteConstellationLoader(new InterSatelliteLinkConfig { Neighbours = 4, Protocol = "other_mst" }, loader1);
        }

        [TestMethod]
        public async Task TestTwoTleFilesAssertPositions()
        {
            var datetime = new DateTime(2024, 10, 21, 7, 59, 28);
            var satellites1 = await loader1.LoadSatelliteConstellation("starlink_20241021_073036.tle", "tle");
            var satellites2 = await loader1.LoadSatelliteConstellation("starlink_20241021_075928.tle", "tle");

            Assert.AreEqual(6469, satellites1.Count, 0);
            Assert.AreEqual(6469, satellites2.Count, 0);

            var pairs = satellites1.Select(s1 => (s1, satellites2.First(s2 => s1.Name == s2.Name)));

            Assert.AreEqual(6469, pairs.Count(), 0);

            foreach ((var s1, var s2) in pairs)
            {
                await s1.UpdatePosition(datetime);
                await s2.UpdatePosition(datetime);
                Assert.AreEqual(s1.Position.X, s2.Position.X, 0);
                Assert.AreEqual(s1.Position.Y, s2.Position.Y, 0);
                Assert.AreEqual(s1.Position.Z, s2.Position.Z, 0);
            }
        }
    }
}
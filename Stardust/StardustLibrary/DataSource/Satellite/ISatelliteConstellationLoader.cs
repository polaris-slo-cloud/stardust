using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace StardustLibrary.DataSource.Satellite;

public interface ISatelliteConstellationLoader
{
    public Task<List<Node.Satellite>> Load(Stream stream);
}
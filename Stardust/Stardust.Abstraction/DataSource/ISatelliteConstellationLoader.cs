using Stardust.Abstraction.Node;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Stardust.Abstraction.DataSource;

public interface ISatelliteConstellationLoader
{
    public Task<List<Satellite>> Load(Stream stream);
}
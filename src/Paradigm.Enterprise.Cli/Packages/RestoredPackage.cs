using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal sealed record RestoredPackage(string Name, string Version, string Project, bool Direct);
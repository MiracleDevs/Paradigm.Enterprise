using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;
internal sealed class AssetsException(string message) : Exception(message);

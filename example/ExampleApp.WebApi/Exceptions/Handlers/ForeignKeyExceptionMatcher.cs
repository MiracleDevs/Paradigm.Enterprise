using Microsoft.Data.SqlClient;
using Paradigm.Enterprise.WebApi.Exceptions.Handlers;
using System.Resources;
using System.Text.RegularExpressions;

namespace ExampleApp.WebApi.Exceptions.Handlers
{
    public partial class ForeignKeyExceptionMatcher : ExceptionMatcherBase
    {
        [GeneratedRegex("The (.*) statement conflicted with the (.*) constraint \"(.*)\". The conflict occurred in database \"(.*)\", table \"(.*)\"")]
        private static partial Regex MyRegex();

        private const string DefaultMessageKey = "ForeignKeyDefaultMessage";

        public override bool Match(Exception ex)
        {
            return ex is SqlException { Number: 547 };
        }

        public override string GetMessageKey(ResourceManager resourceManager, Exception ex)
        {
            var match = MyRegex().Match(ex.Message);
            var key = match.Groups[2].Value;

            var message = resourceManager.GetString(key) ?? resourceManager.GetString(DefaultMessageKey);
            return message ?? string.Empty;
        }
    }
}
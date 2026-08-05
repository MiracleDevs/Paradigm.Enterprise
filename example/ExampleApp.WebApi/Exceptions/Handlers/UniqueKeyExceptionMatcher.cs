using Microsoft.Data.SqlClient;
using Paradigm.Enterprise.WebApi.Exceptions.Handlers;
using System.Resources;
using System.Text.RegularExpressions;

namespace ExampleApp.WebApi.Exceptions.Handlers
{
    public partial class UniqueKeyExceptionMatcher : ExceptionMatcherBase
    {
        #region Constants

        private const string DefaultMessageKey = "UniqueKeyDefaultMessage";

        #endregion

        #region Overrides

        public override bool Match(Exception ex)
        {
            return ex is SqlException { Number: 2601 or 2627 }; //we used 2627 which includes primary keys, because we never send ids.
        }

        public override string? GetMessageKey(ResourceManager resourceManager, Exception ex)
        {
            var match = MyRegex().Match(ex.Message);
            var key = match.Groups[2].Value;
            var table = match.Groups[3].Value.Replace("dbo.", "");

            var message = resourceManager.GetString(key) ?? resourceManager.GetString(DefaultMessageKey);
            return message is not null ? string.Format(message, table) : null;
        }

        #endregion

        #region Private Methods

        [GeneratedRegex("Violation of (.*) constraint '(.*)'. Cannot insert duplicate key in object '(.*)'. The duplicate key value is \\((.*)\\).")]
        private static partial Regex MyRegex();

        #endregion
    }
}

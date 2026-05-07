using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Text.Json;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Json
{
    internal class JsonRow : RowBase
    {
        #region Properties

        /// <summary>
        /// Gets the json items.
        /// </summary>
        /// <value>
        /// The json items.
        /// </value>
        private IReadOnlyList<JsonElement> Items { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonRow"/> class.
        /// </summary>
        /// <param name="tableSchema">The table schema.</param>
        /// <param name="items">The json items.</param>
        public JsonRow(ITableSchema tableSchema, IReadOnlyList<JsonElement> items) : base(tableSchema)
        {
            Items = items;
        }

        #endregion

        #region Abstract implementations

        /// <summary>
        /// Reads a new row.
        /// </summary>
        /// <returns></returns>
        public override bool Read()
        {
            if (Index >= Items.Count)
                return false;

            var item = Items[Index];

            if (item.ValueKind != JsonValueKind.Object)
                throw new FormatException("Each json item must be an object.");

            Values = item
                .EnumerateObject()
                .Select(x => GetValueAsString(x.Value))
                .ToList();

            Index++;

            if (Values.Count == 0 || (Values.Count == 1 && string.IsNullOrEmpty(Values[0])))
                return false;

            ValidateValuesSchema();

            return true;
        }

        /// <summary>
        /// Converts a JSON value to its scalar string representation.
        /// </summary>
        /// <param name="value">The JSON value.</param>
        /// <returns>The scalar string representation.</returns>
        private static string GetValueAsString(JsonElement value)
        {
            return value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                ? string.Empty
                : value.ToString();
        }

        #endregion
    }
}

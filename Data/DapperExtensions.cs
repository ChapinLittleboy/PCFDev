using System.Dynamic;


namespace BlazorServerDatagridApp2.Data;

public static class DapperExtensions
{
    public static IEnumerable<ExpandoObject> ToExpandoObjects(this IEnumerable<dynamic> rows)
    {
        foreach (var row in rows)
        {
            var expando = new ExpandoObject();
            var dict = expando as IDictionary<string, object>;

            // Cast DapperRow to IDictionary<string, object>
            var dapperRow = row as IDictionary<string, object>;
            if (dapperRow == null)
                continue;

            foreach (var key in dapperRow.Keys)
            {
                dict.Add(key, dapperRow[key]);
            }
            yield return expando;
        }
    }
}

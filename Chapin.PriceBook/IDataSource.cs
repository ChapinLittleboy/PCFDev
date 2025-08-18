using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapin.PriceBook;
public interface IDataSource
{
    string Key { get; } // e.g., "sql", "alt"
    Task<IReadOnlyList<PriceBookRow>> GetRowsAsync(bool excludeFuturePrices, CancellationToken ct);
}


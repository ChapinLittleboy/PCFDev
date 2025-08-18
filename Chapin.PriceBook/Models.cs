using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapin.PriceBook;

public sealed record PriceBookRequest(
    string TemplatePath,
    string SourceKey = "sql",
    bool ExcludeFuturePrices = true,
    string? OutputFileName = null
);

public sealed record PriceBookRow(
    string ComboId,        // WS01-SEC01-SS01-ACC00
    int WS, int Sec, int SS, int Acc,
    string DisplayLabel,   // "9_1_2025 Price Book > SPRAYERS > MULTI-USE POLY SPRAYERS"
    string Item,           // Item code
    string Description,    // From im.Uf_CustomerFriendlyDescription (fallback to s.Description)
    decimal? ListPrice,    // unit_price1
    decimal? Ppd4000,      // unit_price2
    decimal? Ppd12500      // unit_price3
);

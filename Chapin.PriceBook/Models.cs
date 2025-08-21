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
    decimal? ListPrice,    // unit_price1 List Price
    decimal? PP1,      // unit_price2   Prepaid 4k
    decimal? PP2 ,     // unit_price3   Prepaid 12.5k
    decimal? BM1,     // unit_price4 
    decimal? BM2 ,     // unit_price5
    decimal? FOB      // unit_price6
);

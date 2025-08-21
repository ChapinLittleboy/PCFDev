using PcfManager.Data;
using PcfManager.Models;
using System.Data;
using Dapper;


namespace PcfManager.Services;




    public interface IPriceCalculationService
    {
        /// <summary>
        /// Calculate simulated new prices for all items in the given family codes,
        /// applying the specified percentage increase per family, using the given source.
        /// </summary>
        /// <param name="familyCodes">Codes to include</param>
        /// <param name="percentages">Map familyCode→percent (e.g. 5.0 for +5%)</param>
        /// <param name="sourceKind">PCF vs StaticList</param>
        /// <param name="listId">If sourceKind==StaticList, the price‐list identifier</param>
        Task<IEnumerable<PriceResult>> CalculateAsync(
            IEnumerable<string> familyCodes,
            IReadOnlyDictionary<string, double> percentages,
            PriceSourceKind sourceKind,
            string? listId = null);
    }


// Services/PriceCalculationService.cs



    public class PriceCalculationService : IPriceCalculationService
    {
        private readonly DbConnectionFactory _factory;
        private IDbConnection _dbPCF;
        private IDbConnection _dbERP;

    public PriceCalculationService(DbConnectionFactory factory)
        {
            _factory = factory;
             _dbERP = factory.CreateReadOnlyConnection("Bat_App");
             _dbPCF = factory.CreateReadWriteConnection("custinfo");

    }

        public async Task<IEnumerable<PriceResult>> CalculateAsync(
            IEnumerable<string> familyCodes,
            IReadOnlyDictionary<string, double> percentages,
            PriceSourceKind sourceKind,
            string? listId = null)
        {
            if (sourceKind == PriceSourceKind.StaticList && string.IsNullOrWhiteSpace(listId))
                throw new ArgumentException("listId is required when using StaticList source");

            var codesArray = familyCodes.Distinct().ToArray();
            if (!codesArray.Any())
                return Enumerable.Empty<PriceResult>();

            // 1) Fetch current prices
            string sql;
            object parameters;
            if (sourceKind == PriceSourceKind.PCF)
            {
                sql = @"
                    SELECT
                        family_code AS FamilyCode,
                        item_code   AS ItemCode,
                        price       AS OldPrice
                    FROM PCFRecords
                    WHERE family_code IN @Codes
                ";
                parameters = new { Codes = codesArray };
            }
            else
            {
                sql = @"
                    SELECT
                        family_code AS FamilyCode,
                        item_code   AS ItemCode,
                        price       AS OldPrice
                    FROM PriceListDetails
                    WHERE list_id = @ListId
                      AND family_code IN @Codes
                ";
                parameters = new { ListId = listId!, Codes = codesArray };
            }

            var raw = await _dbPCF.QueryAsync<PriceResult>(sql, parameters);

            // 2) Apply increases in memory
            var results = raw.Select(r =>
            {
                // look up our percentage (default to 0 if somehow missing)
                percentages.TryGetValue(r.FamilyCode, out var pct);
                var factor = 1 + ((decimal)pct / 100m);
                r.NewPrice = Math.Round(r.OldPrice * factor, 4); // round to 4 decimals
                return r;
            }).ToList();

            return results;
        }
    }

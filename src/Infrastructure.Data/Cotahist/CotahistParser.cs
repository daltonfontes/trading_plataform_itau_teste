using System.Text;

namespace Infrastructure.Data.Cotahist;

public record CotahistRecord(
    DateTime TradingDate,
    string Ticker,
    decimal ClosingPrice
);

public class CotahistParser
{
    // B3 COTAHIST fixed-width layout (1-indexed, ISO-8859-1, 245 chars/line)
    // Type:    pos 1-2   ("01" = detail record)
    // BDI:     pos 11-12 ("02" = standard lot, "96" = fractional)
    // Ticker:  pos 13-24 (12 chars, trim)
    // Market:  pos 25-27 ("010" = spot, "020" = fractional)
    // Date:    pos 3-10  (YYYYMMDD)
    // PREULT:  pos 109-121 (13 digits integer, divide by 100 = R$)

    private static readonly HashSet<string> ValidBdi    = ["02", "96"];
    private static readonly HashSet<string> ValidMarket = ["010", "020"];

    public IEnumerable<CotahistRecord> Parse(string filePath)
    {
        var encoding = Encoding.GetEncoding("ISO-8859-1");

        foreach (var line in File.ReadLines(filePath, encoding))
        {
            if (line.Length < 121) continue;

            string recordType = line.Substring(0, 2);
            if (recordType != "01") continue;

            string bdi    = line.Substring(10, 2);
            string market = line.Substring(24, 3);

            if (!ValidBdi.Contains(bdi) || !ValidMarket.Contains(market)) continue;

            string dateStr   = line.Substring(2, 8);
            string ticker    = line.Substring(12, 12).Trim();
            string priceStr  = line.Substring(108, 13).Trim();

            if (!DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out DateTime tradingDate)) continue;

            if (!long.TryParse(priceStr, out long priceRaw)) continue;

            decimal closingPrice = priceRaw / 100m;

            yield return new CotahistRecord(tradingDate, ticker, closingPrice);
        }
    }
}

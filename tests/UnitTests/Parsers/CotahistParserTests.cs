using Infrastructure.Data.Cotahist;
using System.Text;
using Xunit;

namespace UnitTests.Parsers;

public class CotahistParserTests
{
    private readonly CotahistParser _parser = new();

    private static string BuildLine(string recordType, string date, string bdi, string ticker, string market, long priceRaw)
    {
        // Layout COTAHIST (245 chars, 1-indexed):
        // pos 1-2:   record type
        // pos 3-10:  date YYYYMMDD
        // pos 11-12: BDI
        // pos 13-24: ticker (12 chars)
        // pos 25-27: market
        // pos 109-121: closing price (13 digits integer)

        var line = new StringBuilder(new string(' ', 245));
        void Set(int pos1, string val) { for (int i = 0; i < val.Length; i++) line[pos1 - 1 + i] = val[i]; }

        Set(1, recordType);
        Set(3, date);
        Set(11, bdi);
        Set(13, ticker.PadRight(12));
        Set(25, market);
        Set(109, priceRaw.ToString().PadLeft(13, '0'));

        return line.ToString();
    }

    private static string WriteToTempFile(IEnumerable<string> lines)
    {
        var path = Path.GetTempFileName();
        File.WriteAllLines(path, lines, Encoding.GetEncoding("ISO-8859-1"));
        return path;
    }

    [Fact]
    public void Parse_ValidStandardLotLine_ReturnsRecord()
    {
        string line = BuildLine("01", "20260225", "02", "PETR4", "010", 3500); // R$ 35,00
        var path = WriteToTempFile(new[] { line });

        var records = _parser.Parse(path).ToList();

        Assert.Single(records);
        Assert.Equal("PETR4", records[0].Ticker);
        Assert.Equal(new DateTime(2026, 2, 25), records[0].TradingDate);
        Assert.Equal(35.00m, records[0].ClosingPrice);
    }

    [Fact]
    public void Parse_ValidFractionalLine_ReturnsRecord()
    {
        string line = BuildLine("01", "20260225", "96", "PETR4F", "020", 3502); // R$ 35,02
        var path = WriteToTempFile(new[] { line });

        var records = _parser.Parse(path).ToList();

        Assert.Single(records);
        Assert.Equal("PETR4F", records[0].Ticker);
        Assert.Equal(35.02m, records[0].ClosingPrice);
    }

    [Fact]
    public void Parse_HeaderLine_IsIgnored()
    {
        string header = BuildLine("00", "20260225", "02", "PETR4", "010", 3500);
        var path = WriteToTempFile(new[] { header });

        var records = _parser.Parse(path).ToList();

        Assert.Empty(records);
    }

    [Fact]
    public void Parse_InvalidBdi_IsIgnored()
    {
        string line = BuildLine("01", "20260225", "99", "PETR4", "010", 3500); // BDI inválido
        var path = WriteToTempFile(new[] { line });

        var records = _parser.Parse(path).ToList();

        Assert.Empty(records);
    }

    [Fact]
    public void Parse_MultipleValidLines_ReturnsAll()
    {
        var lines = new[]
        {
            BuildLine("01", "20260225", "02", "PETR4", "010", 3500),
            BuildLine("01", "20260225", "02", "VALE3", "010", 6200),
            BuildLine("01", "20260225", "02", "ITUB4", "010", 3000),
        };
        var path = WriteToTempFile(lines);

        var records = _parser.Parse(path).ToList();

        Assert.Equal(3, records.Count);
        Assert.Equal(35.00m, records[0].ClosingPrice);
        Assert.Equal(62.00m, records[1].ClosingPrice);
        Assert.Equal(30.00m, records[2].ClosingPrice);
    }

    [Fact]
    public void Parse_ShortLine_IsIgnored()
    {
        var path = WriteToTempFile(new[] { "01short" });

        var records = _parser.Parse(path).ToList();

        Assert.Empty(records);
    }
}

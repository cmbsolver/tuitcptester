using System.Text.Json;
using tuitcptester.Models;

namespace tuitcptester.Tests;

public class ModelsTests
{
    [Fact]
    public void Transaction_HasExpectedDefaults()
    {
        var tx = new Transaction();

        Assert.Equal(string.Empty, tx.Data);
        Assert.Equal(TransactionEncoding.Ascii, tx.Encoding);
        Assert.False(tx.AppendReturn);
        Assert.False(tx.AppendNewline);
    }

    [Fact]
    public void TcpConfiguration_HasExpectedDefaults()
    {
        var config = new TcpConfiguration();

        Assert.Equal(string.Empty, config.Name);
        Assert.Equal("127.0.0.1", config.Host);
        Assert.Empty(config.AutoTransactions);
        Assert.Null(config.IntervalMs);
        Assert.Null(config.JitterMinMs);
        Assert.Null(config.JitterMaxMs);
        Assert.Null(config.DumpFilePath);
        Assert.False(config.IncludePayloadHexDump);
    }

    [Fact]
    public void AppConfig_DefaultConnections_IsNotNullAndEmpty()
    {
        var config = new AppConfig();

        Assert.NotNull(config.Connections);
        Assert.Empty(config.Connections);
    }

    [Fact]
    public void AppConfig_Deserialization_RoundTripPreservesConnectionData()
    {
        const string json = """
                            {
                              "Connections": [
                                {
                                  "Name": "Conn1",
                                  "Type": 1,
                                  "Host": "localhost",
                                  "Port": 8080,
                                  "IncludePayloadHexDump": true,
                                  "AutoTransactions": [
                                    {
                                      "Data": "AA BB",
                                      "Encoding": 1,
                                      "AppendReturn": true,
                                      "AppendNewline": false
                                    }
                                  ]
                                }
                              ]
                            }
                            """;

        var config = JsonSerializer.Deserialize<AppConfig>(json);

        Assert.NotNull(config);
        Assert.Single(config.Connections);
        var conn = config.Connections[0];
        Assert.Equal("Conn1", conn.Name);
        Assert.Equal(ConnectionType.Client, conn.Type);
        Assert.Equal("localhost", conn.Host);
        Assert.Equal(8080, conn.Port);
        Assert.True(conn.IncludePayloadHexDump);
        Assert.NotNull(conn.AutoTransactions);
    }
}

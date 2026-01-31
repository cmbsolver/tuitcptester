using tuitcptester.Logic;
using tuitcptester.Models;
using tuitcptester.ViewModels;

namespace tuitcptester.Tests;

public class MainViewModelTests
{
    [Fact]
    public void AddLog_KeepsOnlyMaxEntries()
    {
        var vm = new MainViewModel();

        for (var i = 0; i < 55; i++)
        {
            vm.AddLog(new LogEntry { Timestamp = DateTime.Now, ConnectionName = "C", Message = i.ToString() });
        }

        Assert.Equal(50, vm.Logs.Count);
        Assert.Equal("54", vm.Logs[0].Message);
        Assert.Equal("5", vm.Logs[^1].Message);
    }

    [Fact]
    public void FormatLogEntry_FormatsExpectedLine()
    {
        var entry = new LogEntry
        {
            Timestamp = new DateTime(2025, 1, 1, 12, 34, 56),
            ConnectionName = "ConnA",
            Message = "Hello"
        };

        var line = MainViewModel.FormatLogEntry(entry);

        Assert.Equal("[12:34:56] [ConnA] Hello", line);
    }

    [Fact]
    public void ClearLogs_EmptiesCollection()
    {
        var vm = new MainViewModel();
        vm.AddLog(new LogEntry { Timestamp = DateTime.Now, ConnectionName = "C", Message = "M" });

        vm.ClearLogs();

        Assert.Empty(vm.Logs);
    }

    [Fact]
    public void AddAndRemoveInstance_UpdatesInstancesCollection()
    {
        var vm = new MainViewModel();
        var instance = new TcpInstance(new TcpConfiguration { Name = "X", Type = ConnectionType.Server, Port = 0 });

        vm.AddInstance(instance);
        Assert.Single(vm.Instances);

        vm.RemoveInstance(instance);
        Assert.Empty(vm.Instances);
    }

    [Fact]
    public void ImportConfiguration_InvalidJson_AddsParseFailureLog()
    {
        var vm = new MainViewModel();

        vm.ImportConfiguration("{not json}");

        Assert.Contains(vm.Logs, l => l.ConnectionName == "CONFIG" && l.Message.Contains("Failed to parse configuration"));
    }

    [Fact]
    public void ImportConfiguration_NullPayload_AddsInvalidPayloadLog()
    {
        var vm = new MainViewModel();

        vm.ImportConfiguration("null");

        Assert.Contains(vm.Logs,
            l => l.ConnectionName == "CONFIG" && l.Message.Contains("empty or invalid payload", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ImportConfiguration_InstanceStartFailure_IsLogged()
    {
        var vm = new MainViewModel();
        const string json = """
                            {
                              "Connections": [
                                {
                                  "Name": "BrokenProxy",
                                  "Type": 2,
                                  "Host": "127.0.0.1",
                                  "Port": 9000
                                }
                              ]
                            }
                            """;

        vm.ImportConfiguration(json);

        Assert.Single(vm.Instances);
        Assert.Contains(vm.Logs,
            l => l.ConnectionName == "ERROR/BrokenProxy" && l.Message.Contains("Failed to start instance"));
    }
}

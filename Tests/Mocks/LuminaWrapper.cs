using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Newtonsoft.Json;

namespace Tests.Mocks;

public class LuminaWrapper : IDataManager {
    private readonly GameData _gameData;

    public LuminaWrapper() {
        _gameData = new GameData(Path.Combine(GetFFXIVPath(), "sqpack"));
    }
    public static string GetFFXIVPath() {
        var ffxivPath = Environment.GetEnvironmentVariable("FFXIVPath");

        var outputDir = AppContext.BaseDirectory;
        var settingsPath = Path.Combine(outputDir, "localsettings.json");

        if (string.IsNullOrEmpty(ffxivPath) && File.Exists(settingsPath)) {
            var json = File.ReadAllText(settingsPath);
            var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            ffxivPath = obj?["FFXIVPath"];
        }

        if (string.IsNullOrEmpty(ffxivPath)) {
            throw new Exception("Please set FFXIVPath via environment variable or localsettings.json.");
        }

        return ffxivPath;
    }

    public ExcelSheet<T> GetExcelSheet<T>(ClientLanguage? language = null, string name = null) where T : struct, IExcelRow<T> =>
        _gameData.GetExcelSheet<T>();

    public SubrowExcelSheet<T> GetSubrowExcelSheet<T>(ClientLanguage? language = null, string name = null) where T : struct, IExcelSubrow<T> =>
        throw new NotImplementedException();

    public FileResource GetFile(string path) =>
        throw new NotImplementedException();

    public T GetFile<T>(string path) where T : FileResource =>
        throw new NotImplementedException();

    public Task<T> GetFileAsync<T>(string path, CancellationToken cancellationToken) where T : FileResource =>
        throw new NotImplementedException();

    public bool FileExists(string path) =>
        throw new NotImplementedException();

    ClientLanguage IDataManager.Language => throw new NotImplementedException();

    public GameData GameData => throw new NotImplementedException();

    public ExcelModule Excel => throw new NotImplementedException();

    public bool HasModifiedGameDataFiles => throw new NotImplementedException();
}
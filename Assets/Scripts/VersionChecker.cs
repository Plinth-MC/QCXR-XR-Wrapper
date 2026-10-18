using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class VersionChecker : MonoBehaviour
{
    public string currentPublicVersion;
    public TextMeshProUGUI updateText;
    public TextMeshProUGUI version;
    string currentVersion;

    [ContextMenu("Check Version")]
    public async void Start()
    {
        currentVersion = Application.version;
        version.text = "Plinth " + currentVersion;

        string apkPath = Application.dataPath;
        string visor = await Task.Run(() => ReadVisorVersion(apkPath));
        if (!string.IsNullOrEmpty(visor))
            version.text += " · Visor " + visor;
    }

    // Visor ships inside the APK as assets/mods/Visor.jar, so read its fabric.mod.json from there.
    static string ReadVisorVersion(string apkPath)
    {
        try
        {
            using ZipArchive apk = new ZipArchive(File.OpenRead(apkPath), ZipArchiveMode.Read);
            ZipArchiveEntry jarEntry = apk.GetEntry("assets/mods/Visor.jar");
            if (jarEntry == null) return null;

            using MemoryStream jarBytes = new MemoryStream();
            using (Stream s = jarEntry.Open()) s.CopyTo(jarBytes);
            jarBytes.Position = 0;

            using ZipArchive jar = new ZipArchive(jarBytes, ZipArchiveMode.Read);
            ZipArchiveEntry modJson = jar.GetEntry("fabric.mod.json");
            if (modJson == null) return null;

            using StreamReader reader = new StreamReader(modJson.Open());
            Match m = Regex.Match(reader.ReadToEnd(), "\"version\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : null;
        }
        catch (Exception e)
        {
            Debug.Log("Couldn't read Visor version: " + e.Message);
            return null;
        }
    }

    public void CheckVersion()
    {
        Debug.Log($"Comparing Current version: {currentVersion} with Latest: {currentPublicVersion}");

        if (!Version.TryParse(currentVersion, out var unused1) ||
            !Version.TryParse(currentPublicVersion, out var unused2))
        {
            Debug.Log("Couldn't parse one or both versions, assuming latest.");
            return;
        }
        
        if (Version.Parse(currentVersion) >= Version.Parse(currentPublicVersion))
        {
            Debug.Log("Up to date.");
            return;
        }
        
        Debug.Log("QuestCraft update available.");
        updateText.text += " <color=#D70A53>| Update Available!</color>";
    }
}
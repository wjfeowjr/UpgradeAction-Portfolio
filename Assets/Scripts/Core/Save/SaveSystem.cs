// 세이브 파일 입출력 (JSON 직렬화)

using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class SaveSystem
{
    // 원하는대로 폴더명 바꾸면 됨 (ex: "Save", "Saves", "Profile" 등)
    private const string SaveFolderName = "Save";

    /// <summary>
    /// 예: Windows 기준
    /// C:\Users\{User}\AppData\LocalLow\{CompanyName}\{ProductName}\Saves
    /// </summary>
    public static string SaveDirectory
    {
        get
        {
            // persistentDataPath = LocalLow\CompanyName\ProductName
            return Path.Combine(Application.persistentDataPath, SaveFolderName);
        }
    }

    /// <summary>
    /// 파일명에 .json이 없으면 자동으로 붙임
    /// </summary>
    public static string GetSavePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("fileName is null/empty");

        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            fileName += ".json";

        return Path.Combine(SaveDirectory, fileName);
    }

    public static void Save<T>(string fileName, T data)
    {
        try
        {
            Directory.CreateDirectory(SaveDirectory);

            // JsonUtility는 클래스/구조체의 public 필드 또는 [SerializeField] 필드만 직렬화함
            string json = JsonUtility.ToJson(data, prettyPrint: true);

            string path = GetSavePath(fileName);
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";

            // 원자적 쓰기: 임시 파일에 먼저 쓰고 교체 (쓰기 도중 크래시가 나도 원본은 보존됨)
            // 한글 등 깨짐 방지: UTF8
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            if (File.Exists(path))
                File.Replace(tempPath, path, backupPath); // 교체하면서 직전 세이브를 .bak으로 백업
            else
                File.Move(tempPath, path); // 최초 저장은 원본이 없어서 Replace 불가

#if UNITY_EDITOR
            Debug.Log($"[SaveSystem] Saved");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed ({fileName}): {e}");
        }
    }

    public static bool TryLoad<T>(string fileName, out T data)
    {
        string path = GetSavePath(fileName);

        // 원본이 깨졌으면 직전 세이브 백업(.bak)으로 폴백
        if (TryLoadFile(path, out data))
            return true;

        if (TryLoadFile(path + ".bak", out data))
        {
            Debug.LogWarning($"[SaveSystem] 원본 로드 실패, 백업으로 복구됨 ({fileName})");
            return true;
        }

        return false;
    }

    private static bool TryLoadFile<T>(string path, out T data)
    {
        data = default;

        try
        {
            if (!File.Exists(path))
                return false;

            string json = File.ReadAllText(path, Encoding.UTF8);
            data = JsonUtility.FromJson<T>(json);
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed ({path}): {e}");
            return false;
        }
    }

    public static bool Exists(string fileName)
    {
        string path = GetSavePath(fileName);
        return File.Exists(path);
    }

    public static void Delete(string fileName)
    {
        try
        {
            string path = GetSavePath(fileName);
            if (File.Exists(path))
                File.Delete(path);

            // 백업/임시 파일도 함께 삭제 (남겨두면 TryLoad 폴백으로 지운 세이브가 부활함)
            if (File.Exists(path + ".bak"))
                File.Delete(path + ".bak");
            if (File.Exists(path + ".tmp"))
                File.Delete(path + ".tmp");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Delete failed ({fileName}): {e}");
        }
    }

    public static void Copy(string srcFileName, string dstFileName)
    {
        try
        {
            string srcPath = GetSavePath(srcFileName);
            string dstPath = GetSavePath(dstFileName);
            if (File.Exists(srcPath))
                File.Copy(srcPath, dstPath, overwrite: true);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Copy failed ({srcFileName} → {dstFileName}): {e}");
        }
    }
}

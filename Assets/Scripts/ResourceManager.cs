using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public class ResourceManager : SingletonMono<ResourceManager>
{
    public async UniTask Init()
    {
        await Addressables.InitializeAsync();
        Debug.Log($"{name} 초기화 완료");
    }

    // 리소스 불러오기(비동기)
    public async UniTask<T> LoadAssetAsync<T>(string path)
    {
        try
        {
            //키에 맞는 번들이 존재하는지 확인 후 리소스를 리턴한다
            var locations = await Addressables.LoadResourceLocationsAsync(path).Task;
            if (locations.Any())
            {
                var obj = await Addressables.LoadAssetAsync<T>(path).Task;
                return obj;
            }
            else
                return default(T);
        }
        catch (Exception ex)
        {
            Debug.Log($"불러올 에셋이 Null입니다 : {ex.Message}");
        }

        return default(T);
    }

    // 리소스 불러오기(동기)
    public T LoadAsset<T>(string path)
    {
        try
        {
            var locations = Addressables.LoadResourceLocationsAsync(path).WaitForCompletion();
            if (locations.Any())
            {
                var obj = Addressables.LoadAssetAsync<T>(path).WaitForCompletion();
                return obj;
            }
            else
                return default(T);
        }
        catch (Exception ex)
        {
            Debug.Log($"불러올 에셋이 Null입니다 : {ex.Message}");
        }

        return default(T);
    }
}
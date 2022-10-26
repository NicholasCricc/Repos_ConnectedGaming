using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

using UnityEngine;
using UnityEngine.UI;

using Firebase.Storage;
using Firebase.Extensions;
using JetBrains.Annotations;

public class FirebaseStorageController : MonoBehaviour
{
    
    private FirebaseStorage _firebaseInstance;
    [SerializeField] private GameObject ThumbnailPrefab;
    private GameObject _thumbnailContainer;
    public List<GameObject> instantiatedPrefabs;
    public List<AssetData> DownloadedAssetData;
    public enum DownloadType
    {
        Manifest, Thumbnail
    }
    
    public static FirebaseStorageController Instance
    {
        get;
        private set;
    }
    
    
    private void Awake()
    {
        //Singleton Pattern
        if (Instance != this && Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(this); //GameManager
        _firebaseInstance = FirebaseStorage.DefaultInstance;
    }

    private void Start()
    {
        instantiatedPrefabs = new List<GameObject>();
        _thumbnailContainer = GameObject.Find("Thumbnail_Container");
        //First download manifest.txt
        DownloadFileAsync("gs://cg-02-6e2c8.appspot.com/manifest.xml",DownloadType.Manifest);
        //Get the urls inside the manifest file
        //Download each url and display to the user
    }

    public void DownloadFileAsync(string url, DownloadType filetype ){
        StorageReference storageRef =  _firebaseInstance.GetReferenceFromUrl(url);
        
        // Download in memory with a maximum allowed size of 1MB (1 * 1024 * 1024 bytes)
        const long maxAllowedSize = 1 * 1024 * 1024;
        storageRef.GetBytesAsync(maxAllowedSize).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) {
                Debug.LogException(task.Exception);
                // Uh-oh, an error occurred!
            }
            else {
                Debug.Log($"{storageRef.Name} finished downloading!");
                if (filetype == DownloadType.Manifest)
                {
                    //Load manifest
                    StartCoroutine(LoadManifest(task.Result));
                }else if (filetype == DownloadType.Thumbnail)
                {
                    //Load the image into Unity
                    StartCoroutine(LoadImage(task.Result));
                }
            }
        });
        
    }

    IEnumerator LoadManifest(byte[] byteArr)
    {
        
        XDocument manifest = XDocument.Parse(System.Text.Encoding.UTF8.GetString(byteArr));
        DownloadedAssetData = new List<AssetData>();
        foreach (XElement xElement in manifest.Root.Elements())
        {
            string id = xElement.Element("id")?.Value;
            string name = xElement.Element("name")?.Value;
            string thumbnailUrl = xElement.Element("img")?.Element("url")?.Value;
            string priceStr = xElement.Element("price")?.Element("value")?.Value;
            float price = (priceStr != null) ? float.Parse(priceStr) : 0.0f;
            string currencyStr = xElement.Element("price")?.Element("currency")?.Value;
            AssetData.CURRENCY currency = (currencyStr != null)
                ? ((currencyStr == "diamonds") ? AssetData.CURRENCY.Diamonds : AssetData.CURRENCY.Gold)
                : AssetData.CURRENCY.Default;
            string discountStr = xElement.Element("sale")?.Element("discount")?.Value;
            float discount = (discountStr != null) ? float.Parse(discountStr) : 0.0f;
            AssetData newAsset = new AssetData(id, name, thumbnailUrl, price, currency, discount);
            Debug.Log(newAsset.ToString());
            DownloadedAssetData.Add(newAsset);
            DownloadFileAsync(newAsset.ThumbnailUrl, DownloadType.Thumbnail);
        }
        yield return null;
    }

    IEnumerator LoadImage(byte[] byteArr)
    {
        Texture2D imageTex = new Texture2D(1, 1);
        imageTex.LoadImage(byteArr);
        //Instantiate a new prefab
        GameObject thumbnailPrefab =
            Instantiate(ThumbnailPrefab, _thumbnailContainer.transform.position, 
                Quaternion.identity,_thumbnailContainer.transform);
        thumbnailPrefab.name = "Thumnail_" + instantiatedPrefabs.Count;
        //Load the image to that prefab
        thumbnailPrefab.transform.GetChild(0).GetComponent<RawImage>().texture = imageTex;
            
        instantiatedPrefabs.Add(thumbnailPrefab);
        yield return null;
    }
    
}
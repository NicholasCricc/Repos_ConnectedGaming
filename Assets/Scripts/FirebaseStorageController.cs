using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Firebase.Storage;
using Firebase.Extensions;

public class FirebaseStorageController : MonoBehaviour
{
    
    private FirebaseStorage _firebaseInstance;
    [SerializeField] private GameObject ThumbnailPrefab;
    private GameObject _thumbnailContainer;
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
        _thumbnailContainer = GameObject.Find("Thumbnail_Container");
        //First download manifest.txt
        DownloadFileAsync("gs://cg-02-6e2c8.appspot.com/manifest.txt",DownloadType.Manifest);
        //Get the urls inside the manifest file
        //Download each url and display to the user
    }

    public void DownloadFileAsync(string url, DownloadType filetype){
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
        //Converting from byte array to string
        string manifest = System.Text.Encoding.UTF8.GetString(byteArr);
        //Parsing the string to separate urls
        string[] urls = manifest.Split('\n');
        foreach (string url in urls)
        {
            DownloadFileAsync(url, DownloadType.Thumbnail);
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
        
        //Load the image to that prefab
        thumbnailPrefab.GetComponent<RawImage>().texture = imageTex;
        yield return null;
    }
    
}
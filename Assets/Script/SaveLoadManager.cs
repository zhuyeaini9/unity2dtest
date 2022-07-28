using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using Newtonsoft.Json;

public class SaveLoadManager : MonoBehaviour
{
    public string mSaveFileName;
    public static SaveLoadManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    public void newGame()
    {
    }

    public void saveGame()
    {
        try
        {
            GameData gameData = new GameData();

            IEnumerable<ISaveLoad> ds = FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoad>();
            foreach (var v in ds)
            {
                v.saveData(gameData);
            }

            string fullPath = Path.Combine(Application.persistentDataPath, mSaveFileName);
            Debug.Log(fullPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            string dataStr = JsonConvert.SerializeObject(gameData);

            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            {
                using(StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(dataStr);
                }
            }

        }
        catch(Exception)
        {

        }
        
    }

    public void loadGame()
    {
        try
        {
           
            string fullPath = Path.Combine(Application.persistentDataPath, mSaveFileName);
            if (File.Exists(fullPath))
            {
                string dataToLoad = "";
                using (FileStream stream = new FileStream(fullPath,FileMode.Open))
                {
                    using(StreamReader reader = new StreamReader(stream))
                    {
                        dataToLoad = reader.ReadToEnd();
                    }
                }

                GameData gd = JsonConvert.DeserializeObject<GameData>(dataToLoad);

                IEnumerable<ISaveLoad> ds = FindObjectsOfType<MonoBehaviour>().OfType<ISaveLoad>();
                foreach (var v in ds)
                {
                    v.loadData(gd);
                }

            }
        }
        catch(Exception)
        {

        }
    }

    private void Start()
    {
    }

    private void OnApplicationQuit()
    {
    }

    public void doSave()
    {
        saveGame();
    }
}

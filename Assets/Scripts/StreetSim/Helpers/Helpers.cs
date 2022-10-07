using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Helpers {
    [System.Serializable]
    public class HelperMethods {
        public static bool HasComponent<T> (GameObject obj, out T toReturn) {
            toReturn = obj.GetComponent<T>();
            return toReturn != null;
        }
        public static bool HasComponent<T> (GameObject obj) {
            return obj.GetComponent<T>() != null;
        }
        public static bool HasComponent<T> (Transform t, out T toReturn) {
            return HasComponent<T>(t.gameObject, out toReturn);
        }
        public static bool HasComponent<T> (Transform t) {
            return HasComponent<T>(t.gameObject);
        }

        public static float Map(float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }

    [System.Serializable]
    public class SaveSystemMethods {
        public static string GetSaveLoadDirectory(string path = "") {
             return (path != null && path.Length > 0) ? (path.EndsWith("/")) ? Application.dataPath + "/" + path : Application.dataPath + "/" + path + "/" : Application.dataPath + "/";
        }
        public static bool CheckDirectoryExists(string dirPath) {
            return Directory.Exists(dirPath);
        }
        public static bool CheckOrCreateDirectory(string dirPath) {
            if (!CheckDirectoryExists(dirPath)) Directory.CreateDirectory(dirPath);
            return true;
        }
        public static bool CheckFileExists(string filePath) {
            return File.Exists(filePath);
        }
        
        public static string ConvertToJSON<T>(T data) {
            return JsonUtility.ToJson(data, true);
        }
        public static T ConvertFromJSON<T>(string data) {
            return JsonUtility.FromJson<T>(data);
        }
        public static bool SaveJSON(string filePath, string json) {
            if (filePath.EndsWith(".json")) File.WriteAllText(filePath, json);
            else File.WriteAllText(filePath + ".json", json);
            return true;
        }
        public static bool LoadJSON<T>(string filePath, out T output) {
            string actualFilePath = (filePath.EndsWith(".json")) ? filePath : filePath+".json";
            if (CheckFileExists(actualFilePath)) {
                string fileContents = File.ReadAllText(actualFilePath);
                output = ConvertFromJSON<T>(fileContents);
                return true;
            } else {
                output = default(T);
                return false;
            }
        }
    }
}
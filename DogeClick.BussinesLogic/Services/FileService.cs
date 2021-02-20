using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DogeClick.BussinesLogic.Services
{
    public static class FileService
    {
        public static T ReadJson<T>(string path)
        {
            string str = ReadFromFile(path);
            if (str == null)
            {
                return default(T);
            }
            T obj = JsonConvert.DeserializeObject<T>(str);
            return obj;
        }

        public static void WriteJson<T>(string path, T obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            WriteToFile(path, json);
        }

        public static string ReadFromFile(string path)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            return null;
        }

        public static void AppendToFile(string path, string content)
        {
            File.AppendAllLines(path, new string[] { content });
        }

        public static void WriteToFile(string path, string content)
        {
            File.WriteAllText(path, content);
        }
    }
}

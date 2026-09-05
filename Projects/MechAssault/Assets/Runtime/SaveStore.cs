using System;
using System.IO;
using UnityEngine;
namespace Frontier {
    public sealed class SaveStore {
        readonly string path; readonly int count;
        public SaveStore(string id,int missions,bool smoke=false) {
            count=missions; string dir=Path.Combine(Application.persistentDataPath,smoke?"SmokeTests":"Campaign");
            Directory.CreateDirectory(dir); path=Path.Combine(dir,id+".json");
        }
        public SaveData Load() {
            foreach(var p in new[]{path,path+".bak"})try {
                if(!File.Exists(p))continue; var data=JsonUtility.FromJson<SaveData>(File.ReadAllText(p));
                if(data!=null&&data.Valid(count))return data;
            } catch(Exception e) { Debug.LogWarning("Save recovery: "+e.GetType().Name); }
            return new SaveData();
        }
        public bool Write(SaveData data) {
            try {
                if(!data.Valid(count))throw new InvalidDataException("Invalid campaign data");
                File.WriteAllText(path+".tmp",JsonUtility.ToJson(data,true));
                if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak"); else File.Move(path+".tmp",path);
                return true;
            } catch(Exception e) { Debug.LogWarning("Save failed: "+e.Message); return false; }
        }
    }
}

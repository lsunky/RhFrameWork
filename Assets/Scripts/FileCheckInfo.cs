using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RhFrameWork
{
    public class FileCheckInfo
    {
        public string name;
        public string md5;
        public float size;
        public FileCheckInfo(string str)
        {
            str = str.TrimEnd();
            string[] arr = str.Split('|');
            name    = arr[0];
            size    = int.Parse(arr[1]);
            md5     = arr[2];
        }

        public bool Equals(FileCheckInfo file)
        {
            return file.md5.Equals(md5);
        }
    }

    public class FileCheckResult
    {
        private List<FileCheckInfo> addList;
        private List<FileCheckInfo> deleteList;
        private List<FileCheckInfo> changeList;
        private List<FileCheckInfo> downList;
        public float downSize;

        public List<FileCheckInfo> AddList
        {
            get
            {
                return addList;
            }
        }

        public List<FileCheckInfo> DeleteList
        {
            get
            {
                return deleteList;
            }
        }

        public List<FileCheckInfo> ChangeList
        {
            get
            {
                return changeList;
            }
        }

        public List<FileCheckInfo> DownList
        {
            get
            {
                return downList;
            }
        }
        public FileCheckResult()
        {
            addList     = new List<FileCheckInfo>();
            deleteList  = new List<FileCheckInfo>();
            changeList  = new List<FileCheckInfo>();
            downList    = new List<FileCheckInfo>();
        }

        public void ChangFile(FileCheckInfo file)
        {
            downSize += file.size;
            changeList.Add(file);
            downList.Add(file);
        }

        public void AddFile(FileCheckInfo file)
        {
            downSize += file.size;
            addList.Add(file);
            downList.Add(file);
        }

        public void DeleteFile(FileCheckInfo file)
        {
            deleteList.Add(file);
        }

        int Compare(FileCheckInfo left, FileCheckInfo right)
        {
            return left.name.CompareTo(right.name);
        }

        public void Sort()
        {
            addList.Sort(Compare);
            changeList.Sort(Compare);
            deleteList.Sort(Compare);
        }
    }
}

using System.Collections.Generic;
using System.IO;
using Sonic853.ScriptableData;

namespace Sonic853.PackageManagerChecker
{
    public class PackageList : ScriptableLoader<PackageList>
    {
        public List<string> installPackages = new();
        public List<string> needPackages = new();
        public List<string> wrongPackages = new();
        /// <summary>
        /// 完成安装后删除自身（codespace.txt 存在时无效）
        /// </summary>
        public bool deleteSelf = false;
        public List<string> deleteFolders = new();
        public override void Load()
        {
            savePath = Path.Combine("Assets", "Sonic853", "Editor", "PackageManagerChecker", "Data");
            base.Load();
        }
    }
}

using System.Collections.Generic;
using System.IO;
using Sonic853.ScriptableData;

namespace Sonic853.PackageManagerChecker
{
    public class PackageList : ScriptableLoader<PackageList>
    {
        public string[] installPackages = new string[0];
        public string[] needPackages = new string[0];
        public string[] wrongPackages = new string[0];
        /// <summary>
        /// 完成安装后删除自身（codespace.txt 存在时无效）
        /// </summary>
        public bool deleteSelf = false;
        public string[] deleteFolders = new string[0];
        public override void Load()
        {
            savePath = Path.Combine("Assets", "Sonic853", "Editor", "PackageManagerChecker", "Data");
            base.Load();
        }
    }
}

using System.IO;

namespace Sonic853.PackageManagerChecker
{
    public class PackageList : ScriptableLoader<PackageList>
    {
        /// <summary>
        /// 要安装的包
        /// </summary>
        public string[] installPackages = new string[0];
        /// <summary>
        /// 检查是否已安装的包
        /// </summary>
        public string[] needPackages = new string[0];
        /// <summary>
        /// 检查是否安装了错误的包
        /// </summary>
        public string[] wrongPackages = new string[0];
        /// <summary>
        /// 完成安装后删除自身（codespace.txt 存在时无效）
        /// </summary>
        public bool deleteSelf = false;
        /// <summary>
        /// 要删除的文件夹
        /// </summary>
        public string[] deleteFolders = new string[0];
        public override void Load()
        {
            savePath = Path.Combine("Assets", "Sonic853", "Editor", "PackageManagerChecker", "Data");
            base.Load();
        }
    }
}

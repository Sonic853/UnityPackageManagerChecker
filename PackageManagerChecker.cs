using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Sonic853.Translate.Editor;

namespace Sonic853.PackageManagerChecker
{
    public class PackageManagerCheckerMain : EditorWindow
    {
        static TranslateReader translateReader = null;
        static readonly string path = Path.Combine("Assets", "Sonic853", "Editor", "PackageManagerChecker");
        static readonly string unityPackagePath = Path.Combine(path, "install.unitypackage");
        static readonly string cachePath = Path.Combine("Temp", "com.sonic853.packagemanager");
        static readonly string cacheUnityPackagePath = Path.Combine(cachePath, "install.unitypackage");
        static void Init()
        {
            // 读取编辑器语言
            var language = EditorPrefs.GetString("Editor.kEditorLocale", "ChineseSimplified");
            Debug.Log($"Editor Language: {language}");
            var file = Path.Combine(path, "Language", $"{language}.po.txt");
            if (!File.Exists(file))
            {
                Debug.Log("Language not found, use English");
                file = Path.Combine(path, "Language", "English.po.txt");
            }
            translateReader ??= new TranslateReader(file);
        }
        [InitializeOnLoadMethod]
        static void AutoRun()
        {
            Init();
            if (File.Exists(Path.Combine(path, "codespace.txt")))
            {
                Debug.Log(_("Codespace detected, skip check"));
                return;
            }
            CheckPackages(true);
        }
        [MenuItem("853Lab/CheckPackages", false, 100)]
        static void CheckPackages()
        {
            CheckPackages(false);
        }
        static async void CheckPackages(bool auto)
        {
            var packageListFile = Path.Combine(path, "Data", "PackageList.asset");
            if (!File.Exists(packageListFile))
            {
                Debug.LogError(_("PackageList.asset not found"));
                return;
            }
            var packageList = PackageList.Instance;
            if (packageList.installPackages.Length == 0)
            {
                Debug.LogWarning(_("No package to install"));
                return;
            }
            // 获取包
            var pack = Client.List(true);
            while (!pack.IsCompleted)
                await Task.Yield();
            // 检查现有包
            var haveWrongPackage = false;
            var needPackageNotFound = false;
            foreach (var package in packageList.wrongPackages)
            {
                if (pack.Result.FirstOrDefault(x => x.name == package) != null)
                {
                    haveWrongPackage = true;
                    Debug.LogError(string.Format(_("Package {0} is installed, please remove it"), package));
                }
            }
            foreach (var package in packageList.needPackages)
            {
                if (pack.Result.FirstOrDefault(x => x.name == package) == null)
                {
                    needPackageNotFound = true;
                    Debug.LogError(string.Format(_("Package {0} is not installed, please install it"), package));
                }
            }
            if (haveWrongPackage
            || needPackageNotFound)
            {
                Debug.LogError(_("Please fix the above errors before continuing"));
                return;
            }
            // 安装包
            var haveNoInstalledPackage = false;
            foreach (var package in packageList.installPackages)
            {
                if (pack.Result.FirstOrDefault(x => x.name == package) == null)
                {
                    Debug.Log(string.Format(_("Installing {0} ..."), package));
                    // 询问是否安装
                    if (!auto && !EditorUtility.DisplayDialog(_("Install"), string.Format(_("Install {0} ?"), package), _("Yes"), _("No")))
                    {
                        Debug.Log(string.Format(_("Skip {0}"), package));
                        continue;
                    }
                    Client.Add(package);
                    Debug.Log(string.Format(_("Installed {0}"), package));
                    haveNoInstalledPackage = true;
                }
            }
            if (haveNoInstalledPackage)
            {
                Debug.Log(_("Finish! Please switch the Unity Editor window or restart Unity Editor to complete the installation."));
                if (!auto)
                    EditorUtility.DisplayDialog(_("Finish!"), _("Please switch the Unity Editor window or restart Unity Editor to complete the installation."), _("OK"));
                return;
            }
            // 安装 unitypackage
            if (File.Exists(unityPackagePath))
            {
                Debug.Log(_("Installing unitypackage ..."));
                if (!Directory.Exists(cachePath))
                {
                    Directory.CreateDirectory(cachePath);
                }
                if (File.Exists(cacheUnityPackagePath))
                {
                    File.Delete(cacheUnityPackagePath);
                }
                if (File.Exists(Path.Combine(path, "codespace.txt")))
                    File.Copy(unityPackagePath, cacheUnityPackagePath);
                else
                    File.Move(unityPackagePath, cacheUnityPackagePath);
                AssetDatabase.ImportPackage(cacheUnityPackagePath, false);
                Directory.Delete(cachePath, true);
                Debug.Log(_("unitypackage is installed, Please switch the Unity Editor window or restart Unity Editor to complete the installation."));
            }
            // 删除文件夹
            if (File.Exists(Path.Combine(path, "codespace.txt")))
                return;
            if (packageList.deleteSelf)
            {
                foreach (var folder in packageList.deleteFolders)
                {
                    if (string.IsNullOrWhiteSpace(folder))
                        continue;
                    var _f = folder.Split('\\');
                    if (_f.Length <= 1)
                        continue;
                    var _folder = Path.Combine(_f);
                    if (Directory.Exists(_folder))
                        Directory.Delete(_folder, true);
                }
            }
        }
        static string _(string text)
        {
            return translateReader?._(text) ?? text;
        }
    }
}
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Sonic853.PackageManagerChecker
{
    public class PackageManagerCheckerMain : EditorWindow
    {
        static TranslateReader translateReader = null;
        static readonly string _name = "PackageManagerChecker";
        static readonly string path = Path.Combine("Assets", "Sonic853", "Editor", "PackageManagerChecker");
        static readonly string unityPackagePath = Path.Combine(path, "install.unitypackage");
        static readonly string cachePath = Path.Combine("Temp", "com.sonic853.packagemanager");
        static readonly string cacheUnityPackagePath = Path.Combine(cachePath, "install.unitypackage");
        static void Init()
        {
            // 读取编辑器语言
            var language = EditorPrefs.GetString("Editor.kEditorLocale", "English");
            Debug.Log($"[{_name}] Editor Language: {language}");
            var file = Path.Combine(path, "Language", $"{language}.po.txt");
            if (!File.Exists(file))
            {
                Debug.Log($"[{_name}] Language not found, use English");
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
                Debug.LogWarning($"[{_name}] {_("Codespace detected, skip check")}");
                return;
            }
            CheckPackages(true);
        }
        [MenuItem("Window/853Lab/Package/CheckPackages", false, 100)]
        static void CheckPackages()
        {
            CheckPackages(false);
        }
        static async void CheckPackages(bool auto)
        {
            var packageListFile = Path.Combine(path, "Data", "PackageList.asset");
            if (!File.Exists(packageListFile))
            {
                Debug.LogError($"[{_name}] {_("PackageList.asset not found")}");
                return;
            }
            var packageList = PackageList.Instance;
            if (packageList.installPackages.Length == 0)
            {
                Debug.LogWarning($"[{_name}] {_("No package to install")}");
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
                    Debug.LogError($"[{_name}] {string.Format(_("Package {0} is installed, please remove it"), package)}");
                }
            }
            foreach (var package in packageList.needPackages)
            {
                if (pack.Result.FirstOrDefault(x => x.name == package) == null)
                {
                    needPackageNotFound = true;
                    Debug.LogError($"[{_name}] {string.Format(_("Package {0} is not installed, please install it"), package)}");
                }
            }
            if (haveWrongPackage
            || needPackageNotFound)
            {
                Debug.LogError($"[{_name}] {_("Please fix the above errors before continuing")}");
                return;
            }
            // 安装包
            var haveNoInstalledPackage = false;
            foreach (var package in packageList.installPackages)
            {
                if (pack.Result.FirstOrDefault(x => x.name == package) == null)
                {
                    Debug.Log($"[{_name}] {string.Format(_("Installing {0} ..."), package)}");
                    // 询问是否安装
                    if (!auto && !EditorUtility.DisplayDialog(_("Install"), string.Format(_("Install {0} ?"), package), _("Yes"), _("No")))
                    {
                        Debug.Log($"[{_name}] {string.Format(_("Skip {0}"), package)}");
                        continue;
                    }
                    Client.Add(package);
                    Debug.Log($"[{_name}] {string.Format(_("Installed {0}"), package)}");
                    haveNoInstalledPackage = true;
                }
            }
            if (haveNoInstalledPackage)
            {
                Debug.Log($"[{_name}] {_("Finish! Please switch the Unity Editor window or restart Unity Editor to complete the installation.")}");
                if (!auto)
                    EditorUtility.DisplayDialog(_("Finish!"), _("Please switch the Unity Editor window or restart Unity Editor to complete the installation."), _("OK"));
                return;
            }
            // 安装 unitypackage
            if (File.Exists(unityPackagePath))
            {
                Debug.Log($"[{_name}] {_("Installing unitypackage ...")}");
                if (!Directory.Exists(cachePath))
                    Directory.CreateDirectory(cachePath);
                if (File.Exists(cacheUnityPackagePath))
                    File.Delete(cacheUnityPackagePath);
                if (File.Exists(Path.Combine(path, "codespace.txt")))
                    File.Copy(unityPackagePath, cacheUnityPackagePath);
                else
                    File.Move(unityPackagePath, cacheUnityPackagePath);
                AssetDatabase.ImportPackage(cacheUnityPackagePath, false);
                Directory.Delete(cachePath, true);
                Debug.Log($"[{_name}] {_("unitypackage is installed, Please switch the Unity Editor window or restart Unity Editor to complete the installation.")}");
            }
            // 删除文件夹
            if (File.Exists(Path.Combine(path, "codespace.txt")))
            {
                AssetDatabase.Refresh();
                return;
            }
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
            AssetDatabase.Refresh();
        }
        static string _(string text)
        {
            return translateReader?._(text) ?? text;
        }
    }
    public class ScriptableLoader<T> : ScriptableObject where T : ScriptableLoader<T>, new()
    {
        /// <summary>
        /// Default: Assets/Sonic853/Data
        /// </summary>
        protected static string savePath = Path.Combine("Assets", "Sonic853", "Data");
        /// <summary>
        /// Default: {typeof(T).Name}.asset
        /// </summary>
        protected static string fileName = null;
        protected static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CreateInstance<T>();
                    instance.Load();
                }
                return instance;
            }
        }
        public virtual void Load()
        {
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            if (string.IsNullOrEmpty(fileName))
                fileName = $"{typeof(T).Name}.asset";
            string filePath = Path.Combine(savePath, fileName);
            var _instance = AssetDatabase.LoadAssetAtPath<T>(filePath);
            if (_instance == null)
            {
                _instance = CreateInstance<T>();
                AssetDatabase.CreateAsset(_instance, filePath);
                AssetDatabase.SaveAssets();
            }
            instance = _instance;
        }
        public virtual void Save()
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = $"{typeof(T).Name}.asset";
            string filePath = Path.Combine(savePath, fileName);
            if (File.Exists(filePath))
                EditorUtility.SetDirty(instance);
            else
            {
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);
                AssetDatabase.CreateAsset(instance, filePath);
            }
            AssetDatabase.SaveAssets();
        }
    }
    public class TranslateReader
    {
        public TranslateReader(string path)
        {
            ReadFile(path);
        }
        private string[] lines = new string[0];
        private readonly List<string> msgid = new();
        private readonly List<string> msgstr = new();
        public string language = "en_US";
        public string lastTranslator = "anonymous";
        public string languageTeam = "anonymous";
        public string[] ReadFile(string path, bool parse = true)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"File {path} not found");
                return null;
            }
            var text = File.ReadAllText(path);
            lines = text.Split('\n');
            if (text.Contains("\r\n"))
                lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            if (parse)
                ParseFile(lines);
            return lines;
        }
        public void ParseFile(string path)
        {
            ReadFile(path, true);
        }
        public void ParseFile(string[] _lines)
        {
            msgid.Clear();
            msgstr.Clear();
            var msgidIndex = -1;
            var msgstrIndex = -1;
            var msgidStr = "msgid \"";
            var msgidLength = msgidStr.Length;
            var msgstrStr = "msgstr \"";
            var msgstrLength = msgstrStr.Length;
            var languageStr = "\"Language: ";
            var languageLength = languageStr.Length;
            var lastTranslatorStr = "\"Last-Translator: ";
            var lastTranslatorLength = lastTranslatorStr.Length;
            var languageTeamStr = "\"Language-Team: ";
            var languageTeamLength = languageTeamStr.Length;
            var doubleQuotationStr = "\"";
            var doubleQuotationLength = doubleQuotationStr.Length;
            foreach (var line in _lines)
            {
                var _line = line.Trim();
                if (_line.StartsWith(msgidStr))
                {
                    msgid.Add(ReturnText(_line[msgidLength.._line.LastIndexOf('"')]));
                    msgidIndex = msgid.Count - 1;
                    msgstrIndex = -1;
                    continue;
                }
                if (_line.StartsWith(msgstrStr))
                {
                    msgstr.Add(ReturnText(_line[msgstrLength.._line.LastIndexOf('"')]));
                    msgstrIndex = msgstr.Count - 1;
                    continue;
                }
                if (_line.StartsWith(languageStr) && msgstrIndex == 0)
                {
                    language = _line[languageLength.._line.LastIndexOf('"')];
                    // 找到并去除\n
                    if (language.Contains("\\n"))
                        language = language.Replace("\\n", "");
                    continue;
                }
                if (_line.StartsWith(lastTranslatorStr) && msgstrIndex == 0)
                {
                    lastTranslator = _line[lastTranslatorLength.._line.LastIndexOf('"')];
                    // 找到并去除\n
                    if (lastTranslator.Contains("\\n"))
                        lastTranslator = lastTranslator.Replace("\\n", "");
                    // 将<和>替换为＜和＞
                    lastTranslator = lastTranslator.Replace("<", "＜").Replace(">", "＞");
                    continue;
                }
                if (_line.StartsWith(languageTeamStr) && msgstrIndex == 0)
                {
                    languageTeam = _line[languageTeamLength.._line.LastIndexOf('"')];
                    // 找到并去除\n
                    if (languageTeam.Contains("\\n"))
                        languageTeam = languageTeam.Replace("\\n", "");
                    // 将<和>替换为＜和＞
                    languageTeam = languageTeam.Replace("<", "＜").Replace(">", "＞");
                    continue;
                }
                if (_line.StartsWith(doubleQuotationStr))
                {
                    if (msgidIndex != -1 && msgidIndex != 0)
                    {
                        msgid[msgidIndex] += ReturnText(_line[doubleQuotationLength.._line.LastIndexOf('"')]);
                        continue;
                    }
                    if (msgstrIndex != -1 && msgstrIndex != 0)
                    {
                        msgstr[msgstrIndex] += ReturnText(_line[doubleQuotationLength.._line.LastIndexOf('"')]);
                        continue;
                    }
                }
            }
            if (msgid.Count != msgstr.Count)
            {
                Debug.LogError("msgid.Count != msgstr.Count");
                return;
            }
        }
        string ReturnText(string text)
        {
            if (text.EndsWith("\\\\n"))
            {
                text = text[..^3];
                text += "\\n";
            }
            else if (text.EndsWith("\\n"))
            {
                text = text[..^2];
                text += "\n";
            }
            return text;
        }
        public string GetText(string text)
        {
            var index = msgid.IndexOf(text);
            if (index == -1)
                return text;
            return string.IsNullOrWhiteSpace(msgstr[index]) ? text : msgstr[index];
        }
        public string _(string text)
        {
            return GetText(text);
        }
    }
}
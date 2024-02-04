using AnotherFileBrowser.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrackVisu
{
    public class FileManagement : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] Controller controller;

        [Header("Source")]
        [SerializeField] string path_to_directory_for_csvs = "";
        [SerializeField] bool path_is_relative_to_project = true;

        [Header("Prefabs & UI")]
        [SerializeField] Transform buttons_parent;
        [SerializeField] Button loaded_files_button_prefab;


        private List<string> data_files;
        private string detected_edited_file = null;

        private const string playerprefs_path_key = "path_to_directory_for_csvs";
        private FileSystemWatcher file_watcher = null;

        private Dictionary<string, Button> buttons_for_files;

        // Start is called before the first frame update
        void Start()
        {
            data_files = new List<string>();
            buttons_for_files = new Dictionary<string, Button>();

            if (path_is_relative_to_project)
                path_to_directory_for_csvs = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), path_to_directory_for_csvs);

            if (PlayerPrefs.HasKey(playerprefs_path_key))
                path_to_directory_for_csvs = PlayerPrefs.GetString(playerprefs_path_key);

            while (!Directory.Exists(path_to_directory_for_csvs))
                RequestNewTargetDirectory();

            ReloadInputFiles();

            if (data_files.Count > 0)
                RealizeFromCsv(data_files[0]);

            CreateFileWatcher(path_to_directory_for_csvs);
        }

        public void RequestNewTargetDirectory()
        {
            var bp = new BrowserProperties();
            bp.filter = "csv files (*.csv)|*.csv|All Files (*.*)|*.*";
            bp.filterIndex = 0;
            bp.initialDir = path_to_directory_for_csvs;

            new FileBrowser().OpenFolderBrowser(bp, folder =>
            {
                path_to_directory_for_csvs = folder;
                CreateFileWatcher(path_to_directory_for_csvs);
                PlayerPrefs.SetString(playerprefs_path_key, path_to_directory_for_csvs);
                PlayerPrefs.Save();
                ReloadInputFiles();
                if (data_files.Count > 0)
                    RealizeFromCsv(data_files[0]);
            });
        }

        private void ReloadInputFiles()
        {
            string[] files = Directory.GetFiles(path_to_directory_for_csvs, "*.csv");
            data_files = new List<string>(files);
            buttons_for_files.Clear();

            foreach (Transform bt in buttons_parent)
                Destroy(bt.gameObject);

            Button create_new = Instantiate(loaded_files_button_prefab, buttons_parent);
            create_new.onClick.AddListener(() =>
            {
                string new_file = controller.CreateNewRandom(path_to_directory_for_csvs, data_files.Count);

                // Not needed because of the file watcher! 
                //ReloadInputFiles();
                //RealizeFromCsv(new_file);
            });
            create_new.GetComponentInChildren<TextMeshProUGUI>().text = "DEMO: CREATE NEW RANDOM";

            // Create a button for each file found
            foreach (string filepath in data_files)
            {
                string this_filepath = filepath;
                Button button = Instantiate(loaded_files_button_prefab, buttons_parent);
                button.onClick.AddListener(() => RealizeFromCsv(this_filepath));
                button.GetComponentInChildren<TextMeshProUGUI>().text = filepath.Split("\\").Last();
                buttons_for_files[this_filepath] = button;
            }
        }


        private void Update()
        {
            if (!string.IsNullOrEmpty(detected_edited_file))
            {
                ReloadInputFiles();
                if (File.Exists(detected_edited_file))
                    RealizeFromCsv(detected_edited_file);
                detected_edited_file = null;
            }
        }

        private void RealizeFromCsv(string filepath)
        {
            controller.RealizeFromCsv(filepath, buttons_for_files[filepath]);
        }

        public void CreateFileWatcher(string path)
        {
            if (file_watcher != null)
            {
                file_watcher.EnableRaisingEvents = false;
                file_watcher.Dispose();
            }

            file_watcher = new FileSystemWatcher();
            file_watcher.Path = path;
            file_watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            file_watcher.Filter = "*.csv";

            file_watcher.Changed += new FileSystemEventHandler(FileChanged);
            file_watcher.Created += new FileSystemEventHandler(FileChanged);
            file_watcher.Deleted += new FileSystemEventHandler(FileChanged);
            file_watcher.Renamed += new RenamedEventHandler(FileChanged);

            file_watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers.
        private void FileChanged(object source, FileSystemEventArgs e)
        {
            detected_edited_file = e.FullPath;
        }

        internal Button GetButtonForFile(string filepath)
        {
            return buttons_for_files[filepath];
        }
    }
}
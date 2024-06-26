﻿using System.Security.Cryptography;

namespace LLM.CheckSummer.Actions
{
    internal class HashAction : IAction
    {
        public string CustomFormat = ".txt";

        private string pathData = string.Empty;
        private string filename = string.Empty;
        private string separator = string.Empty;

        private bool isFileCreationNeeded = false;
        private bool isFullPathInsteadRelative = false;

        public string[] ResultLines { get; private set; }
        public string FilePath { get; private set; } = string.Empty;

        public const string INFO_DECORATOR = "♡♡♡♡♡♡♡♡♡♡♡♡♡♡♡♡♡♡♡♡";
        public const string DATA_MAINTAINER_RESULT_KEY = "COMPUTE_HASH_ACTION_RESULT";
        public const string DATA_MAINTAINER_FILEPATH_KEY = "COMPUTE_HASH_ACTION_FILEPATH";

        public string[] GetData() => new string[3] { pathData, filename, separator };

        /// <summary>
        /// data arguments, second one is path, third is filename and forth is separator, second one is mandatory
        /// </summary>
        /// <param name="data">arguments passed to the console</param>
        public IAction SetData(params string[] data)
        {
            pathData = data[1];
            separator = data.Length > 2 ? data[2] : ":";
            filename = data.Length > 3 ? data[3] : "checksum";
            isFileCreationNeeded = data.Length > 3 && !data[3].Contains("++"); //if its not the additional argument and it exists thats probably a filename right?

            if (data.Contains("++fullPath")) isFullPathInsteadRelative = true;

            Console.WriteLine($"[SET DATA] Filename: {filename} | Separator: {separator} \nPath: {pathData}\n\n");
            return this;
        }

        public void Start() => Checksum(pathData, filename, separator);

        public void Checksum(string path, string customFilename, string customSeparator)
        {
            if (path.Length <= 1 && pathData.Length <= 1) path = Directory.GetCurrentDirectory();
            if (!Directory.Exists(path))
            {
                path = Directory.GetCurrentDirectory();
                Console.WriteLine($"[ERROR] This directory does not exist: {path}\n\nCreating checksums from current directory: {path}\n");
            }

            Console.WriteLine("Computing MD5 hashes...");

            string hashListFile = Path.Combine(path, $"{customFilename}{CustomFormat}");
            FilePath = hashListFile;

            if (File.Exists(hashListFile)) File.Delete(hashListFile); //basically if we already have those just delete it to exclude from hash compare

            int counter = 0;
            string[] allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories); //get all files from main path and its subfolders

            if (isFileCreationNeeded) File.Create(hashListFile).Close(); //create file

            string[] lines = new string[allFiles.Length];

            //lookup for all files contained in directory including subfolders
            foreach (string file in allFiles)
            {
                using (var md5 = MD5.Create())
                {
                    FileInfo info = new FileInfo(file);
                    string filePath = info.FullName;
                    if (filePath == hashListFile) continue;
                    {
                        using (var stream = File.OpenRead(filePath))
                        {
                            byte[] fileMD5 = md5.ComputeHash(stream); //hash each file
                            string hash = BitConverter.ToString(fileMD5).Replace("-", "").ToLower(); //convert it to a web and more readable string
                            string currDir = isFullPathInsteadRelative ? filePath : Path.GetRelativePath(path, filePath); //getting the relative path so its not a long version but /{subfolder}/{item}, provide fullPath only if user sets +fullPath
                            lines[counter] = $"{currDir}{customSeparator}{hash}"; //set it to our line formatted as we wanted
                        }
                        counter++;
                    }
                }
            }

            ResultLines = lines;
            if (isFileCreationNeeded)
            {
                WriteToFile(hashListFile, lines);
                Console.WriteLine("Writing hashes to a file...");
            }
        }

        private void WriteToFile(string fileFullPath, string[] lines)
        {
            File.AppendAllLines(fileFullPath, lines);
            Console.WriteLine($"{INFO_DECORATOR}\nFile created at {fileFullPath}.\nTotal: {lines.Length} files\n{INFO_DECORATOR}");
        }
    }
}

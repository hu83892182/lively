﻿using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;

namespace livelywpf
{
    public class NLogger
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool _isInitialized = false;
        public static void SetupNLog()
        {
            DeletePreviousLogFiles(5);
            var config = new NLog.Config.LoggingConfiguration();

            //process start date as filename
            string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".txt";
            if (File.Exists(Path.Combine(Program.AppDataDir, "logs", fileName)))
            {
                fileName = Path.GetRandomFileName() + ".txt";
            }

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = Path.Combine(Program.AppDataDir, "logs", fileName), DeleteOldFileOnStartup = false };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            // Apply config           
            NLog.LogManager.Configuration = config;
            _isInitialized = true;
        }

        private static void DeletePreviousLogFiles(int maxLogs)
        {
            try
            {
                foreach (var fi in new DirectoryInfo(Path.Combine(Program.AppDataDir, "logs")).GetFiles().OrderByDescending(x => x.LastWriteTime).Skip(maxLogs))
                {
                    fi.Delete();
                }
            }
            catch { }
        }

        /// <summary>
        /// Returns data stored in class object file.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string PropertyList(object obj)
        {
            try
            {
                var props = obj.GetType().GetProperties();
                var sb = new StringBuilder();
                foreach (var p in props)
                {
                    sb.AppendLine(p.Name + ": " + p.GetValue(obj, null));
                }
                return sb.ToString();
            }
            catch
            {
                return "Failed to retrive properties of config file.";
            }
        }

        public static void LogHardwareInfo()
        {
            if(!_isInitialized)
            {
                SetupNLog();
            }

            Logger.Info(
                "\n" + "Lively v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() +
                " " + CultureInfo.CurrentCulture.Name +
                " 64Bit:" + Environment.Is64BitProcess + " MSIX:" + Program.IsMSIX +
                "\n" + SystemInfo.GetOSInfo() +
                SystemInfo.GetCPUInfo() +
                SystemInfo.GetGPUInfo());
        }

        public static void LogWin32Error(string msg = null)
        {
            if (!_isInitialized)
            {
                SetupNLog();
            }

            //todo: throw equivalent win32 exception.
            int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            if (err != 0)
            {
                Logger.Error(msg + " HRESULT:" + err);
            }
        }

        public static void ExtractLogFiles()
        {
            var savePath = string.Empty;
            var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "Select location to save the file",
                Filter = "Compressed archive|*.zip",
                FileName = "lively_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".zip",
            };
            if (saveFileDialog1.ShowDialog() == true)
            {
                savePath = saveFileDialog1.FileName;
            }

            if (!string.IsNullOrEmpty(savePath))
            {
                try
                {
                    var files = new List<string>();
                    var logFolder = Path.Combine(Program.AppDataDir, "logs");
                    if (Directory.Exists(logFolder))
                    {
                        files.AddRange(Directory.GetFiles(logFolder, "*.*", SearchOption.TopDirectoryOnly));
                    }

                    var settingsFile = Path.Combine(Program.AppDataDir, "Settings.json");
                    if (File.Exists(settingsFile))
                    {
                        files.Add(settingsFile);
                    }

                    var layoutFile = Path.Combine(Program.AppDataDir, "WallpaperLayout.json");
                    if (File.Exists(layoutFile))
                    {
                        files.Add(layoutFile);
                    }

                    /*
                    var procFile = Path.Combine(Program.AppDataDir, "temp", "process.txt");
                    var procList = new List<string>();
                    foreach (var item in Process.GetProcesses())
                    {
                        procList.Add(item.ProcessName);
                    }
                    File.WriteAllLines(procFile, procList);
                    files.Add(procFile);
                    */

                    if (files.Count != 0)
                    {
                        ZipCreate.CreateZip(savePath,
                            new List<ZipCreate.FileData>() {
                                new ZipCreate.FileData() { ParentDirectory = Program.AppDataDir, Files = files } });
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            }
        }
    }
}

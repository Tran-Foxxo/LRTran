﻿//
//  Program.cs
//
//  Author:
//       Noah Ablaseau <nablaseau@hotmail.com>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using OpenTK;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
namespace linerider
{
    public static class EntryPoint
    {
        #region Methods

        [STAThread]
        public static void Main()
        {
            Program.Run();
        }

        #endregion Methods
    }

    public static class Program
    {
        #region Fields
        public static string BinariesFolder = "bin";
        public readonly static CultureInfo Culture = new CultureInfo("en-US");
        public static string Version = "1.02";
        public static readonly string WindowTitle = "Line Rider: Advanced " + Version;
        public static Random Random;
        private static bool _crashed;
        private static GLWindow glGame;
        private static string _currdir;
        private static string _userdir;

        #endregion Fields

        #region Properties
        /// <summary>
        /// Gets the current directory. Ends in Path.DirectorySeperator
        /// </summary>
        public static string UserDirectory
        {
            get
            {
                if (_userdir == null)
                    _userdir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + Path.DirectorySeparatorChar + "LRA" + Path.DirectorySeparatorChar;
                return _userdir;
            }
        }
        public static string CurrentDirectory
        {
            get
            {
                if (_currdir == null)
                    _currdir = AppDomain.CurrentDomain.BaseDirectory;
                return _currdir;
            }
        }
        #endregion Properties

        #region Methods

        public static void Crash(Exception ex)
        {
            if (!_crashed)
            {
                _crashed = true;
                glGame.Track.BackupTrack();
            }
        }

        public static void NonFatalError(string err)
        {
            System.Windows.Forms.MessageBox.Show("Non Fatal Error: " + err);
        }

        public static void Run()
        {
            //     AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Settings.Load();
            Random = new Random();
            GameResources.Init();

            using (Toolkit.Init(new ToolkitOptions { EnableHighResolution = true, Backend = PlatformBackend.Default }))
            {
                using (glGame = new GLWindow())
                {
                    glGame.RenderSize = new System.Drawing.Size(1280, 720);
                    Drawing.GameRenderer.Game = glGame;
                    var ms = new MemoryStream(GameResources.icon);
                    glGame.Icon = new System.Drawing.Icon(ms);

                    ms.Dispose();
                    glGame.Title = WindowTitle;
                    glGame.Run(60, 0);
                }
                Audio.AudioService.CloseDevice();
            }
        }
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Crash((Exception)e.ExceptionObject);
            if (System.Windows.Forms.MessageBox.Show("Unhandled Exception: " + e.ExceptionObject + "\r\n\r\nWould you like to export the crash data to a log.txt?", "Error!", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                if (!File.Exists(UserDirectory + "log.txt"))
                    File.Create(UserDirectory + "log.txt").Dispose();

                string append = WindowTitle + "\r\n" + e.ExceptionObject.ToString() + "\r\n";
                string begin = File.ReadAllText(UserDirectory + "log.txt", System.Text.Encoding.ASCII);
                File.WriteAllText(UserDirectory + "log.txt", begin + append, System.Text.Encoding.ASCII);
            }

            throw (Exception)e.ExceptionObject;
        }
        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (Configuration.RunningOnWindows)
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (Configuration.RunningOnMacOS)
                {
                    Process.Start("open", url);
                }
                else if (Configuration.RunningOnLinux)
                {
                    Process.Start("xdg-open", url);
                }
                else
                {
                    throw;
                }
            }
        }
        public static void UpdateCheck()
        {
            if (Settings.CheckForUpdates)
            {
                new System.Threading.Thread(() =>
                {
                    try
                    {
                        using (WebClient wc = new WebClient())
                        {
                            string currentversion = "1.03";// wc.DownloadString("https://raw.githubusercontent.com/jealouscloud/linerider-advanced/master/src/version");
                            if (currentversion != WindowTitle)
                            {
                                var window = PopupWindow.Create(glGame.Canvas, glGame, "Would you like to download the latest version?", "Update Available! v" + currentversion, true, true);
                                window.FindChildByName("Okay", true).Clicked += (o, e) =>
                                {
                                    try
                                    {
                                        OpenUrl(@"https://github.com/jealouscloud/linerider-advanced/releases/latest");
                                    }
                                    catch
                                    {
                                        var w2 = PopupWindow.Create(glGame.Canvas, glGame, "Unable to open the browser.", "Error!", true, false);
                                        w2.FindChildByName("Okay", true).Clicked += (o2, e2) =>
                                         {
                                             w2.Close();
                                         };
                                    }
                                    window.Close();
                                };
                                window.FindChildByName("Cancel", true).Clicked += (o, e) => { window.Close(); };
                            }
                        }
                    }
                    catch
                    {
                    }
                })
                {
                    IsBackground = true
                }.Start();
            }
        }

        #endregion Methods
    }
}
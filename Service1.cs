using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Timers;
using System.Xml.Linq;
using ZipFile = Ionic.Zip.ZipFile;
using System.Threading;
using Tempo.ProcessExtensions;

namespace DiskoUpdater
{
    public partial class Service1 : ServiceBase
    {
        public static string programFiles { get; set; }
        public static bool isRunning { get; set; }
        public static int ticks { get; set; }
        System.Timers.Timer timer = new System.Timers.Timer();
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%") + @"\Tempo";

            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 15 * 1000;
            timer.Enabled = true;
        }
        protected override void OnStop()
        {
            isRunning = false;
        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            try
            {
                ProcessExtensions.StartProcessAsCurrentUser("cmd.exe", "echo Is someone out there?", null, false);
            }
            catch
            {
                return;
            }
            if (isRunning)
            {
                if (ticks < 10)
                {
                    ticks += 1;
                    return;
                }
            }
            isRunning = true;

            var strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var strWorkPath = Path.GetDirectoryName(strExeFilePath);
            var xml = new List<string> { };
            foreach (XElement level1Element in XElement.Load(@"http://diskoaio.com/disko_update.xml").Elements("Binaries"))
            {
                foreach (XElement level2Element in level1Element.Elements("Binary"))
                {
                    if (level2Element.Attribute("name").Value == "DiskoAIO.exe")
                        xml.Add(level2Element.Attribute("name").Value + ":" + level2Element.Attribute("version").Value);
                }
            }
            var versionInfo = FileVersionInfo.GetVersionInfo(strWorkPath + @"\DiskoAIO.exe");
            var version = versionInfo.FileVersion;
            version = version.Replace(".", string.Empty);
            var update_version = xml[0].Split(':')[1].Replace(".", string.Empty);
            if (int.Parse(version) < int.Parse(update_version))
            {
                string myWebUrlFile = "http://diskoaio.com/DiskoAIO.zip";
                string myLocalFilePath = strWorkPath + @"\update.zip";
                DirectoryInfo dInfo = new DirectoryInfo(strExeFilePath);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);

                ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;

                using (var client = new WebClient())
                {
                    File.Delete(strWorkPath + @"\update.zip");
                    client.DownloadFile(myWebUrlFile, myLocalFilePath);
                }

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = @"/IM DiskoAIO.exe /F"
                });
                process.WaitForExit();
                while (true)
                {
                    try
                    {
                        using (ZipFile zip1 = ZipFile.Read(myLocalFilePath))
                        {
                            Directory.CreateDirectory("C:\\DiskoUpdate");

                            foreach (ZipEntry entry in zip1)
                            {
                                if (File.Exists(strWorkPath + "\\" + entry.FileName))
                                {
                                    File.Delete(strWorkPath + "\\" + entry.FileName);
                                }
                                entry.Extract("C:\\DiskoUpdate");
                                foreach (var el in Directory.GetFiles("C:\\DiskoUpdate"))
                                {
                                    File.Move(el, strWorkPath + "\\" + entry.FileName);
                                }
                            }
                            Directory.Delete("C:\\DiskoUpdate");
                        }
                        File.Delete(strWorkPath + @"\update.zip");
                        break;
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                }
                while (true)
                {
                    Process[] processes = Process.GetProcessesByName("diskoaio");
                    if (processes.Length == 0)
                    {
                        ProcessExtensions.StartProcessAsCurrentUser(strWorkPath + @"\DiskoAIO.exe", "", strWorkPath, true);
                        Thread.Sleep(5000);
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            isRunning = false;
        }
    }
}

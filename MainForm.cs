using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;

namespace HostsEditor
{
    public partial class MainForm : Form
    {
        private HostsFileManager hostsFile;

        public MainForm()
        {
            InitializeComponent();
            dataGridView.AutoGenerateColumns = true;

            hostsFile = new HostsFileManager();
            LoadHosts();
        }

        private void LoadHosts()
        {
            try
            {
                bindingSource.DataSource = hostsFile.Load().ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            try
            {
                hostsFile.Save(bindingSource.DataSource as List<HostsFileEntry>);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            LoadHosts();
        }
    }

    public class HostsFileEntry
    {
        public bool IsEnabled { get; set; }
        public string IP { get; set; }
        public string Host { get; set; }

        public override string ToString()
        {
            return string.Format("{0}{1} {2}", IsEnabled ? "" : "#", IP, Host);
        }
    }

    public class HostsFileManager
    {
        private string path;

        public HostsFileManager(string path = @"c:\windows\system32\drivers\etc\hosts")
        {
            this.path = path;
        }

        public void GrantAccess()
        {
            WindowsIdentity currentUser = WindowsIdentity.GetCurrent();

            FileInfo fileInfo = new FileInfo(path);
            FileSecurity security = fileInfo.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(currentUser.Name, FileSystemRights.FullControl, AccessControlType.Allow));
            fileInfo.SetAccessControl(security);
            fileInfo.IsReadOnly = false;
        }

        public void Save(IEnumerable<HostsFileEntry> entries)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path)) { }
            }
            catch (UnauthorizedAccessException)
            {
                GrantAccess();
            }

            using (StreamWriter writer = new StreamWriter(path))
            {
                foreach (HostsFileEntry entry in entries)
                    writer.WriteLine(entry.ToString());
            }
        }

        public IEnumerable<HostsFileEntry> Load()
        {
            using (StreamReader reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    IEnumerable<HostsFileEntry> entries = ParseLine(line);
                    if (entries != null)
                        foreach (HostsFileEntry entry in entries)
                            yield return entry;
                }
            }
        }

        private IEnumerable<HostsFileEntry> ParseLine(string line, bool enabled = true)
        {
            if (string.IsNullOrWhiteSpace(line))
                yield break;

            line = line.Trim();

            if (line.Length >= 2 && line[0] == '#')
            {
                foreach (HostsFileEntry entry in ParseLine(line.Substring(1), enabled: false))
                    yield return entry;
            }
            else
            {
                int hashPosition = line.IndexOf('#');
                if (hashPosition != -1)
                    line = line.Substring(0, hashPosition);

                string[] values = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries); // split by space or tab
                if (values.Length < 2)
                    yield break;

                IPAddress ip;
                if (IPAddress.TryParse(values[0], out ip))
                {
                    for (int i = 1; i < values.Length; i++)
                        yield return new HostsFileEntry { IsEnabled = enabled, IP = ip.ToString(), Host = values[i] };
                }
            }
        }
    }
}

using System.Drawing.Printing;
using System.IO;
using ScintillaNET;
using System.Drawing;
using System.Linq;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using CliWrap;
using CliWrap.Buffered;
using System.Text;
using System.Threading.Tasks;
using CliWrap.EventStream;
using System.Collections;
using System.IO.Ports;
using System.Management;
using System;

using System.Threading;
using System.Text.RegularExpressions;




namespace WinFormsApp_ide
{
    public partial class Form1 : Form
    {
        private string path;
        private string output;
        private Rectangle originalFormSize;
        private Rectangle groupBox1OriginalSize;
        private Rectangle SpliContainer1OriginalSize;
        private Rectangle groupBox2OriginalSize;
        private Rectangle groupBox3OriginalSize;
        
        private Rectangle panel1OriginalSize;
        private ManagementEventWatcher watcher;

        private int currentLine = 1;
        private ArrayList prevMessages = new ArrayList();

        private ScintillaNET.Scintilla TextArea;
        public Form1()
        {


            InitializeComponent();

            originalFormSize = new Rectangle(Location, Size);
            SpliContainer1OriginalSize = new Rectangle(splitContainer1.Location, splitContainer1.Size);
            groupBox1OriginalSize = new Rectangle(groupBox1.Location, groupBox1.Size);
            TextArea = new ScintillaNET.Scintilla();
            SetupPort();

            watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");

            watcher.EventArrived += new EventArrivedEventHandler(PortChanged);

            watcher.Query = query;
            watcher.Start();

        }
        private async void GetPortInformation(string portName)
        {
            //WMIC.exe path  win32_pnpentity  where "PNPClass='Ports'" get  Caption
            label1.Text = "";
            try
            {
                var wmic = Cli.Wrap(targetFilePath: "wmic")
                                .WithArguments(args => args
                                                .Add("path")
                                                .Add("win32_pnpentity")
                                                .Add("where")
                                                .Add("PNPClass='Ports'")
                                                .Add("get")
                                                .Add("Caption")
                                            )
                                .WithWorkingDirectory("C:\\Users\\aba212\\source\\repos\\WinFormsApp_ide\\WinFormsApp_ide\\bin\\Debug\\net6.0-windows");

                await foreach (var cmdEvent in wmic.ListenAsync())
                {
                    label1.Text += cmdEvent.ToString();
                    label1.Text += "\n";

                }
            }
            catch (Exception ex) { }



        }

        public class ComboBoxItem
        {
            public string Text { get; set; }
            public object Tag { get; set; }

            public ComboBoxItem(string text, object tag)
            {
                Text = text;
                Tag = tag;
            }

            public override string ToString()
            {
                // Return the string to be displayed in the ComboBox
                return Text;
            }
        }

        private bool TagExists(ComboBox comboBox, object tagToCheck)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag.Equals(tagToCheck))
                {
                    return true;
                }
            }
            return false;
        }

        private async void SetupPort()
        {


            string[] portNames = SerialPort.GetPortNames();

            label1.Text = "";
            comboBox1.Items.Clear();
            string inputString = "";
            var stlinkListRes = Cli.Wrap(targetFilePath: "ST-LINK_CLI")
                        .WithArguments("-List")
                        .WithValidation(CommandResultValidation.None);

            await foreach (var cmdEvent in stlinkListRes.ListenAsync())
            {

                inputString += cmdEvent.ToString() + "\n";


            }


            string pattern = "ST-LINK Probe ";




            int count = Regex.Matches(inputString, Regex.Escape(pattern)).Count;
           
            if (count == 0)
            {
                comboBox1.Items.Clear();
                return;
            }




            for (int i = 0; i < count; i++)
            {
                try
                {
                    var stdOutBuffer = new StringBuilder();
                    var stlinkRes = await Cli.Wrap(targetFilePath: "ST-LINK_CLI")
                        .WithArguments(args => args
                                        .Add("-c")
                                        .Add("SWD")
                                        .Add($"ID={i}")
                                        .Add("-Rst")
                                    )
                        .WithWorkingDirectory("C:\\Users\\aba212\\source\\repos\\WinFormsApp_ide\\WinFormsApp_ide\\bin\\Debug\\net6.0-windows")
                        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                        .ExecuteAsync();

                    string patternSN = @"ST-LINK SN: (\S+)";
                    string patternDF = @"Device family: (\S+)";

                    Match matchSN = Regex.Match(stdOutBuffer.ToString(), patternSN);
                    Match matchDF = Regex.Match(stdOutBuffer.ToString(), patternDF);

                    try
                    {


                        if (matchSN.Success && matchDF.Success)
                        {
                            string serialNumber = matchSN.Groups[1].Value;
                            string deviceFamily = matchDF.Groups[1].Value;
                            if (!TagExists(comboBox1, serialNumber))
                            {
                                comboBox1.Items.Add(new ComboBoxItem(deviceFamily, serialNumber));
                            }

                        }
                    }
                    catch (Exception ex) { }

                }

                catch (Exception ex) { }


            }
        }

        private void PortChanged(object sender, EventArrivedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                SetupPort();
            });
        }


        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            watcher.Stop();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized || this.WindowState == FormWindowState.Maximized)
            {
                splitContainer1.Panel1.Controls.Add(treeView1);
                treeView1.Dock = DockStyle.Fill;
                splitContainer1.Panel2.Controls.Add(tabControl2);
                tabControl2.Dock = DockStyle.Fill;
                splitContainer2.Panel1.Controls.Add(splitContainer1);
                splitContainer1.Dock = DockStyle.Fill;
                
                splitContainer2.Panel2.Controls.Add(richTextBox1);

                richTextBox1.Dock = DockStyle.Fill;
                groupBox1.Controls.Add(splitContainer2);

                splitContainer2.Dock = DockStyle.Fill;
                return;
            }





        }
        private void resizeControl(Rectangle r, Control c)
        {
            float xRate = (float)(Width) / (float)(originalFormSize.Width);
            float yRate = (float)(Height) / (float)(originalFormSize.Height);

            int newX = (int)(r.Width * xRate);
            int newY = (int)(r.Height * yRate);

            int newWidth = (int)(r.Width * xRate);
            int newHeight = (int)(r.Height * yRate);

            //c.Location = new Point(newX + c.Location.X, newY + c.Location.Y);
            c.Size = new Size(newWidth, newHeight);

        }


        private void Form1_Resize(object sender, EventArgs e)
        {

            if (this.WindowState == FormWindowState.Minimized)
            {
                return;
            }


            resizeControl(groupBox1OriginalSize, groupBox1);


            // Ensure the SplitterDistance is within a valid range
            int minSplitterDistance = splitContainer1.Panel1MinSize;
            int maxSplitterDistance = splitContainer1.Width - splitContainer1.Panel2MinSize;
            int desiredSplitterDistance = 400;

            splitContainer1.SplitterDistance = Math.Max(minSplitterDistance, Math.Min(desiredSplitterDistance, maxSplitterDistance));


        }

        private void InitColors()
        {

            TextArea.SetSelectionBackColor(true, IntToColor(0x114D9C));

        }
        public static Color IntToColor(int rgb)
        {
            return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }



        private void InitSyntaxColoring()
        {

            // Configure the default style
            TextArea.StyleResetDefault();
            TextArea.Styles[Style.Default].Font = "Consolas";
            TextArea.Styles[Style.Default].Size = 10;
            TextArea.Styles[Style.Default].BackColor = IntToColor(0x212121);
            TextArea.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
            TextArea.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            TextArea.Styles[Style.Cpp.Identifier].ForeColor = IntToColor(0xD0DAE2);
            TextArea.Styles[Style.Cpp.Comment].ForeColor = IntToColor(0xBD758B);
            TextArea.Styles[Style.Cpp.CommentLine].ForeColor = IntToColor(0x40BF57);
            TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = IntToColor(0x2FAE35);
            TextArea.Styles[Style.Cpp.Number].ForeColor = IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.String].ForeColor = IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.Character].ForeColor = IntToColor(0xE95454);
            TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = IntToColor(0x8AAFEE);
            TextArea.Styles[Style.Cpp.Operator].ForeColor = IntToColor(0xE0E0E0);
            TextArea.Styles[Style.Cpp.Regex].ForeColor = IntToColor(0xff00ff);
            TextArea.Styles[Style.Cpp.CommentLineDoc].ForeColor = IntToColor(0x77A7DB);
            TextArea.Styles[Style.Cpp.Word].ForeColor = IntToColor(0x48A8EE);
            TextArea.Styles[Style.Cpp.Word2].ForeColor = IntToColor(0xF98906);
            TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = IntToColor(0xB3D991);
            TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = IntToColor(0xFF0000);
            TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = IntToColor(0x48A8EE);

            TextArea.Lexer = Lexer.Cpp;

            TextArea.SetKeywords(0, "class extends implements import interface new case do while else if for in switch throw get set function var try catch finally while with default break continue delete return each const namespace package include use is as instanceof typeof author copy default deprecated eventType example exampleText exception haxe inheritDoc internal link mtasc mxmlc param private return see serial serialData serialField since throws usage version langversion playerversion productversion dynamic private public partial static intrinsic internal native override protected AS3 final super this arguments null Infinity NaN undefined true false abstract as base bool break by byte case catch char checked class const continue decimal default delegate do double descending explicit event extern else enum false finally fixed float for foreach from goto group if implicit in int interface internal into is lock long new null namespace object operator out override orderby params private protected public readonly ref return switch struct sbyte sealed short sizeof stackalloc static string select this throw true try typeof uint ulong unchecked unsafe ushort using var virtual volatile void while where yield");
            TextArea.SetKeywords(1, "void Null ArgumentError arguments Array Boolean Class Date DefinitionError Error EvalError Function int Math Namespace Number Object RangeError ReferenceError RegExp SecurityError String SyntaxError TypeError uint XML XMLList Boolean Byte Char DateTime Decimal Double Int16 Int32 Int64 IntPtr SByte Single UInt16 UInt32 UInt64 UIntPtr Void Path File System Windows Forms ScintillaNET");

        }

        private void ListDirectory(TreeView treeView, string path)
        {
            treeView.Nodes.Clear();
            var rootDirectoryInfo = new DirectoryInfo(path);
            var rootDirectoryInfo1 = CreateDirectoryNode(rootDirectoryInfo);
            treeView.Nodes.Add(rootDirectoryInfo1);
        }

        private static TreeNode CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            var directoryNode = new TreeNode(directoryInfo.Name);
            foreach (var directory in directoryInfo.GetDirectories())
            {
                directoryNode.Nodes.Add(CreateDirectoryNode(directory));
            }

            foreach (var file in directoryInfo.GetFiles())
            {
                directoryNode.Nodes.Add(new TreeNode(file.Name));
            }
            return directoryNode;

        }



        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();


            if (fbd.ShowDialog() == DialogResult.OK)
            {
                //label1.Text = fbd.SelectedPath;
                path = fbd.SelectedPath;
                ListDirectory(treeView1, fbd.SelectedPath);
            }

        }

        private void openProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();


            if (fbd.ShowDialog() == DialogResult.OK)
            {
                path = fbd.SelectedPath;
                ListDirectory(treeView1, fbd.SelectedPath);
            }
        }
        private string RemoveFirstRootFromPath(string fullPath)
        {
            // Split the path by the path separator
            string[] parts = fullPath.Split(new string[] { treeView1.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1)
            {
                // Reconstruct the path, starting from the second element
                return String.Join(treeView1.PathSeparator, parts, 1, parts.Length - 1);
            }
            else
            {
                // If there's only one part, return an empty string or as needed
                return string.Empty;
            }
        }


        private class TabPageData
        {
            public string Path { get; set; }
            public string Text { get; set; }
            public string Name { get; set; }
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            //label1.Text = path + '\\' + RemoveFirstRootFromPath(e.Node.FullPath);
            System.IO.StreamReader sr = new System.IO.StreamReader(path + '\\' + RemoveFirstRootFromPath(e.Node.FullPath));
            TabPage tabPage = new TabPage((e.Node.Text));


            TextArea = new ScintillaNET.Scintilla();



            // INITIAL VIEW CONFIG
            TextArea.TextChanged += new EventHandler(TextArea_TextChanged);
            TextArea.Name = (e.Node.Text);
            TextArea.WrapMode = WrapMode.None;
            TextArea.IndentationGuides = IndentView.LookBoth;
            TextArea.Text = sr.ReadToEnd();
            tabPage.Tag = new TabPageData { Path = path + '\\' + RemoveFirstRootFromPath(e.Node.FullPath), Text = TextArea.Text, Name = e.Node.Text };
            // STYLING
            InitColors();
            InitSyntaxColoring();

            tabPage.Controls.Add(TextArea);
            TextArea.Dock = DockStyle.Fill;


            ////////////////////////////////////
            tabControl2.Controls.Add(tabPage);
            tabControl2.SelectedTab = tabPage;
            tabPage.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(tabControl2);
            tabControl2.Dock = DockStyle.Fill;

            sr.Close();
            ////////////////////////////////////

        }

        private void TextArea_TextChanged(object sender, EventArgs e)
        {
            ScintillaNET.Scintilla currentTextArea = sender as ScintillaNET.Scintilla;
            if (currentTextArea != null)
            {
                
                if (tabControl2.SelectedTab != null && tabControl2.SelectedTab.Tag is TabPageData data)
                {
                    
                    TabPage currentTab = currentTextArea.Parent as TabPage;
                    if (currentTab != null && !currentTab.Text.EndsWith("*"))
                    {
                        currentTab.Text = data.Name + "*";
                    }
                }
            }


        }


        private void Save_File()
        {


            if (tabControl2.SelectedTab.Tag is TabPageData data)
            {
                string textArea = tabControl2.SelectedTab.Controls[data.Name].Text;
                
                MessageBox.Show(data.Name + " Saved!");
                System.IO.StreamWriter sw = new System.IO.StreamWriter(data.Path);
                sw.WriteLine(textArea);
                sw.Close();
            }

        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {

                Save_File();
                
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save_File();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            string filePathbuild = path + '\\' + "build";


            if (Directory.Exists(filePathbuild))
            {
                richTextBox1.AppendText("The folder exist");
                richTextBox1.AppendText(Environment.NewLine);
            }
            else
            {


                richTextBox1.AppendText("The folder didn't exist");
                richTextBox1.AppendText(Environment.NewLine);
                DirectoryInfo di = Directory.CreateDirectory(filePathbuild);
                if (di.Exists)
                {
                    richTextBox1.AppendText("The directory was successfully created.");
                    richTextBox1.AppendText(Environment.NewLine);
                }


            }
            //label1.Text = path;


            try
            {
                var makeRes = Cli.Wrap(targetFilePath: "make")
                    .WithWorkingDirectory(path);


                // The output is likely to be multi-line, so split it into lines
                await foreach (var cmdEvent in makeRes.ListenAsync())
                {
                    richTextBox1.AppendText(cmdEvent.ToString());
                    richTextBox1.AppendText(Environment.NewLine);
                    richTextBox1.ScrollToCaret();

                }
            }
            catch (Exception ex)
            {
                //await foreach (var cmdEvent in ex.Message)

                richTextBox1.AppendText(ex.Message);
                richTextBox1.AppendText(Environment.NewLine);
                richTextBox1.ScrollToCaret();


            }

            richTextBox1.ReadOnly = true;




        }

        private void richTextBox1_RegionChanged(object sender, EventArgs e)
        {
            //richTextBox1.GetLineFromCharIndex.ToString();
            //label1.Text = richTextBox1.Lines.Length.ToString();
        }

        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            richTextBox1.ReadOnly = false;
            richTextBox1.ScrollToCaret();

            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = false;
                return;
            }






        }

        private void tabControl2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void splitContainer2_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer2_Panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            string directoryPath = path; // Replace with your directory path
            string searchPattern = "*.elf"; // Pattern to match .elf files
            string elfFile = @"";

            try
            {
                string[] elfFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);

                if (elfFiles.Length == 0)
                {
                    richTextBox1.AppendText("No .elf files found in the specified directory.");
                    richTextBox1.AppendText(Environment.NewLine);
                    richTextBox1.ScrollToCaret();
                }
                else
                {
                    foreach (string file in elfFiles)
                    {
                        richTextBox1.AppendText("Found .elf file: " + file);
                        elfFile = file;
                        richTextBox1.AppendText(Environment.NewLine);
                        richTextBox1.ScrollToCaret();
                    }
                }
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText("An error occurred: " + ex.Message);
                richTextBox1.AppendText(Environment.NewLine);
                richTextBox1.ScrollToCaret();

            }

            elfFile = elfFile.Replace('\\', '/');

            label1.Text += ("Found port: " + elfFile);
            label1.Text += "\n";



            elfFile = $"program {elfFile} verify";

            var openocdRes = Cli.Wrap(targetFilePath: "openocd")
                .WithArguments(new[] {
                "-f", "interface\\stlink-v2.cfg",
                "-f", "target\\stm32f4x.cfg",
                "-c", elfFile,
                "-c", "exit"})
                .WithWorkingDirectory("C:\\Users\\aba212\\source\\repos\\WinFormsApp_ide\\WinFormsApp_ide\\bin\\Debug\\net6.0-windows");

            // The output is likely to be multi-line, so split it into lines
            await foreach (var cmdEvent in openocdRes.ListenAsync())
            {
                richTextBox1.AppendText(cmdEvent.ToString());
                richTextBox1.AppendText(Environment.NewLine);

            }
            richTextBox1.ScrollToCaret();
            richTextBox1.ReadOnly = true;


        }
    }
}
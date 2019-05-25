using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Ionic.Zlib;
using System.Globalization;
using PackageIO;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PS3Lib;
using DiscordRPC;
using System.Threading;
using MW2Lib;

namespace PS_Connect
{
    public partial class Form1 : Form
    {

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public System.Net.WebClient request = new System.Net.WebClient();
        

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
        private string path;
        public static PS3API PS3 = new PS3API();
       
        public DiscordRpcClient client;

        public int prestige;
        public int rank;

        public Form1()
        {
            Thread t = new Thread(new ThreadStart(SplashStart));
            t.Start();
            Thread.Sleep(5000);
            PS3Lib.PS3API ps3 = new PS3Lib.PS3API();
            InitializeComponent();
            t.Abort();

            shaderp.Location = new Point(5, 431); // type your X and Y coordinates here
            shaderp.Visible = true; // Display the panel
            home.Location = new Point(177, 39);
            home.Visible = true;
        }

        public class Offsets

        {

            public static uint

                VectoAngles = 0x2590A8,

                SetClientViewAngles = 0x16CBE0,

                G_Client = 0x14E2200,

                G_ClientSize = 0x3700,

                G_Entity = 0x1319800,

                G_EntitySize = 0x280;

        }

        public class Buttons

        {

            public static string

                DpadUp = "+actionslot 1",

                DpadDown = "+actionslot 2",

                DpadRight = "+actionslot 4",

                DpadLeft = "+actionslot 3",

                Cross = "+gostand",

                Circle = "+stance",

                Triangle = "weapnext",

                Square = "+usereload",

                R3 = "+melee",

                R2 = "+frag",

                R1 = "+attack",

                L3 = "+breath_sprint",

                L2 = "+smoke",

                L1 = "+speed_throw",

                Select = "togglescores",

                Start = "togglemenu";

        }

        public static bool ButtonPressed(int client, string Button)

        {

            if (PS3.Extension.ReadString(0x34750E9F + ((uint)client * 0x97F80)) == Button)

                return true;

            else return false;

        }

        public static void SV_GameSendServerCommand(int Client, int Type, string Command)

        {

            RPC.Call(0x005720C0, new object[] { Client, Type, Command });

        }


        public static uint G_Client(int Client, uint Mod = 0x0)

        {

            return Offsets.G_Client + (Offsets.G_ClientSize * (uint)Client) + Mod;

        }



        public static uint G_Entity(int Client, uint Mod = 0x0)

        {

            return Offsets.G_Entity + (Offsets.G_EntitySize * (uint)Client) + Mod;

        }



        public static bool ReturnPlayerActivity(int Client)

        {

            return PS3.Extension.ReadString(G_Client(Client, 0x3290)) != "";

        }



        public static bool ReturnPlayerLifeStatus(int Client)

        {

            return PS3.Extension.ReadByte(G_Client(Client, 0x345C)) != 0x01;

        }



        public static float[] ReturnOrigin(int Client)

        {

            float[] Origin = new float[3];

            Origin[0] = PS3.Extension.ReadFloat(G_Client(Client, 0x1C));

            Origin[1] = PS3.Extension.ReadFloat(G_Client(Client, 0x20));

            Origin[2] = PS3.Extension.ReadFloat(G_Client(Client, 0x24));



            return Origin;

        }



        public static int ReturnNearestPlayer(int Client)

        {

            int NearestPlayer = -1;

            float Closest = 0xFFFFFFFF;

            float[] Distance3D = new float[3];

            float Difference = new float();

            for (int i = 0; i < 18; i++)

            {

                Distance3D[0] = ReturnOrigin(i)[0] - ReturnOrigin(Client)[0];

                Distance3D[1] = ReturnOrigin(i)[1] - ReturnOrigin(Client)[1];

                Distance3D[2] = ReturnOrigin(i)[2] - ReturnOrigin(Client)[2];



                Difference = (float)(Math.Sqrt((Distance3D[0] * Distance3D[0]) + (Distance3D[1] * Distance3D[1]) + (Distance3D[2] * Distance3D[2])));



                if ((i != Client))

                {

                    if (ReturnPlayerActivity(i) && ReturnPlayerLifeStatus(i))

                    {

                        if (Difference < Closest)

                        {

                            NearestPlayer = i;

                            Closest = Difference;

                        }

                    }

                }

            }

            return NearestPlayer;

        }



        public static void SetClientViewAngles(int Client, float[] Angles)

        {

            PS3.Extension.WriteFloat(0x10004000, Angles[0]);

            PS3.Extension.WriteFloat(0x10004004, Angles[1]);

            PS3.Extension.WriteFloat(0x10004008, Angles[2]);

            RPC.Call(Offsets.VectoAngles, 0x10004000, 0x1000400C);

            RPC.Call(Offsets.SetClientViewAngles, G_Entity(Client), 0x1000400C);

        }



        public static void DoAimbot(int Client)

        {

            if (ButtonPressed(Client, Buttons.L1) || ButtonPressed(Client, Buttons.L1 + Buttons.R1))

            {

                SetClientViewAngles(Client, ReturnOrigin(ReturnNearestPlayer(Client)));

            }

        }

    
    public void SplashStart()
        {
            Application.Run(new Form2());
        }
        

        public static void CompressBuffer(byte[] b, Stream compressor)
        {
            try
            {
                compressor.Write(b, 0, b.Length);
            }
            finally
            {
                if (compressor != null)
                {
                    ((IDisposable)compressor).Dispose();
                }
            }
        }
        public static byte[] CompressBuffer(byte[] b)
        {
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Form1.CompressBuffer(b, new ZlibStream(memoryStream, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestCompression));
                result = memoryStream.ToArray();
            }
            return result;
        }

        private void getZoneFile(string fastfile, string zonefile)
        {
            BinaryReader binaryReader = new BinaryReader(new FileStream(fastfile, FileMode.Open), Encoding.Default);
            string a = new string(binaryReader.ReadChars(8));
            int num = IPAddress.NetworkToHostOrder(binaryReader.ReadInt32());
            int num2 = (int)IPAddress.NetworkToHostOrder(binaryReader.ReadInt16());
            BinaryWriter binaryWriter = new BinaryWriter(new FileStream(zonefile, FileMode.Create), Encoding.Default);
            bool flag = !(a == "IWffu100") || num != 269 || num2 == 30938 || !(Path.GetFileName(fastfile) != "common_mp.ff");
            if (flag)
            {
                bool flag2 = (!(a == "IWffu100") || num != 387 || num2 == 30938) && (!(a == "IWffu100") || num != 1 || num2 == 30938);
                if (flag2)
                {
                    binaryReader.Close();
                    MessageBox.Show(fastfile + "\nis not a valid PS3 Fast File", "Error, Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    return;
                }
                binaryReader.BaseStream.Position = 12L;
            }
            else
            {
                binaryReader.BaseStream.Position = 37L;
            }
            int num3 = 0;
            try
            {
                int num4;
                for (int i = 1; i < 5000; i = num4 + 1)
                {
                    num4 = num3;
                    num3 = num4 + 1;
                    int count = int.Parse(BitConverter.ToString(binaryReader.ReadBytes(2)).Replace("-", ""), NumberStyles.AllowHexSpecifier);
                    binaryWriter.Write(Ionic.Zlib.DeflateStream.UncompressBuffer(binaryReader.ReadBytes(count)));
                    num4 = i;
                }
                binaryReader.Close();
                binaryWriter.Close();
            }
            catch (Exception ex)
            {
                bool flag3 = !(ex is FormatException);
                if (flag3)
                {
                    throw;
                }
                binaryReader.Close();
                binaryWriter.Close();
            }
        }


        private void PictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void Button2_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("You are exiting PlayStation Connect, would you like to disconnect from your console first? If you are not connected, just hit no.", "PS Connect", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                MessageBox.Show("Disconnecting");
                try
                {
                    PS3.CCAPI.DisconnectTarget();
                }
                catch
                {
                    MessageBox.Show("Your console wasn't connected!");
                }
                Application.Exit();
            }
            if (dialogResult == DialogResult.No)
            {
                Application.Exit();
            }
            if (dialogResult == DialogResult.Cancel)
            {
                MessageBox.Show("Not closing the program, tool is still connected.");
            }
        }


        private void Button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Warning! You are exiting the tool, your unsaved projects (if any) will be deleted. Working on an auto save function.");
            Form1.ActiveForm.WindowState = FormWindowState.Minimized;
        }

        private void PS3ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Button4_Click(object sender, EventArgs e)
        {
            hidepanels();
            shaderp.Location = new Point(5, 5); // type your X and Y coordinates here
            shaderp.Visible = true; // Display the panel
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            hidepanels();
            shaderp.Location = new Point(5, 53); // type your X and Y coordinates here
            shaderp.Visible = true; // Display the panel
            mw2rtm.Location = new Point(173, 40);
            mw2rtm.Visible = true;
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            hidepanels();
            shaderp.Location = new Point(5, 101); // type your X and Y coordinates here
            shaderp.Visible = true; // Display the panel
            home.Visible = false;
            zoneeditor.Location = new Point(187, 44);
            zoneeditor.Visible = true;
        }

        private void Button7_Click(object sender, EventArgs e)
        {
            hidepanels();
            shaderp.Location = new Point(5, 431); // type your X and Y coordinates here
            shaderp.Visible = true; // Display the panel
            home.Location = new Point(177, 39);
            home.Visible = true;
        }

        public void hidepanels()
        {
            home.Visible = false;
            zoneeditor.Visible = false;
        }
        private void ConnectToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void AttachToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        public void connect()
        {
            try
            {
                PS3.CCAPI.ConnectTarget();
                cellTemp.Text = "" + PS3.CCAPI.GetTemperatureCELL();
                rsxTemp.Text = "" + PS3.CCAPI.GetTemperatureRSX();
                fwtxt.Text = "" + PS3.CCAPI.GetFirmwareVersion();
                systxt.Text = "" + PS3.CCAPI.GetFirmwareType();
                PS3.CCAPI.Notify(PS3Lib.CCAPI.NotifyIcon.INFO, "PS Connect | Connected!"); 
                status.Text = "Connected!";
                try
                {
                    PS3.CCAPI.AttachProcess();
                    MessageBox.Show("You have attached!");
                    status.Text = "Attached!";
                }
                catch
                {
                    MessageBox.Show("Attachment wasn't possible, try again!");
                    status.Text = "Failed";
                }
            }
            catch
            {
                MessageBox.Show("Connection wasn't possible, try again!");
                status.Text = "Failed";
            }
        }
        private void Button13_Click(object sender, EventArgs e)
        {
            connect();
        }

        private void Button10_Click(object sender, EventArgs e)
        {
            try
            {
                PS3.CCAPI.ShutDown(CCAPI.RebootFlags.ShutDown);
            }
            catch
            {
                MessageBox.Show("Console isn't connected!");
            }
        }

        private void Button8_Click(object sender, EventArgs e)
        {
            PS3.CCAPI.Notify(CCAPI.NotifyIcon.FRIEND, "Hey! Notifications are working!");
        }

        private void Button9_Click(object sender, EventArgs e)
        {
            PS3.CCAPI.ShutDown(CCAPI.RebootFlags.HardReboot);
        }

        private void Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void Button14_Click(object sender, EventArgs e)
        {
            shaderp.Location = new Point(5, 149); // type your X and Y coordinates here
            shaderp.Visible = true; // Display the panel

            
        }

        private void Button12_Click(object sender, EventArgs e)
        {
            try
            {
                PS3.CCAPI.Notify(CCAPI.NotifyIcon.CAUTION, "PS Connect | Check your PC!");
                DialogResult dialogResult = MessageBox.Show("Are you sure you wish to set your CID / PSID's and reboot?", "PS Connect", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if(dialogResult == DialogResult.Yes)
                {
                    PS3.CCAPI.SetConsoleID(consolepanel.Text);
                    PS3.CCAPI.SetPSID(psidtxt.Text);
                    MessageBox.Show("Rebooting Console Now.", "PS Connect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    PS3.CCAPI.ShutDown(CCAPI.RebootFlags.HardReboot);
                }
                if(dialogResult == DialogResult.No)
                {
                    MessageBox.Show("Nothing Happened, your CID / PSID is not changed.", "PS Connect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch
            {
                MessageBox.Show("Console is not connected, task failed.");
            }
        }

        private void Button16_Click(object sender, EventArgs e)
        {
            try
            {
                cellTemp.Text = "" + PS3.CCAPI.GetTemperatureCELL();
                rsxTemp.Text = "" + PS3.CCAPI.GetTemperatureRSX();
                fwtxt.Text = "" + PS3.CCAPI.GetFirmwareVersion();
                systxt.Text = "" + PS3.CCAPI.GetFirmwareType();
            }
            catch
            {
                DialogResult dialogResult = MessageBox.Show("Error, you are not connected. Would you like to reconnect?", "PS Connect", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    try
                    {
                        PS3.CCAPI.ConnectTarget();
            
                        PS3.CCAPI.Notify(CCAPI.NotifyIcon.INFO, "PS Connect | Connected!");
                    }
                    catch
                    {
                        MessageBox.Show("Restart the tool, and make sure the IP Address is correct.");
                    }
                }
                if (dialogResult == DialogResult.No)
                {
                    MessageBox.Show("Nothing happened.", "PS Connect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void Button17_Click(object sender, EventArgs e)
        {
            try
            {
                PS3.CCAPI.Notify(CCAPI.NotifyIcon.INFO, "PS Connect | " + notify.Text);
            }
            catch
            {
                DialogResult dialogResult = MessageBox.Show("Error, you are not connected. Would you like to reconnect?", "PS Connect", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    try
                    {
                        PS3.CCAPI.ConnectTarget();
                        PS3.CCAPI.Notify(CCAPI.NotifyIcon.INFO, "PS Connect | Connected!");
                    }
                    catch
                    {
                        MessageBox.Show("Restart the tool, and make sure the IP Address is correct.");
                    }
                }
                if (dialogResult == DialogResult.No)
                {
                    MessageBox.Show("Nothing happened.", "PS Connect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (cmd.Text == "/ps3 connect")
            {
                DialogResult dialogResult = MessageBox.Show("If this is correct, hit Yes, otherwise, hit No. \n" + cmd.Text, "PS Connect", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    connect();
                }
                if (dialogResult == DialogResult.No)
                {
                    MessageBox.Show("Nothing happened.", "PS Connect", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void Button15_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is the current list of commands!\n/ps3 connect //Connects CCAPI to your ps3.", "PS Connect", MessageBoxButtons.OK);
        }

        private void OpenZoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Grab your patch_mp.ff",
                Filter = "Fast Files (*.ff)|*.ff",
                RestoreDirectory = true
            };
            OpenFileDialog openFileDialog2 = openFileDialog;
            bool flag = openFileDialog2.ShowDialog() == DialogResult.OK;
            if (flag)
            {
                string fileName = openFileDialog2.FileName;
                string zonefile = Path.GetDirectoryName(openFileDialog2.FileName) + "\\" + Path.GetFileNameWithoutExtension(openFileDialog2.FileName) + ".zone";
                this.getZoneFile(fileName, zonefile);
            }
        }

        private void compressZone()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                AddExtension = true,
                RestoreDirectory = true,
                Title = "Select a PS3 Zone File...",
                Filter = "Zone Files (*.zone)|*.zone|Dat Files (*.dat)|*.dat"
            };
            OpenFileDialog openFileDialog2 = openFileDialog;
            bool flag = openFileDialog2.ShowDialog() == DialogResult.OK;
            if (flag)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    AddExtension = true,
                    Title = "Compress into fast file...",
                    Filter = "FF (*.ff)|*.ff"
                };
                SaveFileDialog saveFileDialog2 = saveFileDialog;
                saveFileDialog2.OverwritePrompt = false;
                bool flag2 = saveFileDialog2.ShowDialog() == DialogResult.OK;
                if (flag2)
                {
                    BinaryReader binaryReader = new BinaryReader(new FileStream(openFileDialog2.FileName, FileMode.Open), Encoding.Default);
                    BinaryWriter binaryWriter = new BinaryWriter(new FileStream(saveFileDialog2.FileName, FileMode.Create), Encoding.Default);
                    binaryWriter.Write(this.ffHeader);
                    binaryReader.BaseStream.Position = 0L;
                    int num = Convert.ToInt32(binaryReader.BaseStream.Length / 65536L);
                    int num2;
                    for (int i = 1; i <= num; i = num2 + 1)
                    {
                        byte[] b = binaryReader.ReadBytes(65536);
                        byte[] array = new byte[0];
                        array = Form1.CompressBuffer(b);
                        byte[] bytes = BitConverter.GetBytes(Convert.ToInt32(array.Length - 2));
                        Array.Reverse(bytes);
                        byte[] array2 = new byte[array.Length];
                        Buffer.BlockCopy(bytes, 2, array2, 0, 2);
                        Buffer.BlockCopy(array, 2, array2, 2, array.Length - 2);
                        binaryWriter.Write(array2);
                        num2 = i;
                    }
                    binaryReader.Close();
                    binaryWriter.BaseStream.Position = 13L;
                    binaryWriter.Write(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(Convert.ToInt64(DateTime.Now.Subtract(new TimeSpan(0, 4, 0, 0)).ToFileTimeUtc()))));
                    byte[] bytes2 = BitConverter.GetBytes(IPAddress.NetworkToHostOrder(Convert.ToInt32(binaryWriter.BaseStream.Length)));
                    binaryWriter.BaseStream.Position = 29L;
                    binaryWriter.Write(bytes2);
                    binaryWriter.Write(bytes2);
                    binaryWriter.Close();
                }
            }
        }
        // Token: 0x04000001 RID: 1
        private int extra = 0;

        // Token: 0x04000002 RID: 2
        private string addspace = " ";

        // Token: 0x04000003 RID: 3
        private string zonewrd = "";

        // Token: 0x04000004 RID: 4
        private int zonemax = 0;

        // Token: 0x04000005 RID: 5
        private int zonelen = 0;

        // Token: 0x04000006 RID: 6
        private int zoneofs = 0;

        // Token: 0x04000007 RID: 7
        private int zoneofsx = 0;

        // Token: 0x04000008 RID: 8
        private byte[] zonebyte = new byte[1];

        // Token: 0x04000009 RID: 9
        private bool isloaded = false;

        // Token: 0x0400000A RID: 10
        private Random EElol = new Random();

        // Token: 0x0400000B RID: 11
        private int luckynum = 0;

        // Token: 0x0400000C RID: 12
        private byte[] ffHeader;

        private void InjectZoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ffHeader = new byte[]
{
                73,
                87,
                102,
                102,
                117,
                49,
                48,
                48,
                0,
                0,
                1,
                13,
                1,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                1,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0
};
            this.compressZone();
        }

        private void FileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void OpenZoneToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openzone = new OpenFileDialog();
            openzone.Title = "Choose your .ZONE File";
            openzone.Filter = ".ZONE Files | *.zone";
            if (openzone.ShowDialog() == DialogResult.OK)
            {
                path = openzone.FileName;
                button16.Enabled = true;
                button17.Enabled = true;
                MessageBox.Show("Zone File Opened!\nEvery edit you make is perminant!");

            }
            else
            {
                MessageBox.Show("Nothing has been done.");
            }
        }

        private void Button20_Click(object sender, EventArgs e)
        {
            PS3.SetMemory((0x014e2467), new byte[] { 0x02 });
        }

        private void RemoveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleGodModde(dataGridView1.CurrentRow.Index, false);
            noty("God Mode Disabled");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.RowCount = 18;
            //
            for (int i = 0; i < 18; i++)
            {
                dataGridView1.Rows[i].Cells[0].Value = i;
            }
        }

        private void Button21_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 1)
            {
                dataGridView1.Rows.Add(17);
            }
            for (int i =0; i < 18; i++)
            {
                dataGridView1[0, Convert.ToInt32(i)].Value = i;
                dataGridView1[1, Convert.ToInt32(i)].Value = GetNames(i);
            }
        }
        public static string GetNames(int client)
        {
            string names = PS3.Extension.ReadString(0x014E5408 + ((uint)client * 0x3700));
            return names;
        }

        private void GiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleGodModde(dataGridView1.CurrentRow.Index, true);
            noty("God Mode Enabled");
        }
        public static void ToggleGodModde(int client, bool GMode)
        {
            if (GMode == true)
            {
                byte[] On = new byte[] { 0xFF, 0xFF };
                PS3.SetMemory(0x14E235A + ((uint)client * 0x3700), On);
            }
            else if (GMode == false)
            {
                byte[] Off = new byte[] { 0x00, 0x64 };
                PS3.SetMemory(0x14E235A + ((uint)client * 0x3700), Off);
            }
        }

        public static void ToggleAmmo(int client, bool GMode)
        {
            if (GMode == true)
            {
                byte[] On = new byte[] { 0x0F, 0xFF, 0xFF, 0xFF };
                PS3.SetMemory(0x014E256C + ((uint)client * 0x3700), On);
            }
            else if (GMode == false)
            {
                byte[] Off = new byte[] { 0x00, 0x00, 0x00, 0xFF };
                PS3.SetMemory(0x014E256C + ((uint)client * 0x3700), Off);
            }
        }
        public static void ToggleBoxes(int client, bool GMode)
        {
            if (GMode == true)
            {
                byte[] On = new byte[] { 0x55 };
                PS3.SetMemory(0x014E2213 + ((uint)client * 0x3700), On);
            }
            else if (GMode == false)
            {
                byte[] Off = new byte[] { 0x00 };
                PS3.SetMemory(0x014E2213 + ((uint)client * 0x3700), Off);
            }
        }

        public static void ToggleAkimbo(int client, bool GMode)
        {
            if (GMode == true)
            {
                byte[] On = new byte[] { 0x01 };
                PS3.SetMemory(0x014E2467 + ((uint)client * 0x3700), On);
            }
            else if (GMode == false)
            {
                byte[] Off = new byte[] { 0x00 };
                PS3.SetMemory(0x014E2467 + ((uint)client * 0x3700), Off);
            }
        }

        public static void ToggleAkimboo(int client, bool GMode)
        {
            if (GMode == true)
            {
                byte[] On = new byte[] { 0x01 };
                PS3.SetMemory(0x014e245d + ((uint)client * 0x3700), On);
            }
            else if (GMode == false)
            {
                byte[] Off = new byte[] { 0x00 };
                PS3.SetMemory(0x014e245d + ((uint)client * 0x3700), Off);
            }
        }
        public static void ToggleNoclip(int client, bool GMode)
        {
            if (GMode == true)
            {
                byte[] On = new byte[] { 0x01 };
                PS3.SetMemory(0x014E5623 + ((uint)client * 0x3700), On);
            }
            else if (GMode == false)
            {
                byte[] Off = new byte[] { 0x00 };
                PS3.SetMemory(0x014E5623 + ((uint)client * 0x3700), Off);
            }
        }

        public static void ToggleUFO(int client, bool GMode)
        {
            if (GMode == true)
            {
                byte[] On = new byte[] { 0x02 };
                PS3.SetMemory(0x014E5623 + ((uint)client * 0x3700), On);
            }
            else if (GMode == false)
            {
                byte[] Off = new byte[] { 0x00 };
                PS3.SetMemory(0x014E5623 + ((uint)client * 0x3700), Off);
            }
        }

        public static void ToggleFreeze(int client, bool GMode)
        {
            if (GMode == true)
            {
                byte[] On = new byte[] { 0x04 };
                PS3.SetMemory(0x014E5623 + ((uint)client * 0x3700), On);
            }
            else if (GMode == false)
            {
                byte[] Off = new byte[] { 0x00 };
                PS3.SetMemory(0x014E5623 + ((uint)client * 0x3700), Off);
            }
        }

        public static void ToggleCamo(int client, int num)
        {
            if (num == 1)
            {
                byte[] On = new byte[] { 0x00 };
                PS3.SetMemory(0x014E245E + ((uint)client * 0x3700), On);
            }
            if (num == 2)
            {
                byte[] On = new byte[] { 0x01 };
                PS3.SetMemory(0x014E245E + ((uint)client * 0x3700), On);
            }
            if (num == 3)
            {
                byte[] On = new byte[] { 0x02 };
                PS3.SetMemory(0x014E245E + ((uint)client * 0x3700), On);
            }
            if (num == 4)
            {
                byte[] On = new byte[] { 0x03 };
                PS3.SetMemory(0x014E245E + ((uint)client * 0x3700), On);
            }
            if (num == 5)
            {
                byte[] On = new byte[] { 0x04 };
                PS3.SetMemory(0x014E245E + ((uint)client * 0x3700), On);
            }
            if (num == 6)
            {
                byte[] On = new byte[] { 0x05 };
                PS3.SetMemory(0x014E245E + ((uint)client * 0x3700), On);
            }
            if (num == 7)
            {
                byte[] On = new byte[] { 0x06 };
                PS3.SetMemory(0x014E245E + ((uint)client * 0x3700), On);
            }
            if (num == 8)
            {
                byte[] On = new byte[] { 0x07 };
                PS3.SetMemory(0x014E245E + ((uint)client * 0x3700), On);
            }
            else if (num == 9)
            {
                byte[] On = new byte[] { 0x08 };
                PS3.SetMemory(0x014E245E + ((uint)client * 0x3700), On);
            }
        }
        public static void WritePowerPc(bool Active)

        {

            byte[] NewPPC = new byte[] { 0xF8, 0x21, 0xFF, 0x61, 0x7C, 0x08, 0x02, 0xA6, 0xF8, 0x01, 0x00, 0xB0, 0x3C, 0x60, 0x10, 0x03, 0x80, 0x63, 0x00, 0x00, 0x60, 0x62, 0x00, 0x00, 0x3C, 0x60, 0x10, 0x04, 0x80, 0x63, 0x00, 0x00, 0x2C, 0x03, 0x00, 0x00, 0x41, 0x82, 0x00, 0x28, 0x3C, 0x60, 0x10, 0x04, 0x80, 0x63, 0x00, 0x04, 0x3C, 0xA0, 0x10, 0x04, 0x38, 0x80, 0x00, 0x00, 0x30, 0xA5, 0x00, 0x10, 0x4B, 0xE8, 0xB2, 0x7D, 0x38, 0x60, 0x00, 0x00, 0x3C, 0x80, 0x10, 0x04, 0x90, 0x64, 0x00, 0x00, 0x3C, 0x60, 0x10, 0x05, 0x80, 0x63, 0x00, 0x00, 0x2C, 0x03, 0x00, 0x00, 0x41, 0x82, 0x00, 0x24, 0x3C, 0x60, 0x10, 0x05, 0x30, 0x63, 0x00, 0x10, 0x4B, 0xE2, 0xF9, 0x7D, 0x3C, 0x80, 0x10, 0x05, 0x90, 0x64, 0x00, 0x04, 0x38, 0x60, 0x00, 0x00, 0x3C, 0x80, 0x10, 0x05, 0x90, 0x64, 0x00, 0x00, 0x3C, 0x60, 0x10, 0x03, 0x80, 0x63, 0x00, 0x04, 0x60, 0x62, 0x00, 0x00, 0xE8, 0x01, 0x00, 0xB0, 0x7C, 0x08, 0x03, 0xA6, 0x38, 0x21, 0x00, 0xA0, 0x4E, 0x80, 0x00, 0x20 };

            byte[] RestorePPC = new byte[] { 0x81, 0x62, 0x92, 0x84, 0x7C, 0x08, 0x02, 0xA6, 0xF8, 0x21, 0xFF, 0x01, 0xFB, 0xE1, 0x00, 0xB8, 0xDB, 0x01, 0x00, 0xC0, 0x7C, 0x7F, 0x1B, 0x78, 0xDB, 0x21, 0x00, 0xC8, 0xDB, 0x41, 0x00, 0xD0, 0xDB, 0x61, 0x00, 0xD8, 0xDB, 0x81, 0x00, 0xE0, 0xDB, 0xA1, 0x00, 0xE8, 0xDB, 0xC1, 0x00, 0xF0, 0xDB, 0xE1, 0x00, 0xF8, 0xFB, 0x61, 0x00, 0x98, 0xFB, 0x81, 0x00, 0xA0, 0xFB, 0xA1, 0x00, 0xA8, 0xFB, 0xC1, 0x00, 0xB0, 0xF8, 0x01, 0x01, 0x10, 0x81, 0x2B, 0x00, 0x00, 0x88, 0x09, 0x00, 0x0C, 0x2F, 0x80, 0x00, 0x00, 0x40, 0x9E, 0x00, 0x64, 0x7C, 0x69, 0x1B, 0x78, 0xC0, 0x02, 0x92, 0x94, 0xC1, 0xA2, 0x92, 0x88, 0xD4, 0x09, 0x02, 0x40, 0xD0, 0x09, 0x00, 0x0C, 0xD1, 0xA9, 0x00, 0x04, 0xD0, 0x09, 0x00, 0x08, 0xE8, 0x01, 0x01, 0x10, 0xEB, 0x61, 0x00, 0x98, 0xEB, 0x81, 0x00, 0xA0, 0x7C, 0x08, 0x03, 0xA6, 0xEB, 0xA1, 0x00, 0xA8, 0xEB, 0xC1, 0x00, 0xB0, 0xEB, 0xE1, 0x00, 0xB8, 0xCB, 0x01, 0x00, 0xC0, 0xCB, 0x21, 0x00, 0xC8 };

            if (Active)

                PS3.SetMemory(0x0038EDE8, NewPPC);

            else

                PS3.SetMemory(0x0038EDE8, RestorePPC);
        }

        public static void SV_SendServerCommand(int clientIndex, int num, string Command)
        {

            Form1.WritePowerPc(true);

            PS3.Extension.WriteString(0x10040010, Command);

            PS3.Extension.WriteInt32(0x10040004, clientIndex);

            PS3.Extension.WriteBool(0x10040003, true);

            bool isRunning;

            do { isRunning = PS3.Extension.ReadBool(0x10040003); } while (isRunning != false);

            Form1.WritePowerPc(false);
        }
        public static void SetClientDvars(int client, string dvars)

        {

            SV_SendServerCommand(client, 1, "v " + dvars);
        }

        private void ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            ToggleAmmo(dataGridView1.CurrentRow.Index, true);
            noty("Infinite Ammo Enabled");
        }
        private void ToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            ToggleAmmo(dataGridView1.CurrentRow.Index, false);
            noty("Infinite Ammo Disabled");
        }

        private void ToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            ToggleAkimbo(dataGridView1.CurrentRow.Index, true);
            noty("Akimbo Enabled");
        }

        private void ToolStripMenuItem6_Click(object sender, EventArgs e)
        {
            ToggleAkimbo(dataGridView1.CurrentRow.Index, false);
            noty("Akimbo's disabled");
        }

        private void ToolStripMenuItem8_Click(object sender, EventArgs e)
        {
            ToggleNoclip(dataGridView1.CurrentRow.Index, true);
            noty("Noclip Enabled");
        }

        private void ToolStripMenuItem9_Click(object sender, EventArgs e)
        {
            ToggleNoclip(dataGridView1.CurrentRow.Index, false);
            noty("Noclip disabled");
        }
        public void noty(string notification)
        {
            PS3.CCAPI.Notify(CCAPI.NotifyIcon.INFO, "PS Connect | " + notification);
        }

        private void ToolStripMenuItem11_Click(object sender, EventArgs e)
        {
            ToggleBoxes(dataGridView1.CurrentRow.Index, true);
            noty("RedBoxes Enabled");
        }

        private void ToolStripMenuItem12_Click(object sender, EventArgs e)
        {
            ToggleBoxes(dataGridView1.CurrentRow.Index, false);
            noty("RedBoxes Disabled");
        }

        private void ToolStripMenuItem14_Click(object sender, EventArgs e)
        {
            ToggleUFO(dataGridView1.CurrentRow.Index, true);
            noty("UFO Mode Enabled");
        }

        private void ToolStripMenuItem15_Click(object sender, EventArgs e)
        {
            ToggleUFO(dataGridView1.CurrentRow.Index, false);
            noty("UFO Mode Disabled");
        }

        private void ToolStripMenuItem17_Click(object sender, EventArgs e)
        {
            ToggleFreeze(dataGridView1.CurrentRow.Index, true);
            noty("Player Frozen");
        }

        private void ToolStripMenuItem18_Click(object sender, EventArgs e)
        {
            ToggleFreeze(dataGridView1.CurrentRow.Index, false);
            noty("Player Unfrozen");
        }

        private void ToolStripMenuItem20_Click(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 0);
            noty("Camo Default");
        }

        private void ToolStripMenuItem26_Click(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 1);
            noty("Camo Desert");
        }

        private void ToolStripMenuItem25_Click(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 2);
            noty("Camo Arctic");
        }

        private void ToolStripMenuItem24_Click(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 3);
            noty("Camo Woodland");
        }

        private void ToolStripMenuItem23_Click(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 4);
            noty("Camo Digital");
        }

        private void ToolStripMenuItem22_Click(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 5);
            noty("Camo Urban");
        }

        private void ToolStripMenuItem21_Click(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 6);
            noty("Camo Blue Tiger");
        }

        private void ToolStripMenuItem29_Click(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 7);
            noty("Camo Red Tiger");
        }

        private void ToolStripMenuItem28_Click(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 8);
            noty("Cammo Default");
        }

        public static void GiveUnlockAll(int client)

        {


        }

        private void Button24_Click(object sender, EventArgs e)
        {

        }

        private void Button35_Click(object sender, EventArgs e)
        {
            if (prestige == 11)
            {
                prestige = 0;
                textBox3.Text = prestige.ToString();
            }
            else if (prestige < 11)
            {
                prestige = prestige + 1;
                textBox3.Text = prestige.ToString();
            }
        }

        private void Button60_Click(object sender, EventArgs e)
        {
            if (prestige == 0)
            {
                prestige = 11;
                textBox3.Text = prestige.ToString();
            }
            else if (prestige > 0)
            {
                prestige = prestige - 1;
                textBox3.Text = prestige.ToString();
            }
        }

        private void Button22_Click(object sender, EventArgs e)
        {
            if (prestige == 0)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x00 });
            }
            if (prestige == 1)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x01 });
            }
            if (prestige == 2)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x02 });
            }
            if (prestige == 3)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x03 });
            }
            if (prestige == 4)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x04 });
            }
            if (prestige == 5)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x05 });
            }
            if (prestige == 6)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x06 });
            }
            if (prestige == 7)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x07 });
            }
            if (prestige == 8)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x08 });
            }
            if (prestige == 9)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x09 });
            }
            if (prestige == 10)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x0A });
            }
            if (prestige == 11)
            {
                noty("Prestige Changed");
                PS3.SetMemory(0x01FF9A9C, new byte[] { 0x0B });
            }

        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        public byte[] StringToByteArray(string str)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            return encoding.GetBytes(str);
        }

        private void Button23_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(textBox2.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x01F9F11C, bytes);
        }

   

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if ( checkBox1.Checked == true)
            {
                PS3.CCAPI.SetMemory(0x1D148C7, new byte[] { 0x01 });

                PS3.CCAPI.SetMemory(0x1D148B8, new byte[] { 0x01 });
                noty("Force Host Enabled");
            }
            else
            {
                PS3.CCAPI.SetMemory(0x1D148C7, new byte[] { 0x00 });

                PS3.CCAPI.SetMemory(0x1D148B8, new byte[] { 0x00 });
                noty("Force Host Disabled");
            }
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                PS3.CCAPI.SetMemory(0x01d1ff04, new byte[] { 0x01 });

                noty("UAV Enabled");
            }
            else
            {
                PS3.CCAPI.SetMemory(0x01d1ff04, new byte[] { 0x00 });
                noty("UAV Disabled");
            }
        }

        private void CheckBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                PS3.CCAPI.SetMemory(0x9342C, new byte[] { 0x60, 0x00, 0x00, 0x00 });

                noty("No Recoil Enabled");
            }
            else
            {
                PS3.CCAPI.SetMemory(0x9342C, new byte[] { 0x4B, 0xFA, 0x10, 0xF5 });
                noty("No Recoil Disabled");
            }
        }

        private void Button36_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.Show();
            
        }

        private void Button36_Click_1(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name1.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x01ff9e6c, bytes);
        }

        private void Button37_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name2.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x01ff9eac, bytes);
        }

        private void Button38_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name3.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x1FF9EEC, bytes);
        }

        private void Button39_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name4.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x1FF9F2C, bytes);
        }

        private void Button40_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name5.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x1FF9F6C, bytes);
        }

        private void Button41_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name6.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x1FF9FAC, bytes);
        }

        private void Button42_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name7.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x1FF9FEC, bytes);
        }

        private void Button43_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name8.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x1FFA02C, bytes);
        }

        private void Button44_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name9.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x1FFA06C, bytes);
        }

        private void Button45_Click(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(name10.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x1FFA0AC, bytes);
        }

        private void AimbotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RPC.EnableRPC();
            DoAimbot(dataGridView1.CurrentRow.Index);
        }

        private void ToolStripMenuItem30_Click(object sender, EventArgs e)
        {
            ToggleAkimboo(dataGridView1.CurrentRow.Index, true);
        }

        private void ToolStripMenuItem31_Click(object sender, EventArgs e)
        {
            ToggleAkimboo(dataGridView1.CurrentRow.Index, false);
        }

        private void Button46_Click(object sender, EventArgs e)
        {
            PS3.SetMemory(0x2005000, new byte[] { 0x75, 0x69, 0x5f, 0x6d, 0x61, 0x70, 0x6e, 0x61, 0x6d, 0x65, 0x22, 0x6d, 0x70, 0x5f, 0x72, 0x75, 0x73, 0x74, 0x3b, 0x5e, 0x31, 0x54, 0x65, 0x73, 0x74, 0x22 });

            PS3.SetMemory(0x253AB8, new byte[] { 0x38, 0x60, 0x00, 0x00, 0x3C, 0x80, 0x02, 0x00, 0x30, 0x84, 0x50, 0x00, 0x4B, 0xF8, 0x63, 0xFD });

            PS3.SetMemory(0x253AB8, new byte[] { 0x81, 0x22, 0x45, 0x10, 0x81, 0x69, 0x00, 0x00, 0x88, 0x0B, 0x00, 0x0C, 0x2F, 0x80, 0x00, 0x00 });

            PS3.SetMemory(0x2005000, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        }

        private void Prestige10ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RPC.EnableRPC();
            SetClientDvars(dataGridView1.CurrentRow.Index, "cg_fov 60");
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            ToggleAkimboo(dataGridView1.CurrentRow.Index, true);
        }

        private void Button24_Click_1(object sender, EventArgs e)
        {
            akimbotimer.Start();
        }

        private void Button25_Click(object sender, EventArgs e)
        {
            ammotimer.Start();
        }

        private void Ammotimer_Tick(object sender, EventArgs e)
        {
            ToggleAmmo(dataGridView1.CurrentRow.Index, true);
        }

        private void Button26_Click(object sender, EventArgs e)
        {
            redboxtimer.Start();
        }

        private void Allammo_Tick(object sender, EventArgs e)
        {
            ToggleBoxes(dataGridView1.CurrentRow.Index, true);
        }

        private void Button27_Click(object sender, EventArgs e)
        {
            ammotimer.Stop();
            akimbotimer.Stop();
            redboxtimer.Stop();
            camotimer.Stop();
        }

        private void Button28_Click(object sender, EventArgs e)
        {
            redboxtimer.Start();
        }

        private void PictureBox3_Click(object sender, EventArgs e)
        {
            timerpanel.Show();
            classpanel.Hide();
        }

        private void PictureBox2_Click(object sender, EventArgs e)
        {
            timerpanel.Hide();
            classpanel.Show();
        }

        private void Button26_Click_1(object sender, EventArgs e)
        {
            camotimer.Start();
        }

        private void Timer1_Tick_1(object sender, EventArgs e)
        {
            ToggleCamo(dataGridView1.CurrentRow.Index, 7);
        }

        private void Button29_Click(object sender, EventArgs e)
        {
        
            timer1.Start();
        }

        private void Button30_Click(object sender, EventArgs e)
        {
            CoDLibrary.MW2.Func.ExplosiveBullets(2, true);
            MW2Lib.RPC.EnableRPC();
            CoDLibrary.MW2.HudAndRPC.SV_SendServerCommand(dataGridView1.CurrentRow.Index, "N 2064 07000");
        }

        private void Timer1_Tick_2(object sender, EventArgs e)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(textBox5.Text);
            Array.Resize<byte>(ref bytes, bytes.Length + 1);
            PS3.CCAPI.SetMemory(0x01F9F11C, bytes);
        }
    }
}

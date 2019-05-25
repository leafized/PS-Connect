using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PS_Connect
{
    public partial class Form3 : Form
    {
        int[] MenuScroll = new int[18];
        public static string[] Menu_Title = new string[18];
        bool[] InMenu = new bool[18];
        public static string[] Status = new string[18];
        bool[] IsVerified = new bool[18];
        string[] SubMenu = new string[18];
        int[] MaxScroll = new int[18];
        public Form3()
        {
            InitializeComponent();
        }

        private void Button6_Click(object sender, EventArgs e)
        {

        }
    }
}

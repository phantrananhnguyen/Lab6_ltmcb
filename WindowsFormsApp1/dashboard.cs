using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class dashboard : Form
    {
        public dashboard()
        {
            InitializeComponent();
        }

        private void btn_server_Click(object sender, EventArgs e)
        {
            server form = new server();
            form.Show();
            btn_server.Enabled = false;
        }

        private void btn_client_Click(object sender, EventArgs e)
        {
            starting form = new starting();
            form.Show();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MIddlewareTestWindowsApp
{
    public partial class FormConnect : Form
    {
        public FormConnect()
        {
            InitializeComponent();
        }

       public string GetUser()
        {
            return txtUsername.Text;
        }

        public string GetPassword()
        {
            return txtPassword.Text;
        }
    }
}

using ImageFormats.Properties;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

/*
 
This is a test application that tests the ImageFormats class library
included with this project. Refer to the individual source code
files for each image type for more information.

Copyright 2013 by Warren Galyen.
You may use this source code in your application(s) free of charge,
as long as attribution is given to me (Warren Galyen) and my URL
(http://mechanikadesign.com) in your application's "about" box and/or
documentation. 

*/

namespace ImageViewer
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            this.Text = Application.ProductName;
        }

        private void FormMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
                e.Effect = DragDropEffects.All;
        }

        private void FormMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0) return;
            OpenFile(files[0]);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var openDlg = new OpenFileDialog();
            openDlg.DefaultExt = ".*";
            openDlg.CheckFileExists = true;
            openDlg.Title = Resources.openDlgTitle;
            openDlg.Filter = "All Files (*.*)|*.*";
            openDlg.FilterIndex = 1;
            if (openDlg.ShowDialog() == DialogResult.Cancel) return;
            OpenFile(openDlg.FileName);
        }

        private void OpenFile(string fileName)
        {
            try
            {
                Bitmap bmp = null;
                bmp = MechanikaDesign.ImageFormats.Picture.Load(fileName);

                if (bmp == null)
                {
                    //try loading the file natively...
                    try { bmp = (Bitmap)Bitmap.FromFile(fileName); }
                    catch (Exception e) { Debug.WriteLine(e.Message); }
                }

                if (bmp == null)
                    throw new ApplicationException(Resources.errorLoadFailed);

                pictureBox1.Image = bmp;
                pictureBox1.Size = bmp.Size;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

    }
}

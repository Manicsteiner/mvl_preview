using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mvlView
{
    struct Chara
    {
        public string body;
        public List<string> eyes;
        public List<string> mouths ;
    }
    public partial class Form1 : Form
    {
        JObject json;
        List<Chara> data=new List<Chara>();
        string path;
        Bitmap pic;
        int body_index,eye_index,mouth_index;
        int body_x0, body_y0;

        public Form1()
        {
            InitializeComponent();
        }

        private void openJsonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Filter = "Json File|*.json|All File|*.*";
            file.ShowDialog();
            if (File.Exists(file.FileName))
            {
                data.Clear();
                listBox1.Items.Clear();
                path = Path.GetDirectoryName(file.FileName) + "\\";
                OpenJSON(file.FileName);
            }
        }

        public void OpenJSON(string filename)
        {
            StreamReader sr = new StreamReader(filename);
            json = (JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
            sr.Close();
            
            foreach(var item in json)
            {
                if (item.Key.Length==9)
                {
                    Chara temp;
                    temp.body = item.Key;
                    temp.eyes = new List<string>();
                    temp.mouths = new List<string>();
                    foreach (var block in json)
                    {
                        if (block.Key.Length == 11 && block.Key.Substring(0,9)== item.Key)
                        {
                            if (block.Key.Substring(9, 1) == "E")
                            {
                                temp.eyes.Add(block.Key);
                            }
                            if (block.Key.Substring(9, 1) == "L")
                            {
                                temp.mouths.Add(block.Key);
                            }

                        }
                    }
                    data.Add(temp);
                }
            }
            for(int i = 0; i < data.Count; i++)
            {
                listBox1.Items.Add(data[i].body);
            }

        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItems.Count <= 0)
                return;

            body_index = listBox1.SelectedIndex;
            
            if (body_index >= 0)
            {
                //eyes
                listView_eye.Clear();
                ImageList image_list = new ImageList();  
                for(int i=0;i< data[body_index].eyes.Count; i++)
                {
                    image_list.Images.Add(Bitmap.FromFile(path + data[body_index].eyes[i] + ".png"));
                    image_list.ImageSize = new Size(75, 75);
                }
                listView_eye.LargeImageList = image_list;
                for (int i = 0; i < data[body_index].eyes.Count; i++)
                {
                    ListViewItem item = new ListViewItem();
                    item.ImageIndex = i;
                    listView_eye.Items.Add(item);
                }

                //mouths
                listView_mouth.Clear();
                image_list = new ImageList();
                for (int i = 0; i < data[body_index].mouths.Count; i++)
                {
                    image_list.Images.Add(Bitmap.FromFile(path + data[body_index].mouths[i] + ".png"));
                    image_list.ImageSize = new Size(75, 75);
                }
                listView_mouth.LargeImageList = image_list;
                for (int i = 0; i < data[body_index].mouths.Count; i++)
                {
                    ListViewItem item = new ListViewItem();
                    item.ImageIndex = i;
                    listView_mouth.Items.Add(item);
                }


                //init
                eye_index = mouth_index = 0;
                listView_eye.Select();
                listView_mouth.Select();
                DrawBody(body_index);
                DrawEye(body_index, eye_index);
                DrawMouth(body_index, mouth_index);
                view.Image = pic;
               


            }
            
        }
        public void DrawBody(int body_index, bool save = false)
        {
            Bitmap bmp = new Bitmap(path + data[body_index].body + ".png");
            label5.Text = bmp.Width.ToString() + "x" + bmp.Height.ToString();
            body_x0 = (int)json[data[body_index].body]["min_x"];
            body_y0 = (int)json[data[body_index].body]["min_y"];
            pic = new Bitmap(bmp.Width, bmp.Height);
            Graphics g = Graphics.FromImage(pic);
            if(!save)
                g.Clear(Color.FromArgb(225, 225, 225, 255));
            g.DrawImage(bmp, 0, 0);
            bmp.Dispose();
            g.Dispose();
        }
        public void DrawEye(int body_index,int eye_index)
        {
            Bitmap bmp = new Bitmap(path + data[body_index].eyes[eye_index] + ".png");
            Graphics g = Graphics.FromImage(pic);
            int x = (int)json[data[body_index].eyes[eye_index]]["min_x"] - body_x0;
            int y = (int)json[data[body_index].eyes[eye_index]]["min_y"] - body_y0;
            g.DrawImage(bmp, x, y);
            bmp.Dispose();
            g.Dispose();
        }
        public void DrawMouth(int body_index, int mouth_index)
        {
            Bitmap bmp = new Bitmap(path + data[body_index].mouths[mouth_index] + ".png");
            Graphics g = Graphics.FromImage(pic);
            int x = (int)json[data[body_index].mouths[mouth_index]]["min_x"] - body_x0;
            int y = (int)json[data[body_index].mouths[mouth_index]]["min_y"] - body_y0;
            g.DrawImage(bmp, x, y);
            bmp.Dispose();
            g.Dispose();
        }
        private void listView_mouth_DoubleClick(object sender, EventArgs e)
        {
            if (listView_mouth.SelectedItems.Count <= 0)
                return;
            eye_index = listView_mouth.FocusedItem.Index;
            DrawMouth(body_index, eye_index);
            view.Image = pic;
        }
        private void listView_eye_DoubleClick(object sender, EventArgs e)
        {
            if (listView_eye.SelectedItems.Count <= 0)
                return;
            mouth_index = listView_eye.FocusedItem.Index;
            DrawEye(body_index, mouth_index);
            view.Image = pic;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (body_index < 0 || body_index > data.Count) return;
            DrawBody(body_index,true);
            DrawEye(body_index, listView_eye.FocusedItem.Index);
            DrawMouth(body_index, listView_mouth.FocusedItem.Index);
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\chara"))
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\chara");
            pic.Save(Directory.GetCurrentDirectory() + "\\chara\\" + data[body_index].body + "E" + eye_index.ToString() + "L" + mouth_index.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
            MessageBox.Show("Save success!");
        }

       
    }
}

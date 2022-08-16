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
        public List<string> mouths;
        //在结构体定义里增加了用于判断该立绘是否有眼、嘴分支的变量
        public bool haseyes;
        public bool hasmouths;
    }
    public partial class Form1 : Form
    {
        JObject json;
        List<Chara> data=new List<Chara>();
        string path;
        Bitmap pic;
        int body_index,eye_index,mouth_index;
        int body_x0, body_y0;
        string[] inargs;

        public Form1()
        {
            InitializeComponent();
        }

        public Form1(string[] args)
        {
            this.inargs = args;
            InitializeComponent();

            for(int i = 0; i < inargs.Length; i++)
            {
                if (File.Exists(inargs[i]))
                {
                    data.Clear();
                    listBox1.Items.Clear();
                    path = Path.GetDirectoryName(inargs[i]) + "\\";
                    OpenJSON(inargs[i]);
                    saveAll();
                }
            }
            this.Close();
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

            //新增两个变量，用于定义body.png名称长度
            bool lengthdef = false;
            int baselength = 0;
            foreach(var item in json)
            {
                if (!lengthdef)
                {
                    baselength = item.Key.Length;
                    lengthdef = true;
                }
                if (item.Key.Length == baselength)
                {
                    Chara temp;
                    temp.body = item.Key;
                    temp.eyes = new List<string>();
                    temp.mouths = new List<string>();
                    //初始化，假定立绘没有子分支
                    temp.haseyes = false;
                    temp.hasmouths = false;
                    foreach (var block in json)
                    {
                        if (block.Key.Length - 2 == baselength && block.Key.Substring(0,baselength)== item.Key)
                        {
                            if (block.Key.Substring(baselength, 1) == "E")
                            {
                                temp.eyes.Add(block.Key);
                                temp.haseyes = true;
                                //判定为有眼部子分支
                            }
                            if (block.Key.Substring(baselength, 1) == "L")
                            {
                                temp.mouths.Add(block.Key);
                                temp.hasmouths = true;
                                //同上
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
                //添加了判断，如立绘没有eyes子类，则跳过这一部分，避免报错
                if (data[body_index].haseyes)
                {
                    for (int i = 0; i < data[body_index].eyes.Count; i++)
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
                }

                //mouths
                listView_mouth.Clear();
                image_list = new ImageList();
                //添加了判断，同上
                if (data[body_index].hasmouths) {
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
                }


                //init
                eye_index = mouth_index = 0;
                listView_eye.Select();
                listView_mouth.Select();
                DrawBody(body_index);
                if (data[body_index].haseyes) DrawEye(body_index, eye_index);
                if (data[body_index].hasmouths) DrawMouth(body_index, mouth_index);
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

        public void saveAll()
        {
            if (body_index < 0 || body_index > data.Count) return;
            for (int i_body = 0; i_body < data.Count; i_body++)
            {
                DrawBody(i_body, true);
                if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\chara"))
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\chara");
                if (data[i_body].haseyes)
                {
                    if (data[i_body].hasmouths)
                    {
                        for (int i_mouths = 0; i_mouths < data[i_body].mouths.Count; i_mouths++)
                        {
                            for (int i_eyes = 0; i_eyes < data[i_body].mouths.Count; i_eyes++)
                            {
                                DrawEye(i_body, i_eyes);
                                DrawMouth(i_body, i_mouths);
                                pic.Save(Directory.GetCurrentDirectory() + "\\chara\\" + data[i_body].body + "E" + i_eyes.ToString() + "L" + i_mouths.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                    }
                    else
                    {
                        for (int i_eyes = 0; i_eyes < data[i_body].mouths.Count; i_eyes++)
                        {
                            DrawEye(i_body, i_eyes);
                            pic.Save(Directory.GetCurrentDirectory() + "\\chara\\" + data[i_body].body + "E" + i_eyes.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    //DrawEye(i_body, listView_eye.FocusedItem.Index); 
                }
                else
                {
                    if (data[i_body].hasmouths)
                    {
                        for (int i_mouths = 0; i_mouths < data[i_body].mouths.Count; i_mouths++)
                        {
                            DrawMouth(i_body, i_mouths);
                            pic.Save(Directory.GetCurrentDirectory() + "\\chara\\" + data[i_body].body + "L" + i_mouths.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    else
                    {
                        pic.Save(Directory.GetCurrentDirectory() + "\\chara\\" + data[i_body].body + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
            MessageBox.Show("Saved all!");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            saveAll();
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
            string tempFileName = data[body_index].body;
            if (data[body_index].haseyes) { 
                DrawEye(body_index, listView_eye.FocusedItem.Index);
                tempFileName += "E" + eye_index.ToString();
            }
            if (data[body_index].hasmouths) { 
                DrawMouth(body_index, listView_mouth.FocusedItem.Index);
                tempFileName += "L" + mouth_index.ToString();
            }
            if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\chara"))
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\chara");
            pic.Save(Directory.GetCurrentDirectory() + "\\chara\\" + tempFileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
            MessageBox.Show("Save success!");
        }              
    }
}

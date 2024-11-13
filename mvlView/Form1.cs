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
    /// <summary>窗口左上框中的一个Chara</summary>
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
        //JObject json;
        List<MvlSpirit> mvljson = new List<MvlSpirit>();
        List<Chara> data = new List<Chara>();
        string sourcepath;
        string targetpath;
        List<string> temppath = new List<string>();
        Bitmap pic;
        int body_index,eye_index,mouth_index;
        int body_x0, body_y0;
        string[] inargs;
        string workstatus = "mvl";

        /// <summary>默认启动 default boot</summary>
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>命令行启动 boot from command line</summary>
        /// <param name="args">mvl/json filepath</param>
        public Form1(string[] args)
        {
            this.inargs = args;
            InitializeComponent();

            for(int i = 0; i < inargs.Length; i++)
            {
                if (File.Exists(inargs[i]))
                {
                    data.Clear();
                    mvljson.Clear();
                    listBox1.Items.Clear();
                    if (Format.IsKaleidoJson(inargs[i]))
                    {
                        Kaleido thisKaleido = new Kaleido(inargs[i]);
                        sourcepath = thisKaleido.targetTempPath;
                        temppath.Add(thisKaleido.targetTempPath);
                        mvljson = thisKaleido.listofmvl;
                        OpenMVL_Kaleido(mvljson);
                        saveAll(false);
                    }
                    if (Path.GetExtension(inargs[i]).Equals(".json") && !Format.IsKaleidoJson(inargs[i])) {
                        sourcepath = Path.GetDirectoryName(inargs[i]) + "\\";
                        OpenJSON(inargs[i]);
                        saveAll(false);
                    }
                    if (Path.GetExtension(inargs[i]).Equals(".mvl") | Format.IsMvl(inargs[i]))
                    {
                        Mvl thisMvl = new Mvl(inargs[i]);
                        sourcepath = thisMvl.targetTempPath;
                        temppath.Add(thisMvl.targetTempPath);
                        mvljson = thisMvl.listofmvl;
                        OpenMVL(mvljson);
                        saveAll(false);
                    }
                }
            }
            foreach (var item in temppath)
            {
                DirectoryInfo di = new DirectoryInfo(item);
                di.Delete(true);
            }
            this.Close();
        }

        /// <summary>-o for open single file</summary>
        /// <param name="arg">mvl/json filepath</param>
        public Form1(string arg)
        {
            InitializeComponent();
            if(File.Exists(arg))
            {
                if (Format.IsKaleidoJson(arg))
                {
                    Kaleido thisKaleido = new Kaleido(arg);
                    sourcepath = thisKaleido.targetTempPath;
                    temppath.Add(thisKaleido.targetTempPath);
                    mvljson = thisKaleido.listofmvl;
                    OpenMVL_Kaleido(mvljson);
                }
                if (Path.GetExtension(arg).Equals(".json", StringComparison.OrdinalIgnoreCase) && !Format.IsKaleidoJson(arg))
                {
                    sourcepath = Path.GetDirectoryName(arg) + "\\";
                    OpenJSON(arg);
                }
                if (Path.GetExtension(arg).Equals(".mvl", StringComparison.OrdinalIgnoreCase) | Format.IsMvl(arg))
                {
                    Mvl thisMvl = new Mvl(arg);
                    sourcepath = thisMvl.targetTempPath;
                    temppath.Add(thisMvl.targetTempPath);
                    mvljson = thisMvl.listofmvl;
                    OpenMVL(mvljson);
                }
            }
        }

        /// <summary>程序菜单：打开JSON</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openJsonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Filter = "Original Json File|index.json|Json File|*.json|All File|*.*";
            file.ShowDialog();
            if (File.Exists(file.FileName))
            {
                data.Clear();
                mvljson.Clear();
                listBox1.Items.Clear();
                sourcepath = Path.GetDirectoryName(file.FileName) + "\\";
                targetpath = Path.GetDirectoryName(file.FileName) + "\\";
                OpenJSON(file.FileName);

            }
        }

        /// <summary>处理来自mvl的index.json文件</summary>
        /// <param name="filename">index.json filepath</param>
        public void OpenJSON(string filename)
        {
            /*StreamReader sr = new StreamReader(filename);
            JObject json = (JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
            sr.Close();

            int num = 0;
            List<string> names = new List<string>();
            foreach(var item in json)
            {
                num++;
                names.Add(item.Key);
            }
            for(int i = 0; i< num; i++)
            {
                MvlSpirit temp;
                temp.name = names[i];
                temp.max_x = (int)json[names[i]]["max_x"];
                temp.min_x = (int)json[names[i]]["min_x"];
                temp.max_y = (int)json[names[i]]["max_y"];
                temp.min_y = (int)json[names[i]]["min_y"];
                mvljson.Add(temp);
            }*/
            mvljson = Format.JsonToMvl(filename);
            OpenMVL(mvljson);


            //新增两个变量，用于定义body.png名称长度
            /*bool lengthdef = false;
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
            }*/

        }

        /// <summary>load pictures to listbox, workstatus as mvl</summary>
        /// <param name="mvllist">list of spirits, as index.json</param>
        public void OpenMVL(List<MvlSpirit> mvllist)
        {
            //新增两个变量，用于定义body.png名称长度
            int baselength = int.MaxValue;
            foreach (var item in mvllist) {
                baselength = baselength > item.name.Length ? item.name.Length : baselength;
            }
            
            foreach (var item in mvllist)
            {
                /*if (!lengthdef)
                {
                    baselength = item.name.Length;
                    lengthdef = true;
                }*/
                if (item.name.Length == baselength)
                {
                    Chara temp;
                    temp.body = item.name;
                    temp.eyes = new List<string>();
                    temp.mouths = new List<string>();
                    //初始化，假定立绘没有子分支
                    temp.haseyes = false;
                    temp.hasmouths = false;
                    foreach (var block in mvllist)
                    {
                        if (block.name.Length - 2 == baselength && block.name.Substring(0, baselength) == item.name)
                        {
                            if (block.name.Substring(baselength, 1) == "E")
                            {
                                temp.eyes.Add(block.name);
                                temp.haseyes = true;
                                //判定为有眼部子分支
                            }
                            if (block.name.Substring(baselength, 1) == "L")
                            {
                                temp.mouths.Add(block.name);
                                temp.hasmouths = true;
                                //同上
                            }

                        }
                    }
                    data.Add(temp);
                }
            }
            for (int i = 0; i < data.Count; i++)
            {
                listBox1.Items.Add(data[i].body);
            }
            workstatus = "mvl";
        }

        /// <summary>load pictures to listbox, workstatus as kaleido</summary>
        /// <param name="mvllist">list of spirits, 已从psb.m.json提取</param>
        public void OpenMVL_Kaleido(List<MvlSpirit> mvllist)
        {
            //新增两个变量，用于定义body.png名称长度
            bool lengthdef = false;
            int baselength = 0;
            foreach (var item in mvllist)
            {
                if (!lengthdef)
                {
                    baselength = item.name.Length;
                    lengthdef = true;
                }
                if (item.name.Length == baselength)
                {
                    Chara temp;
                    temp.body = item.name;
                    temp.eyes = new List<string>();
                    temp.mouths = new List<string>();
                    //初始化，假定立绘没有子分支
                    temp.haseyes = false;
                    temp.hasmouths = false;
                    foreach (var block in mvllist)
                    {
                        //此处需要修改eyediff判定
                        if (block.name.Length > baselength)
                        {
                            if (block.name.Substring(baselength + 1, 1) == "目")
                            {
                                temp.eyes.Add(block.name);
                                temp.haseyes = true;
                                //判定为有眼部子分支
                            }
                            if (block.name.Substring(baselength + 1, 1) == "口")
                            {
                                temp.mouths.Add(block.name);
                                temp.hasmouths = true;
                                //同上
                            }

                        }
                    }
                    data.Add(temp);
                }
            }
            for (int i = 0; i < data.Count; i++)
            {
                listBox1.Items.Add(data[i].body);
            }
            workstatus = "kaleido";
        }

        /// <summary>load spirit to window from listbox</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                        //Image temppiceyes = Bitmap.FromFile(sourcepath + data[body_index].eyes[i] + ".png");
                        image_list.Images.Add(Bitmap.FromFile(sourcepath + data[body_index].eyes[i] + ".png"));
                        image_list.ImageSize = new Size(75, 75);
                        //temppiceyes.Dispose();
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
                        //Image temppicmouth = Bitmap.FromFile(sourcepath + data[body_index].mouths[i] + ".png"); ;
                        image_list.Images.Add(Bitmap.FromFile(sourcepath + data[body_index].mouths[i] + ".png"));
                        image_list.ImageSize = new Size(75, 75);
                        //temppicmouth.Dispose();
                    }
                    listView_mouth.LargeImageList = image_list;
                    for (int i = 0; i < data[body_index].mouths.Count; i++)
                    {
                        ListViewItem item = new ListViewItem();
                        item.ImageIndex = i;
                        listView_mouth.Items.Add(item);
                    }
                    foreach(Image item in image_list.Images)
                    {
                        item.Dispose();
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
            Bitmap bmp = new Bitmap(sourcepath + data[body_index].body + ".png");
            label5.Text = bmp.Width.ToString() + "x" + bmp.Height.ToString();
            MvlSpirit tempsp = mvljson.Find(e => e.name.Equals(data[body_index].body));
            //body_x0 = (int)json[data[body_index].body]["min_x"];
            body_x0 = tempsp.min_x;
            //body_y0 = (int)json[data[body_index].body]["min_y"];
            body_y0 = tempsp.min_y;
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
            Bitmap bmp = new Bitmap(sourcepath + data[body_index].eyes[eye_index] + ".png");
            Graphics g = Graphics.FromImage(pic);
            MvlSpirit tempsp = mvljson.Find(e => e.name.Equals(data[body_index].eyes[eye_index]));
            //int x = (int)json[data[body_index].eyes[eye_index]]["min_x"] - body_x0;
            //int y = (int)json[data[body_index].eyes[eye_index]]["min_y"] - body_y0;
            int x = tempsp.min_x - body_x0;
            int y = tempsp.min_y - body_y0;
            g.DrawImage(bmp, x, y);
            bmp.Dispose();
            g.Dispose();
        }
        public void DrawMouth(int body_index, int mouth_index)
        {
            Bitmap bmp = new Bitmap(sourcepath + data[body_index].mouths[mouth_index] + ".png");
            Graphics g = Graphics.FromImage(pic);
            MvlSpirit tempsp = mvljson.Find(e => e.name.Equals(data[body_index].mouths[mouth_index]));
            //int x = (int)json[data[body_index].mouths[mouth_index]]["min_x"] - body_x0;
            //int y = (int)json[data[body_index].mouths[mouth_index]]["min_y"] - body_y0;
            int x = tempsp.min_x - body_x0;
            int y = tempsp.min_y - body_y0;
            g.DrawImage(bmp, x, y);
            bmp.Dispose();
            g.Dispose();
        }

        //Save all
        public void saveAll(bool CompletAnnouce)
        {
            if (body_index < 0 || body_index > data.Count) return;
            for (int i_body = 0; i_body < data.Count; i_body++)
            {
                DrawBody(i_body, true);
                string outputpath;
                if (checkBox1.Checked)
                {
                    if (!Directory.Exists(targetpath + "\\chara"))
                        Directory.CreateDirectory(targetpath + "\\chara");
                    outputpath = targetpath + "\\chara\\";
                }
                else
                {
                    if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\chara"))
                        Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\chara");
                    outputpath = Directory.GetCurrentDirectory() + "\\chara\\";
                }
                if (data[i_body].haseyes)
                {
                    if (data[i_body].hasmouths)
                    {
                        for (int i_mouths = 0; i_mouths < data[i_body].mouths.Count; i_mouths++)
                        {
                            for (int i_eyes = 0; i_eyes < data[i_body].eyes.Count; i_eyes++)
                            {
                                DrawEye(i_body, i_eyes);
                                DrawMouth(i_body, i_mouths);
                                pic.Save(outputpath + data[i_body].body + "E" + i_eyes.ToString() + "L" + i_mouths.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                            }
                        }
                    }
                    else
                    {
                        for (int i_eyes = 0; i_eyes < data[i_body].eyes.Count; i_eyes++)
                        {
                            DrawEye(i_body, i_eyes);
                            pic.Save(outputpath + data[i_body].body + "E" + i_eyes.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
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
                            pic.Save(outputpath + data[i_body].body + "L" + i_mouths.ToString() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    else
                    {
                        pic.Save(outputpath + data[i_body].body + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
            if (CompletAnnouce) MessageBox.Show("Saved all!");
        }

        //程序菜单：Exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //listView_eye.LargeImageList.Images.Clear();
            //listView_mouth.LargeImageList.Images.Clear();
            /*foreach (Image item in listView_mouth.LargeImageList.Images)
            {
                item.Dispose();
                //MessageBox.Show("itworks");
            }
            foreach (ListViewItem item in listView_mouth.Items)
            {
                item.Remove();
                //MessageBox.Show("itworks");
            }
            listView_mouth.Dispose();*/
            //pic.Dispose();
            
            foreach (var item in temppath)
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(item);
                    di.Delete(true);
                }
                catch(IOException ex) {
                    //throw ex;
                }
            }
            this.Close();
        }

        //程序菜单：Open mvl file Enter
        private void openMvlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Filter = "mvl File|*.mvl|All File|*.*";
            file.ShowDialog();
            if (File.Exists(file.FileName))
            {
                data.Clear();
                mvljson.Clear();
                listBox1.Items.Clear();
                //sourcepath = Path.GetDirectoryName(file.FileName) + "\\";
                Mvl thisMvl = new Mvl(file.FileName);
                sourcepath = thisMvl.targetTempPath;
                targetpath = Path.GetDirectoryName(file.FileName) + "\\";
                //OpenJSON(file.FileName);
                temppath.Add(thisMvl.targetTempPath);
                mvljson = thisMvl.listofmvl;
                OpenMVL(mvljson);
            }
        }

        //save all button click
        private void button2_Click(object sender, EventArgs e)
        {
            saveAll(true);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Developed by Wetor@github, Manicsteiner@github\rThanks support of ningshanwutuobang@github\rClick OK to open the github page.",
                "About", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/Manicsteiner/mvl_preview") { UseShellExecute = true});
            }
        }

        //程序菜单：Kaleido
        private void openJsonForKaleidoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Filter = "Original Json File|*.psb.m.json|Json File|*.json|All File|*.*";
            file.ShowDialog();
            if (File.Exists(file.FileName))
            {
                data.Clear();
                mvljson.Clear();
                listBox1.Items.Clear();
                Kaleido thisKaleido = new Kaleido(file.FileName);
                sourcepath = thisKaleido.targetTempPath;
                targetpath = Path.GetDirectoryName(file.FileName) + "\\";
                temppath.Add(thisKaleido.targetTempPath);
                mvljson = thisKaleido.listofmvl;
                OpenMVL_Kaleido(mvljson);
            }
        }

        //Drag Enter 拖放文件时判断文件格式
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)){
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    string extension = Path.GetExtension(files[0]);
                    if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Effect = DragDropEffects.Copy;
                    }
                    else if (extension.Equals(".mvl", StringComparison.OrdinalIgnoreCase) | Format.IsMvl(files[0]))
                    {
                        e.Effect = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                    }
                }
                else
                    e.Effect = DragDropEffects.None;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        //Drag Drop 拖放文件的处理
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1)
                {
                    string extension = Path.GetExtension(files[0]);
                    if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Format.IsKaleidoJson(files[0]))
                        {
                            data.Clear();
                            mvljson.Clear();
                            listBox1.Items.Clear();
                            Kaleido thisKaleido = new Kaleido(files[0]);
                            sourcepath = thisKaleido.targetTempPath;
                            targetpath = Path.GetDirectoryName(files[0]) + "\\";
                            temppath.Add(thisKaleido.targetTempPath);
                            mvljson = thisKaleido.listofmvl;
                            OpenMVL_Kaleido(mvljson);
                        }
                        else
                        {
                            data.Clear();
                            mvljson.Clear();
                            listBox1.Items.Clear();
                            sourcepath = Path.GetDirectoryName(files[0]) + "\\";
                            targetpath = Path.GetDirectoryName(files[0]) + "\\";
                            OpenJSON(files[0]);
                        }
                    }
                    else if (extension.Equals(".mvl", StringComparison.OrdinalIgnoreCase) | Format.IsMvl(files[0]))
                    {
                        data.Clear();
                        mvljson.Clear();
                        listBox1.Items.Clear();
                        Mvl thisMvl = new Mvl(files[0]);
                        sourcepath = thisMvl.targetTempPath;
                        targetpath = Path.GetDirectoryName(files[0]) + "\\";
                        temppath.Add(thisMvl.targetTempPath);
                        mvljson = thisMvl.listofmvl;
                        OpenMVL(mvljson);
                    }
                }
            }
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
        //save one
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
            if (checkBox1.Checked)
            {
                if (!Directory.Exists(targetpath + "\\chara"))
                    Directory.CreateDirectory(targetpath + "\\chara");
                pic.Save(targetpath + "\\chara\\" + tempFileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
            else
            {
                if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\chara"))
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\chara");
                pic.Save(Directory.GetCurrentDirectory() + "\\chara\\" + tempFileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
            MessageBox.Show("Save success!");
        }              
    }
}

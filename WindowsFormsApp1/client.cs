using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class client : Form
    {
        bool isDirty = false;
        public client(string host,string code,string name, string email, StreamWriter wt, StreamReader rd, TcpClient Client)
        {
            InitializeComponent();
            this.Width = 900;
            this.Height = 700;
            bm = new Bitmap(pic.Width, pic.Height);
            g = Graphics.FromImage(bm);
            g.Clear(Color.White);
            pic.Image = bm;
            this.panel4.Hide();
            this.panel5.Hide();
            eraser = new Pen(Color.White,10);
            tb_sizeeraser.Text = eraser.Width.ToString();
            tb_pensize.Text = p.Width.ToString();
            p = new Pen(Color.Black,1);
            this.Resize += new EventHandler(Form1_Resize);
            this.host = host;
            this.code = code;
            this.email = email;
            this.name = name;
            tb_host.Text = host;
            tb_id.Text = code;
            writer = wt;
            reader = rd;
            //this.Load += client_Load; 
            this.tcpClient = Client;
            this.Text = $"User: {name}";
            this.panel_imageurl.Visible = false;
    }
        TcpClient tcpClient;
        Bitmap bm;
        Graphics g;
        bool paint = false;
        Point px, py;
        Pen p = new Pen (Color.Black, 1);
        Pen eraser = new Pen(Color.White, 10);
        int index;
        int x,y,sX,sY,cX,cY;
        string host, code,name ,email;
        ColorDialog cd = new ColorDialog();
        Color new_color;
        Image image;
        
        
        private string currentFilePath = null;

        StreamWriter writer;
        StreamReader reader;

        Thread listenThread;
        private bool isListening = true;

        private void pic_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
           
            if (paint)
            {
                if (index == 3)
                {
                    g.DrawEllipse(p, cX, cY, sX, sY);
                }
                if (index == 4)
                {
                    g.DrawRectangle(p, cX, cY, sX, sY);
                }
                if (index == 5)
                {
                    g.DrawLine(p, cX, cY, x, y);
                }
            }
            
            
        }
        private void colorPicker_MouseClick(object sender, MouseEventArgs e)
        {
            Point point = set_point(colorPicker, e.Location);
            btn_piccolor.BackColor = ((Bitmap)colorPicker.Image).GetPixel(point.X, point.Y);
            new_color = btn_piccolor.BackColor;
            p.Color = btn_piccolor.BackColor;
            
        }

        private async void ListenForMessages()
        {
            try
            {
                while (isListening)
                {
                    await readSemaphore.WaitAsync();
                    try
                    {
                        string message = await reader.ReadLineAsync();
                        

                        if (message != null)
                        {
                            if (this.IsHandleCreated)
                            {
                                
                                this.Invoke(new Action(() => ProcessServerMessage(message)));
                            }
                            else
                            {
                                // Handle case when handle is not created
                            }
                        }
                    }
                    finally
                    {
                        readSemaphore.Release();
                    }
                }
            }
            catch 
            {
            }
        }


        private void ProcessServerMessage(string message)
        {
            string[] parts = message.Split('|');
            string command = message.Substring(0,message.IndexOf('|'));
            string data = message.Substring(message.IndexOf("|") + 1);
   
            switch (command)
            {
                case "participants":
                    if (parts.Length > 1)
                    {
                        UpdateParticipantsListBox(parts[1].Split(','));
                    }
                    break;
                case "draw":
                    if (parts.Length > 1)
                    {
                        
                        ExecuteDrawCommand(data);
                    }
                    
                    break;
                
                default:
                    break;
            }
        }
        private async void ExecuteDrawCommand(string command)
        {
            string[] parts = command.Split('|');
            
            if (parts.Length < 2)
            {
                return;
            }
            string shapeType = parts[0];
            string[] parameters = parts[1].Split(',');
            if (shapeType == "mousedown")
            {
                
                //paint = true;
                int x1 = int.Parse(parameters[0]);
                int y1 = int.Parse(parameters[1]);
                px = new Point(x1, y1);
                
            }
            else if (shapeType == "line")
            {
                int x1 = int.Parse(parameters[0]);
                int y1 = int.Parse(parameters[1]);
                py = new Point(x1, y1);
                Color color = Color.FromArgb(int.Parse(parameters[2]));
                float width = float.Parse(parameters[3]);
                p = new Pen(color,width);
                
                g.DrawLine(p, px, py);
                px = py;
                pic.Invalidate();

            }
            else if( shapeType == "eraser")
            {
                int x1 = int.Parse(parameters[0]);
                int y1 = int.Parse(parameters[1]);
                py = new Point(x1, y1);
                Color color = Color.FromArgb(int.Parse(parameters[2]));
                float width = float.Parse(parameters[3]);
                eraser = new Pen(Color.White, width);

                g.DrawLine(eraser, px, py);
                px = py;
                pic.Invalidate();
            }
            else if (shapeType == "ellipse")
            {
                int cX1 = int.Parse(parameters[0]);
                int cY1 = int.Parse(parameters[1]);
                int sX1 = int.Parse(parameters[2]);
                int sY1 = int.Parse(parameters[3]);
                Color color = Color.FromArgb(int.Parse(parameters[4]));
                float widthline = float.Parse(parameters[5]);
                p = new Pen(color,widthline);
                g.DrawEllipse(p, cX1,cY1,sX1,sY1);
                
                pic.Invalidate();
            }
            else if (shapeType == "rectangle")
            {
                int cX1 = int.Parse(parameters[0]);
                int cY1 = int.Parse(parameters[1]);
                int sX1 = int.Parse(parameters[2]);
                int sY1 = int.Parse(parameters[3]);
                Color color = Color.FromArgb(int.Parse(parameters[4]));
                float widthline = float.Parse(parameters[5]);
                p = new Pen(color, widthline);
                g.DrawRectangle(p, cX1, cY1, sX1, sY1);
                
                pic.Invalidate();
            }
            else if(shapeType == "straight")
            {
                int cX1 = int.Parse(parameters[0]);
                int cY1 = int.Parse(parameters[1]);
                int x1 = int.Parse(parameters[2]);
                int y1 = int.Parse(parameters[3]);
                Color color = Color.FromArgb(int.Parse(parameters[4]));
                float widthline = float.Parse(parameters[5]);
                p = new Pen(color, widthline);
                g.DrawLine(p, cX1, cY1, x1, y1);
                
                pic.Invalidate();
            }
            else if (shapeType == "fill")
            {
                int x = int.Parse(parameters[0]);
                int y = int.Parse(parameters[1]);
                Color color = Color.FromArgb(int.Parse(parameters[2]));
                Fill(bm, x, y, color);
                pic.Invalidate();
            }
            else if (shapeType == "image")
            {
                string imagebase64 = parts[1];
                byte[] bytes = Convert.FromBase64String(imagebase64);
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    image = Image.FromStream(ms);
                    InsertImage(image);
                }
            }
            else if(shapeType == "re_image")
            {
                int w = int.Parse(parameters[0]);
                int h = int.Parse(parameters[1]);
                Size newsize = new Size(w, h);
                Image resizedImage = ResizeImageInternal(image, newsize);
                InsertImage(resizedImage);
                image = resizedImage;
            }
            else if(shapeType == "cleared")
            {
                string clearper = parts[1];
                DialogResult rs = MessageBox.Show($"Another participants({clearper}) has clear the screen.Do you want to save this picture before clear it", "Nontification", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (rs == DialogResult.Yes)
                {
                    savefile();
                    clear();
                }
                else
                clear();
            }
        }
        private void InsertImage(Image image)
        {
            if(bm == null)
            {
                bm = new Bitmap(pic.Width, pic.Height);
            }
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(image,new Rectangle(50,50,image.Width,image.Height));
            }
            pic.Image = bm;
            pic.Invalidate();
        }

        private string ImageBase64(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                byte[] bytes = ms.ToArray();
                return Convert.ToBase64String(bytes);
            }
        }
        
        

        private void UpdateParticipantsListBox(string[] participants)
        {

            if (rtb_participants.InvokeRequired)
            {
                rtb_participants.Invoke(new Action(() => UpdateParticipantsListBox(participants)));
            }
            else
            {
                rtb_participants.Clear();
                foreach (var participant in participants)
                {
                    
                    rtb_participants.AppendText(participant+"\n");
                    
                }
                label4.Text = $"Participants: {participants.Length}";
            }
        }
        
        private async void ResizeImage(string data)
        {
            string[] parts = data.Split(',');
            if (parts.Length < 2)
            {
                // Invalid command format
                return;
            }
            string url = parts[0];
            int width = int.Parse(parts[1]);
            int height = int.Parse(parts[2]);
            using (Graphics g = Graphics.FromImage(bm))
            {
                using (Brush brush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(brush, new Rectangle(0, 0, pic.Width, pic.Height));
                }
            }
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] imageBytes = await client.GetByteArrayAsync(url);
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        using (Image image = Image.FromStream(ms))
                        {
                            Size newSize = new Size(width, height);
                            Image resizedImage = ResizeImageInternal(image, newSize);
                            g.DrawImage(resizedImage,0,0);
                            pic.Image = bm;
                            tb_imageurl.Text = url;
                            tb_width.Text = newSize.Width.ToString(); tb_height.Text = newSize.Height.ToString();
                            pic.Refresh();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while inserting the image: {ex.Message}");
            }
        }
        
        private Size GetResizedImageSize(Size originalSize, Size maxSize)
        {
            double widthRatio = (double)maxSize.Width / originalSize.Width;
            double heightRatio = (double)maxSize.Height / originalSize.Height;
            double ratio = Math.Min(widthRatio, heightRatio);

            int newWidth = (int)(originalSize.Width * ratio)/2;
            int newHeight = (int)(originalSize.Height * ratio)/2;

            return new Size(newWidth, newHeight);
        }
        

        private Image ResizeImageInternal(Image image, Size newSize)
        {
            Bitmap result = new Bitmap(newSize.Width, newSize.Height);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(image, new Rectangle(0, 0, newSize.Width, newSize.Height));
            }

            return result;
        }

        private void pic_MouseDown(object sender, MouseEventArgs e)
        {
            paint = true;
            py = e.Location;
            cX = e.X; 
            cY = e.Y;
            string command = $"draw|{code}|mousedown|{cX},{cY}";
            SendMessageToServer(command);
        }

        private void pic_MouseClick(object sender, MouseEventArgs e)
        {
            Point point = set_point(pic, e.Location);
            if (index == 7)
            {
                
                Fill(bm, point.X,point.Y,new_color);
                string command = $"draw|{code}|fill|{point.X},{point.Y},{new_color.ToArgb()}";
                SendMessageToServer(command);
            }
            isDirty = true;
        }

        private void pic_MouseMove(object sender, MouseEventArgs e)
        {
            if (paint)
            {
                
               
                if (index == 1)
                {
                    px = e.Location;
                    g.DrawLine(p, px, py);
                    py = px;
                    string command = $"draw|{code}|line|{py.X},{py.Y},{p.Color.ToArgb()},{p.Width}";
                    SendMessageToServer(command);
                    
                }
                if (index == 2)
                {
                    px = e.Location;
                    g.DrawLine(eraser, px, py);
                    py = px;
                    string command = $"draw|{code}|eraser|{py.X},{py.Y},{eraser.Color.ToArgb()},{eraser.Width}";
                    SendMessageToServer(command);
                    
                }
                pic.Refresh();
                x = e.X;
                y = e.Y;
                sX = x - cX;
                sY = y-cY;
            }
        }

        private void pic_MouseUp(object sender, MouseEventArgs e)
        {
            paint = false;
            x = e.Location.X;
            y = e.Location.Y;
            sX = x - cX;
            sY = y - cY;

            if (index == 3) // Ellipse
            {
                g.DrawEllipse(p, cX, cY, sX, sY);
                string command = $"draw|{code}|ellipse|{cX},{cY},{sX},{sY},{p.Color.ToArgb()},{p.Width}";
                SendMessageToServer(command);
                
            }
            else if (index == 4) // Rectangle
            {
                g.DrawRectangle(p, cX, cY, sX, sY);
                string command = $"draw|{code}|rectangle|{cX},{cY},{sX},{sY},{p.Color.ToArgb()},{p.Width}";
                SendMessageToServer(command);
            }
            else if (index == 5) // Line
            {
                g.DrawLine(p, cX, cY, x, y);
                string command = $"draw|{code}|straight|{cX},{cY},{x},{y},{p.Color.ToArgb()},{p.Width}";
                SendMessageToServer(command);
            }
        }
        string status = "";
        private void savefile()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Image Files (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All Files (*.*)|*.*";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = sfd.FileName;
                }
                else
                {
                    return; // Người dùng đã hủy hộp thoại lưu, không làm gì cả
                }
            }

            try
            {
                Bitmap btm = bm.Clone(new Rectangle(0, 0, pic.Width, pic.Height), bm.PixelFormat);
                btm.Save(currentFilePath, ImageFormat.Jpeg);
                MessageBox.Show("Image Saved Successfully!!");
                isDirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private void openfile()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;*.gif";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                currentFilePath = ofd.FileName;
                Bitmap loadedImage = new Bitmap(ofd.FileName);
                bm = new Bitmap(loadedImage, pic.Width, pic.Height);
                g = Graphics.FromImage(bm);
                pic.Image = bm;
                isDirty = false; // Đặt lại cờ khi mở tệp
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            panel4.Hide();
        }
        private void btn_new_Click(object sender, EventArgs e)
        {
            if (isDirty)
            {
                DialogResult rs = MessageBox.Show("Do you want to save before creating a new drawing?", "Attention", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (rs == DialogResult.Yes)
                {
                    savefile();
                    if (status != "cancel")
                    {
                        g.Clear(Color.White);
                        pic.Image = bm;
                        index = 0;
                        isDirty = false; // Đặt lại cờ khi tạo mới
                        status = "";
                        currentFilePath = null; // Đặt lại đường dẫn tệp
                    }
                }
                else
                {
                    g.Clear(Color.White);
                    pic.Image = bm;
                    index = 0;
                    isDirty = false; // Đặt lại cờ khi không có gì để lưu
                    currentFilePath = null; // Đặt lại đường dẫn tệp
                }
            }
            else
            {
                g.Clear(Color.White);
                pic.Image = bm;
                index = 0;
                isDirty = false; // Đặt lại cờ khi không có gì để lưu
                currentFilePath = null; // Đặt lại đường dẫn tệp
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isDirty)
            {
                DialogResult rs = MessageBox.Show("Do you want to save before closing?", "Attention", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (rs == DialogResult.Yes)
                {
                    savefile();
                    if (status == "cancel")
                    {
                        e.Cancel = true; // Hủy đóng form nếu người dùng hủy lưu
                    }
                }
                else if (rs == DialogResult.Cancel)
                {
                    e.Cancel = true; // Hủy đóng form nếu người dùng chọn hủy
                }
            }
            try
            {
                writer.WriteLine($"close|{code}|{name}");
                writer.Flush();
                reader.Close();
                writer.Close();
                tcpClient.Close();
            }
            catch(Exception ex) { 
                MessageBox.Show($"Error closing the connection: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void pic_more_Click(object sender, EventArgs e)
        {
            panel4.Show();
        }
        private void pic_save_Click_1(object sender, EventArgs e)
        {
            savefile();
        }
        private void pic_clear_Click(object sender, EventArgs e)
        {
            string command = $"clear|{code}|{name}";
            if (isDirty)
            {
                DialogResult result = MessageBox.Show("Do you want to save this picture before clear it?", "Attention!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    savefile();
                    clear();
                    SendMessageToServer(command);
                }
                else if (result == DialogResult.No) 
                {
                    
                    clear();
                    SendMessageToServer(command);
                }
                else
                return;
            }
            else return;
        }
        private void clear()
        {
            g.Clear(Color.White);
            pic.Image = bm;
            pic.Invalidate();
        }
        private void pic_color_Click(object sender, EventArgs e)
        {
            cd.ShowDialog();
            new_color = cd.Color;
            btn_piccolor.BackColor = cd.Color;
            p.Color = cd.Color;
            
        }
        private void pic_fill_Click(object sender, EventArgs e)
        {
            index = 7;
        }
        private void pic_pencil_Click(object sender, EventArgs e)
        {
            index = 1;
        }
        private void pic_eraser_Click(object sender, EventArgs e)
        {
            index = 2;
        }
        private void pic_ellipse_Click(object sender, EventArgs e)
        {
            index = 3;
        }
        private void pic_rec_Click(object sender, EventArgs e)
        {
            index = 4;
        }
        private void pic_line_Click(object sender, EventArgs e)
        {
            index = 5;

        }
        private void btn_eraserup_Click(object sender, EventArgs e)
        {
            eraser.Width += 5; 
            if (eraser.Width > 50) 
            {
                eraser.Width = 50;
            }
            tb_sizeeraser.Text = eraser.Width.ToString();
        }
        private void btn_eraserdown_Click(object sender, EventArgs e)
        {
            eraser.Width -= 5; 
            if (eraser.Width < 5) 
            {
                eraser.Width = 5;
            }
            tb_sizeeraser.Text = eraser.Width.ToString();
        }
        private void btn_penup_Click(object sender, EventArgs e)
        {
            p.Width += 2; 
            if (p.Width > 20) 
            {
                p.Width = 20;
            }
            tb_pensize.Text = p.Width.ToString();
        }
        private void btn_pendown_Click(object sender, EventArgs e)
        {
            p.Width -= 2; 
            if (p.Width < 1) 
            {
                p.Width = 1;
            }
            tb_pensize.Text = p.Width.ToString();
        }
        private void btn_open_Click(object sender, EventArgs e)
        {
            openfile();
            this.panel4.Hide();
        }
        private void btn_participants_Click(object sender, EventArgs e)
        {
            
            this.panel5.Show();
        }


        private readonly SemaphoreSlim readSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim writeSemaphore = new SemaphoreSlim(1, 1);

        private void btn_image_Click(object sender, EventArgs e)
        {
            
            this.panel_imageurl.Visible = !this.panel_imageurl.Visible;
        }

        private void tb_width_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void tb_height_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void btn_enter_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(tb_width.Text)&& !string.IsNullOrEmpty(tb_height.Text))
            {
                int width = int.Parse(tb_width.Text);
                int height = int.Parse(tb_height.Text);
                Size newsize = new Size(width, height); 
                Image resizedImage = ResizeImageInternal(image, newsize);
                string drawCommand = $"resize|{code}|re_image|{width},{height}";
                SendMessageToServer(drawCommand);
                InsertImage(resizedImage);
                image = resizedImage; 
                
            }
        }

        private async void btn_insertimage_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(tb_imageurl.Text))
            {
                try
                {
                    using (HttpClient Client = new HttpClient())
                    {
                        byte[] imageBytes = await Client.GetByteArrayAsync(tb_imageurl.Text.Trim());
                        using (MemoryStream ms = new MemoryStream(imageBytes))
                        {
                            image = Image.FromStream(ms);
                            Size newSize = new Size(image.Width, image.Height);
                            Image resizedImage = ResizeImageInternal(image, newSize);
                            tb_width.Text = image.Size.Width.ToString();
                            tb_height.Text = image.Size.Height.ToString();
                            string imagebase64 = ImageBase64(resizedImage);
                            string drawCommand = $"insert|{code}|image|{imagebase64}";
                            SendMessageToServer(drawCommand);
                            InsertImage(image);
                        }
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void client_Load_1(object sender, EventArgs e)
        {
            await Task.Run(() => ListenForMessages());
        }

        
        private void btn_closeparti_Click(object sender, EventArgs e)
        {
            this.panel5.Hide();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            pic.Size = new Size(this.ClientSize.Width, this.ClientSize.Height);

            // Create a new Bitmap with the updated size
            Bitmap newBm = new Bitmap(pic.Width, pic.Height);
            Graphics newG = Graphics.FromImage(newBm);

            // Clear the new bitmap and draw the existing bitmap onto it
            newG.Clear(Color.White);
            if (bm != null)
            {
                newG.DrawImage(bm, 0, 0);
            }

            // Update the references
            bm = newBm;
            g = newG;
            pic.Image = bm;
            
        }
        private async void SendMessageToServer(string message)
        {
            await writeSemaphore.WaitAsync();
            try
            {
                await writer.WriteLineAsync(message);
            }
            finally
            {
                writeSemaphore.Release();
            }
        }
        

        
        static Point set_point(PictureBox pb, Point pt)
        {
            float pX = 1f*pb.Image.Width/pb.Width;
            float pY = 1f*pb.Image.Height/pb.Height;
            return new Point((int)(pt.X * pX),(int)(pt.Y * pY));
        }

        private void validate(Bitmap bm, Stack<Point> sp, int x, int y , Color old_color, Color new_color)
        {
            Color cx = bm.GetPixel(x, y);
            if(cx == old_color) 
            {
                sp.Push(new Point(x, y));
                bm.SetPixel(x, y, new_color);
            }
        }

        public void Fill(Bitmap bm, int x,int y, Color new_color)
        {
            Color old_color = bm.GetPixel(x, y);
            Stack<Point> pixel = new Stack<Point>();
            pixel.Push(new Point(x,y));
            bm.SetPixel(x, y, new_color);
            if (old_color == new_color)
                return;
            while(pixel.Count > 0)
            {
                Point pt = (Point)pixel.Pop();
                if(pt.X>0 && pt.Y>0 && pt.X<bm.Width-1 && pt.Y < bm.Height - 1)
                {
                    validate(bm,pixel,pt.X-1,pt.Y, old_color,new_color);
                    validate(bm, pixel, pt.X, pt.Y-1, old_color, new_color);
                    validate(bm, pixel, pt.X + 1, pt.Y, old_color, new_color);
                    validate(bm, pixel, pt.X, pt.Y+1, old_color, new_color);
                }
            }
     
        }
    }
}

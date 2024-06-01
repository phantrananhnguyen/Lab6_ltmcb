using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Mail;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace WindowsFormsApp1
{
    public partial class starting : Form
    {
        public starting()
        {
            InitializeComponent();
            connect();
            if (client.Connected)
            {
                reader = new StreamReader(client.GetStream());
                writer = new StreamWriter(client.GetStream());
                writer.AutoFlush = true;
                this.OTP = GenerateOTP();
            }
            else return;
        }
        
        StreamWriter writer;
        StreamReader reader;
        string code;
        private TcpClient client;
        string host;
        string name;
        string email;
        private void btn_create_Click(object sender, EventArgs e)
        {
            if (client.Connected)
            {
                if (!string.IsNullOrEmpty(tb_name.Text)&&!string.IsNullOrEmpty(tb_email.Text)&& !string.IsNullOrEmpty(tb_otp.Text))
                {
                    if (tb_otp.Text.Trim() == OTP)
                    {
                        Thread newroom = new Thread(create);
                        newroom.Start();
                        newroom.IsBackground = true;
                    }
                    else MessageBox.Show("The OTP is incorrect");
                }
                else
                {
                    MessageBox.Show("Pleased provide suitable information");

                }
            }
        }
        private void connect()
        {
            client = new TcpClient();
            try
            {
                client.Connect(IPAddress.Parse("127.0.0.1"), 8080);
                reader = new StreamReader(client.GetStream());
                writer = new StreamWriter(client.GetStream());
                writer.AutoFlush = true;
            }
            catch
            {
                MessageBox.Show("server is not active");
                
                return;
            }
            
        }
       
        private void create()
        {
            host =name= tb_name.Text.Trim();
            writer.WriteLine("create");
            code = generatecode();
            email = tb_email.Text.Trim();   
            writer.WriteLine($"{host}|{code}|{email}") ;
            string response = reader.ReadLine();
            if(response == "success")
            {
                Invoke(new Action(() =>
                {
                    client form = new client(host, code,name,email, writer,reader, client);
                    MessageBox.Show("Created new room successfully");
                    form.Show();
                    this.Hide();
                }
                ));
                
                
               
            }
            else if(response =="fail")
            {
                MessageBox.Show("An Error occur! Please try again");
                return;
            }
        }
        private void join()
        {
            name = tb_name.Text.Trim();
            code = tb_idroom.Text.Trim();
            writer.WriteLine("join");
            writer.WriteLine($"{name}|{code}");
            string response = reader.ReadLine();
            if (response == "Full")
            {
                MessageBox.Show("The number of participants has reached its limit");
                return;
            }
                string status = response.Substring(0, response.IndexOf("|"));
            string host = response.Substring(response.IndexOf("|") + 1);
            if(status == "success") 
            {
                Invoke(new Action(() =>
                {
                    client form = new client(host, code,name,email, writer, reader,client);
                    MessageBox.Show("Join room successfully");
                    form.Show();
                    this.Hide();
                }
                ));
            }
            
            else if(status =="fail")
            {
                MessageBox.Show("An Error occur! Please try again");
                return;
            }
        }
        private string generatecode()
        {
            Random random = new Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] stringchars = new char[8];
            for(int i = 0; i < stringchars.Length; i++)
            {
                stringchars[i] = chars[random.Next(chars.Length)];
            }
            return new string(stringchars);
        }

        private void btn_join_Click(object sender, EventArgs e)
        {
            if (client.Connected)
            {
                if (!string.IsNullOrEmpty(tb_name.Text))
                {
                    if (!string.IsNullOrEmpty(tb_idroom.Text))
                    {
                        Thread joinroom = new Thread(join);
                        joinroom.Start();
                        joinroom.IsBackground = true;
                    }
                    else MessageBox.Show("Please enter the invite code");
                }
                else
                {
                    MessageBox.Show("Pleased enter a name");
                }
            }
        }
        string OTP;
        private void btn_sendotp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("The OTP is being sent to you email");
            new Thread(() => SendEmail(tb_email.Text.Trim(), OTP)).Start();
        }
        private void SendEmail(string toEmail, string code)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("22520985@gm.uit.edu.vn", "Painting Application of team N_T");
                mail.To.Add(toEmail);
                mail.Subject = "OTP";
                mail.Body = $"Hello,\n\nYour OTP is: {code}\n\nWe will provide you with a room ID after the room is created successfully.\nShare this room ID with your friends in order to invite them to join your painting room";
                SmtpServer.Port = 587;
                SmtpServer.Credentials = new NetworkCredential("22520985@gm.uit.edu.vn", "1991419869");
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                
            }
            catch (Exception ex)
            {
                
                    MessageBox.Show($"Failed to send email: {ex.Message}\r\n");
                
            }
        }
        private string GenerateOTP()
        {
            const string chars = "0123456789";
            var random = new Random();
            var code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            return code;
        }
    }
}

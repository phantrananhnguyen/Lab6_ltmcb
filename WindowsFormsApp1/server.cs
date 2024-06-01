using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Net.Mail;
namespace WindowsFormsApp1
{
    public partial class server : Form
    {
        public server()
        {
            InitializeComponent();
        }

        private TcpListener tcpListener;
        private bool isRunning;
        private Dictionary<string, roominfor> roomlist;
        private Dictionary<string, List<string>> participants;
        private Dictionary<string, TcpClient> clientConnections;
        private Dictionary<string,string> mailofahost;
        private Dictionary<string, Queue<string>> drawingCommands;

        public class roominfor
        {
            public string host { get; set; }
            public string code { get; set; }
           
            public roominfor(string code, string host)
            {
                this.host = host;
                this.code = code;
                
            }
        }

        private void server_Load(object sender, EventArgs e)
        {
            roomlist = new Dictionary<string, roominfor>();
            participants = new Dictionary<string, List<string>>();
            clientConnections = new Dictionary<string, TcpClient>();
            mailofahost = new Dictionary<string, string>(); 
            drawingCommands = new Dictionary<string, Queue<string>>();
        }

        private void btn_listen_Click(object sender, EventArgs e)
        {
            if (isRunning)
            {
                StopServer();
            }
            else
            {
                StartServer();
            }
        }

        private void StartServer()
        {
            isRunning = true;
            Thread listenThread = new Thread(Listen);
            listenThread.Start();
            listenThread.IsBackground = true;

            rtb_content.AppendText($"{DateTime.Now} : Server is running\r\n");
            btn_listen.Text = "Stop";
        }

        private void StopServer()
        {
            isRunning = false;
            tcpListener.Stop();

            lock (clientConnections)
            {
                foreach (var client in clientConnections.Values)
                {
                    client.Close();
                }
                clientConnections.Clear();
            }

            rtb_content.AppendText($"{DateTime.Now} : Server stopped\r\n");
            btn_listen.Text = "Listen";
        }

        private void Listen()
        {
            tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, 8080));
            tcpListener.Start();

            while (isRunning)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClientComm(client));
                    clientThread.Start();
                    clientThread.IsBackground = true;
                }
                catch (SocketException)
                {
                    if (isRunning)
                    {
                        Invoke(new Action(() =>
                        {
                            rtb_content.AppendText("Socket exception occurred while listening for clients.\r\n");
                        }));
                    }
                }
            }
        }

        private void HandleClientComm(TcpClient tcpClient)
        {
            try
            {
                StreamReader reader = new StreamReader(tcpClient.GetStream());
                StreamWriter writer = new StreamWriter(tcpClient.GetStream()) { AutoFlush = true };

                while (tcpClient.Connected)
                {
                    string request = reader.ReadLine();
                    if (request == null)
                    {
                        break;
                    }

                    if (request == "create")
                    {
                        string infor = reader.ReadLine();

                        Invoke(new Action(() =>
                        {
                            rtb_content.AppendText($"{DateTime.Now} - Received create request: {infor}\r\n");
                        }));

                        string host = infor.Substring(0, infor.IndexOf('|'));
                        string infor1 = infor.Substring(infor.IndexOf('|') + 1);
                        string code = infor1.Substring(0, infor1.IndexOf('|'));
                        string email = infor1.Substring(infor1.IndexOf('|') + 1);
                        mailofahost[code] = email;
                        if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(host))
                        {
                            lock (roomlist)
                            {
                                roominfor room = new roominfor(code, host);
                                if (!roomlist.ContainsKey(code))
                                {
                                    roomlist.Add(code, room);
                                    participants[code] = new List<string> { host };
                                    clientConnections[host] = tcpClient;
                                    writer.WriteLine("success");
                                    SendParticipantsList(code);
                                }
                                else
                                {
                                    writer.WriteLine("fail");
                                }
                            }
                        }
                        else
                        {
                            writer.WriteLine("fail");
                        }
                    }
                    else if (request == "join")
                    {
                        string infor = reader.ReadLine();
                        string name = infor.Substring(0, infor.IndexOf("|"));
                        string code = infor.Substring(infor.IndexOf("|") + 1);
                        if (participants[code].Count == 5)
                        {
                            writer.WriteLine("Full");

                        }
                        else if (participants[code].Count <= 5)
                        {
                            Invoke(new Action(() =>
                            {
                                rtb_content.AppendText($"{DateTime.Now} - Received join request for room: {infor}\r\n");
                            }));

                            lock (roomlist)
                            {
                                if (roomlist.ContainsKey(code))
                                {
                                    participants[code].Add(name);
                                    clientConnections[name] = tcpClient;
                                    writer.WriteLine($"success|{roomlist[code].host}");
                                    SendParticipantsList(code);
                                    if (drawingCommands.ContainsKey(code))
                                    {
                                        foreach (string command in drawingCommands[code])
                                        {
                                            writer.WriteLine($"draw|{command}");
                                        }
                                    }
                                }
                                else
                                {
                                    writer.WriteLine("fail");
                                }
                            }
                        }
                    }
                    else if (request.StartsWith("draw|"))
                    {
                        string rq = request.Substring(request.IndexOf("|") + 1);
                        string[] parts = request.Split('|');


                        if (parts.Length > 2)
                        {
                            string code = rq.Substring(0, rq.IndexOf('|'));
                            string drawCommand = rq.Substring(rq.IndexOf('|') + 1);

                            lock (participants)
                            {
                                if (participants.ContainsKey(code))
                                {
                                    if (!drawingCommands.ContainsKey(code))
                                    {
                                        drawingCommands[code] = new Queue<string>();
                                    }
                                    drawingCommands[code].Enqueue(drawCommand);
                                    BroadcastDrawCommand(code, drawCommand);
                                }
                            }
                        }
                    }
                    else if (request.StartsWith("insert|"))
                    {
                        string infor = request.Substring(request.IndexOf("|") + 1);
                        string code = infor.Substring(0, infor.IndexOf('|'));
                        string drawCommand = infor.Substring(infor.IndexOf('|') + 1);
                        

                        lock (participants)
                        {
                            if (participants.ContainsKey(code))
                            {
                                if (!drawingCommands.ContainsKey(code))
                                {
                                    drawingCommands[code] = new Queue<string>();
                                }
                                drawingCommands[code].Enqueue(drawCommand);
                                BroadcastDrawCommand(code, drawCommand);
                            }
                        }
                    }
                    else if (request.StartsWith("resize|"))
                    {
                        string infor = request.Substring(request.IndexOf("|") + 1);
                        string code = infor.Substring(0, infor.IndexOf('|'));
                        string drawCommand = infor.Substring(infor.IndexOf('|') + 1);
                        Invoke(new Action(() =>
                        {
                            rtb_content.AppendText($"{DateTime.Now} - Received image resize request for room {code}: {drawCommand}\r\n");
                        }));
                        lock (participants)
                        {
                            if (participants.ContainsKey(code))
                            {
                                if (!drawingCommands.ContainsKey(code))
                                {
                                    drawingCommands[code] = new Queue<string>();
                                }
                                drawingCommands[code].Enqueue(drawCommand);
                                BroadcastDrawCommand(code, drawCommand);
                            }
                        }
                    }
                    else if (request.StartsWith("close|"))
                    {

                        try
                        {
                            string[] part = request.Split('|');
                            string code = part[1];
                            string name = part[2];

                            tcpClient.Close();

                            lock (participants)
                            {
                                if (participants.ContainsKey(code) && participants[code].Contains(name))
                                {
                                    participants[code].Remove(name);

                                    if (participants[code].Count == 0)
                                    {
                                        lock (drawingCommands)
                                        {
                                            drawingCommands.Remove(code);
                                        }
                                    }
                                }
                            }

                            SendParticipantsList(code);
                        }
                        catch (Exception ex)
                        {
                            // Log the exception or handle it appropriately
                            Console.WriteLine($"An error occurred: {ex.Message}");
                        }
                    }
                    else if (request.StartsWith("clear|"))
                    {
                        string[] part = request.Split('|');
                        string code = part[1];
                        string name = (string)part[2];
                        drawingCommands.Clear();
                        foreach (var participant in participants[code]) 
                        {
                            if (participant != name)
                            {
                                string command = $"cleared|{name}";
                                SendMessageToParticipant(participant, $"draw|{command}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    rtb_content.AppendText($"Exception: {ex.Message}\r\n");
                }));
            }
            finally
            {
                lock (clientConnections)
                {
                    var client = clientConnections.FirstOrDefault(x => x.Value == tcpClient).Key;
                    if (client != null)
                    {
                        clientConnections.Remove(client);
                        foreach (var room in participants.Values)
                        {
                            room.Remove(client);
                        }
                    }

                    tcpClient.Close();
                }
            }
        }

        private void BroadcastDrawCommand(string roomCode, string command)
        {
        
            foreach (var participant in participants[roomCode])
            {
                SendMessageToParticipant(participant, $"draw|{command}");
            }
        }
        private void SendEmail(string toEmail)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                
                mail.From = new MailAddress("22520985@gm.uit.edu.vn","Painting Application of team N_T");
                mail.To.Add(toEmail);
                mail.Subject = "About the number of participants in your painting room";
                mail.Body = "Pay attention! The number of participants has reached the limit of 5";

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new NetworkCredential("22520985@gm.uit.edu.vn", "1991419869");
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                Invoke(new Action(() =>
                {
                    rtb_content.AppendText("Email sent to administrator.\r\n");
                }));
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    rtb_content.AppendText($"Failed to send email: {ex.Message}\r\n");
                }));
            }
        }

        private void SendParticipantsList(string code)
        {
            string participantList = string.Join(",", participants[code]);
            string message = $"participants|{participantList}";

            foreach (var participant in participants[code])
            {
                SendMessageToParticipant(participant, message);
            }
            if (participants[code].Count == 5)
            {
                string adminEmail = mailofahost[code];
                // Administrator's email
                SendEmail(adminEmail);
            }
        }

        private void SendMessageToParticipant(string participant, string message)
        {
            if (clientConnections.TryGetValue(participant, out TcpClient tcpClient))
            {
                if (tcpClient.Connected)
                {
                    try
                    {
                        StreamWriter writer = new StreamWriter(tcpClient.GetStream()) { AutoFlush = true };
                        writer.WriteLine(message);
                    }
                    catch (Exception ex)
                    {
                        Invoke(new Action(() =>
                        {
                            rtb_content.AppendText($"Error sending message to {participant}: {ex.Message}\r\n");
                        }));
                    }
                }
                else
                {
                    Invoke(new Action(() =>
                    {
                        rtb_content.AppendText($"Connection to {participant} lost.\r\n");
                        lock (clientConnections)
                        {
                            clientConnections.Remove(participant);
                        }
                    }));
                }
            }
            else
            {
                Invoke(new Action(() =>
                {
                    rtb_content.AppendText($"Participant {participant} not found.\r\n");
                }));
            }
        }

        private void server_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopServer();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

using Server;

namespace ColoringGame_Ver._2
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            listener = new Listener(2014);
            clients = new List<Client>();
            rooms = new List<Room>();
            
            tmrGReceiver = new System.Windows.Forms.Timer();
            tmrGReceiver.Interval = 100;
            tmrGReceiver.Tick += gReceive;
            tmrGReceiver.Start();

            LogView("서버 시작");
            LogView("게스트 수신 타이머 시작");

        }

        Listener listener;
        public List<Client> clients;
        public List<Room> rooms; 
        public int RoomCount = 0; // 방 번호를 매겨줌.

        System.Windows.Forms.Timer tmrGReceiver;

        //여기에 0페이즈 수신도 있음.
        void gReceive(object sender, EventArgs e)
        {

            if (listener.WaitingCount > 0)
            {
                Guest guest = listener.GetGuest();

                Thread.Sleep(100);
                Client c = new Client(guest);
                Thread.Sleep(100);

                void Receive0(object s, EventArgs ev)
                {

                    System.Windows.Forms.Timer t = s as System.Windows.Forms.Timer;

                    if (c.phase != 0) { return; }

                    if (guest.Connected)
                    {
                        string msg = guest.GetMsg();

                        if (msg != "")
                        {
                            if (c.phase == 0)
                            {

                                string tag = msg.Substring(0, 6); string content = msg.Substring(6);

                                //방들의 정보 신호를 받았을 때
                                if (tag == "#rfsh ")
                                {
                                    string temp = "0#rfsh ";
                                    
                                    for(int i = 0; i < rooms.Count; i++)
                                    {
                                        temp += rooms[i].RoomInfo();
                                        if (i != rooms.Count - 1) temp += "&";

                                    }

                                   guest.SendMsg(temp);

                                }
                                else if(tag == "#name ")
                                {
                                    c.name = content;
                                    LogView(c.name + "님의 참가");

                                    Broadcast("0#allc " + c.name + "님이 게임에 접속하셨습니다.");
                                }
                                //방 만들기 신호를 받았을 때
                                else if (tag == "#crte ")
                                {
                                    Room r = new Room(content, RoomCount, this);

                                    RoomCount++;

                                    r.EnMember(c);
                                    c.mnumber = 0;
                                    c.phase = 1;

                                    guest.SendMsg("0#crte Agree");
                                    rooms.Add(r);

                                    
                                    LogView(c.name + "님이" + content + "라는 새로운 방을 만들었습니다.");

                                }
                                //방 참여 신호를 받았을 때
                                else if (tag == "#join ")
                                {
                                    int target = Convert.ToInt32(content);

                                    bool jud = false;
                                    foreach(Room r in rooms)
                                    {
                                        //들어갈 방 확인
                                        if(r.number == target)
                                        {
                                            int n = r.EnMember(c);
                                            c.guest.SendMsg("0#join " + n.ToString());
                                            if(n != 99) c.phase = 1;
                                            jud = true;
                                        }
                                    }
                                    if (!jud)
                                        guest.SendMsg("0#join 99");

                                }
                                //전체 채팅 신호를 받았을 때
                                else if (tag == "#allc ")
                                {
                                    Broadcast("0#allc " + c.name + " : " + content);
                                    
                                }

                            }

                        }

                    }
                    else
                    {
                        LogView(c.name + "님이 종료하셨습니다.");
                        c.close();
                        t.Dispose();
                    }
                }

                c.tmrReceiver.Tick += Receive0;
                c.tmrReceiver.Start();
                clients.Add(c);

            }
        }
    
        private void MainForm_Load(object sender, EventArgs e)
        {

            FormClosing += MainForm_FormClosing;
            btnSend.Click += BtnSend_Click;

        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            Broadcast("0#allc 공지 : " + txtSend.Text);
            Broadcast("1#locc 공지 : " + txtSend.Text);

        }

        //로그
        public void LogView(string txt)
        {
            txtLog.AppendText(txt + "\r\n");
            txtLog.Focus();
            txtLog.ScrollToCaret();

            btnClient.Text = "클라 수 : " + (clients.Count).ToString() ;
            btnRoom.Text = "방 개수 : " + (rooms.Count).ToString();

        }

        //폼 닫기
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            listener.Close();


            foreach(Client client in clients)
            {
                client.close();

            }

        }
        //모든 멤버에게 보내기
        public void Broadcast(string txt)
        {
            foreach(Client client in clients)
            {
                client.guest.SendMsg(txt);
            }

        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Sanford.Multimedia.Midi;
using Sanford.Multimedia.Midi.UI;

namespace SequencerDemo
{
    public partial class Form1 : Form
    {
        enum PlayStatus {
            Playing, //���ڲ���
            Paused, //��;��ͣ 
            Loaded //�ռ�����ϣ���ʼ����
        }
        enum PlayMode
        {
            Repeat,
            Order,
            Random
        }

        private PlayMode mode;
        private int index = 0;
        private PlayStatus status;
        //Ϊtrue���Զ����ţ�Ϊ0���ֶ�����
        //private bool playingFlag = false;
        private PlayStatus Status {
               set {
                //
                if (value == PlayStatus.Paused)
                {
                    this.StateController.BackgroundImage = Image.FromFile("pic\\stop.png");
                    this.Stop();
                }
                else if (value == PlayStatus.Playing)
                {
                    this.StateController.BackgroundImage = Image.FromFile("pic\\play.png");
                    //if (Status == PlayStatus.Loaded)
                    if(Status == PlayStatus.Playing)
                    {
                        this.PlayingBox.SetSelected(index, true);
                        this.Start();
                    } else if (Status == PlayStatus.Paused)
                    { 
                        this.Continue();
                    }
                }
                else if (value == PlayStatus.Loaded) {
                    this.StateController.BackgroundImage = Image.FromFile("pic\\stop.png");
                }

                status = value;
            }

            get {
                return status;
            }
        }

        private bool scrolling = false;

        private bool playing = false;

        private bool closing = false;


        public List<String> playingList = new List<String>();

        //������һ���ⲿ�豸����
        private OutputDevice outDevice;

        private int outDeviceID = 0;

        private OutputDeviceDialog outDialog = new OutputDeviceDialog();
        AutoSizeFormClass asc = new AutoSizeFormClass();
        public Form1()
        {
            InitializeComponent();
            asc.controllInitializeSize(this);
            var str = Application.StartupPath;
            var aa = str;
        }

        protected override void OnLoad(EventArgs e)
        {
            if(OutputDevice.DeviceCount == 0)
            {
                MessageBox.Show("No MIDI output devices available.", "Error!",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Close();
            }
            else
            {
                try
                {
                    outDevice = new OutputDevice(outDeviceID);
                    sequence1.LoadProgressChanged += HandleLoadProgressChanged;
                    sequence1.LoadCompleted += HandleLoadCompleted;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error!",
                        MessageBoxButtons.OK, MessageBoxIcon.Stop);

                    Close();
                }
            }
            //���base��fromɶ��ϵѽ����ѭ��???
            base.OnLoad(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            //���ݰ��ĵļ�������Ӧ����,ͨ����ѭ����������Ϊ
            pianoControl1.PressPianoKey(e.KeyCode);
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            pianoControl1.ReleasePianoKey(e.KeyCode);
            base.OnKeyUp(e);
        }

        //�ر���
        protected override void OnClosing(CancelEventArgs e)
        {
            closing = true;

            base.OnClosing(e);
        }
        //�رպ󣬰��ⲿ�豸����Դ�ͷš��������
        protected override void OnClosed(EventArgs e)
        {
            sequence1.Dispose();

            if(outDevice != null)
            {
                outDevice.Dispose();
            }

            outDialog.Dispose();

            base.OnClosed(e);
        }

        //
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(openMidiFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = openMidiFileDialog.FileName;
                Open(fileName);
                if (!playingList.Contains(fileName)){
                    this.playingList.Add(fileName);
                    this.updateList(fileName);
                }
            }
        }

        public void Open(string fileName)
        {
            try
            {

                sequencer1.Stop();
                playing = false;
                sequence1.LoadAsync(fileName);
                this.Cursor = Cursors.WaitCursor;
                openToolStripMenuItem.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void outputDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutDialog dlg = new AboutDialog();

            dlg.ShowDialog();
        }

        private void Stop()
        {
            try
            {
                playing = false;
                sequencer1.Stop();
                timer1.Stop();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void Start()
        {
            try
            {
                playing = true;
                sequencer1.Start();
                timer1.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void Continue()
        {
            try
            {
                playing = true;
                sequencer1.Continue();
                timer1.Start();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void positionHScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if(e.Type == ScrollEventType.EndScroll)
            {
                sequencer1.Position = e.NewValue;

                scrolling = false;
            }
            else
            {
                scrolling = true;
            }
        }

        private void HandleLoadProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void HandleLoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            //Status = PlayStatus.Loaded;
            Status = PlayStatus.Playing;
            this.Cursor = Cursors.Arrow;
            openToolStripMenuItem.Enabled = true;

            if (e.Error == null)
            {
                positionHScrollBar.Value = 0;
                positionHScrollBar.Maximum = sequence1.GetLength();
            }
            else
            {
                MessageBox.Show(e.Error.Message);
            }

        }

        private void HandleChannelMessagePlayed(object sender, ChannelMessageEventArgs e)
        {
            if(closing)
            {
                return;
            }

            outDevice.Send(e.Message);
            pianoControl1.Send(e.Message);
        }

        private void HandleChased(object sender, ChasedEventArgs e)
        {
            foreach(ChannelMessage message in e.Messages)
            {
                outDevice.Send(message);
            }
        }

        private void HandleSysExMessagePlayed(object sender, SysExMessageEventArgs e)
        {
       //     outDevice.Send(e.Message); Sometimes causes an exception to be thrown because the output device is overloaded.
        }

        private void HandleStopped(object sender, StoppedEventArgs e)
        {
            foreach(ChannelMessage message in e.Messages)
            {
                outDevice.Send(message);
                pianoControl1.Send(message);
            }
        }

        //������Ϻ󣬸����û�ѡ���mode���в��š�
        //��һ�μ�������ǲ��Զ����ŵ�
        private void HandlePlayingCompleted(object sender, EventArgs e)
        {
            timer1.Stop();
            if (this.playingList.Count == 0)
            {
                return;
            }
            switch (mode) {
                case PlayMode.Order:
                    index = (index + 1) % this.playingList.Count;
                    break;
                case PlayMode.Random:
                    index = new Random().Next(this.playingList.Count);
                    break;
                case PlayMode.Repeat:
                    break;
            }

            this.BeginInvoke((Action<string>)Open, this.playingList[index]);
        }
        private void pianoControl1_PianoKeyDown(object sender, PianoKeyEventArgs e)
        {
            #region Guard

            if(playing)
            {
                return;
            }

            #endregion

            outDevice.Send(new ChannelMessage(ChannelCommand.NoteOn, 0, e.NoteID, 127));
        }

        private void pianoControl1_PianoKeyUp(object sender, PianoKeyEventArgs e)
        {
            #region Guard

            if(playing)
            {
                return;
            }

            #endregion

            outDevice.Send(new ChannelMessage(ChannelCommand.NoteOff, 0, e.NoteID, 0));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if(!scrolling)
            {
                positionHScrollBar.Value = Math.Min(sequencer1.Position, positionHScrollBar.Maximum);
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            asc.controlAutoSize(this);
        }

    
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            //��ʼ��һ��OpenFileDialog��
            OpenFileDialog fileDialog = new OpenFileDialog();

            //�ж��û��Ƿ���ȷ��ѡ�����ļ�
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                //��ȡ�û�ѡ���ļ��ĺ�׺��
                string extension = Path.GetExtension(fileDialog.FileName);
                //��������ĺ�׺��
                string[] str = new string[] { ".gif", ".jpge", ".jpg" };
                if (!((IList)str).Contains(extension))
                {
                    MessageBox.Show("�����ϴ�gif,jpge,jpg��ʽ��ͼƬ��");
                }
                else
                {
                    //��ȡ�û�ѡ����ļ������ж��ļ���С���ܳ���20K��fileInfo.Length�����ֽ�Ϊ��λ��
                    FileInfo fileInfo = new FileInfo(fileDialog.FileName);
                    if (fileInfo.Length > 204800)
                    {
                        MessageBox.Show("�ϴ���ͼƬ���ܴ���200K");
                    }
                    else
                    {
                        this.BackgroundImage = Image.FromFile(fileDialog.FileName);
                        //������Ϳ���д��ȡ����ȷ�ļ���Ĵ�����
                    }
                }
            }
        }

        private void panel2_Click(object sender, EventArgs e)
        {
            if (Status == PlayStatus.Paused || Status == PlayStatus.Loaded)
            {
                Status = PlayStatus.Playing;
            }
            else {
                Status = PlayStatus.Paused;
            };
        }


        private void modeButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton button = (RadioButton)sender;
            String name = button.Name;
            switch (name) {
                case "repeatButton":
                    mode = PlayMode.Repeat;
                    break;
                case "orderButton":
                    mode = PlayMode.Order;
                    break;
                case "randomButton":
                    mode = PlayMode.Random;
                    break;
                default:
                    break;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
            this.BackgroundImage = Image.FromFile("pic\\simple1.jpg");
            this.StateController.BackgroundImage = Image.FromFile("pic\\stop.png");
            this.orderButton.Checked = true;
            
        }
        private void updateList(string fileName)
        {
            string s = System.IO.Path.GetFileNameWithoutExtension(fileName);
            this.PlayingBox.Items.Add(s);
        }

        private void PlayingBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                this.index = e.Index;
                e.DrawBackground();
                Brush mybsh = Brushes.Black;
                // �ж���ʲô���͵ı�ǩ
                mybsh = Brushes.Yellow;
                // �����
                e.DrawFocusRectangle();
                //�ı� 
                e.Graphics.DrawString(PlayingBox.Items[e.Index].ToString(), e.Font, mybsh, e.Bounds, StringFormat.GenericDefault);
            }
        }

        private void StateController_SizeChanged(object sender, EventArgs e)
        {
            this.StateController.BackgroundImage = Image.FromFile("pic\\stop.png");
        }
    }
}
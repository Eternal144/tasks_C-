﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using ZedGraph;

namespace WpfSerialPort
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort mySerialPort=null;
        MidiData Rxdata = null;
        Byte[] BSendTemp = new Byte[3];
        List<int> valueList;
        System.Windows.Threading.DispatcherTimer dtimer;
        private bool isLogging = false;
        private string logStr = "";
        private PointPairList lightList;
        private PointPairList tempList;

        public MainWindow()
        {
            BSendTemp[0] = 0x96;
            BSendTemp[1] = 0x00;
            BSendTemp[2] = 0x00;
            //BSendTemp = { 0x96,0x00,0x00}
            //这个Rx是midi传过来的值
            InitializeComponent();
            Rxdata =new MidiData();

            Binding myBinding = new Binding("FrameMsg");
            myBinding.Source = Rxdata;
            //myBinding.Mode = BindingMode.TwoWay;
           
            /////myBinding.ElementName = "FrameMsg";
            //// Bind the new data source to the myText TextBlock control's Text dependency property.
            txtRxDataReal.SetBinding(TextBox.TextProperty, myBinding);
            //showShine.SetBinding(TextBox.TextProperty, myTemperbinding);
            //showTemper.SetBinding(TextBox.TextProperty, myLightBinding);

            comboBoxBaud.Items.Clear();
            comboBoxBaud.Items.Add("9600");
            comboBoxBaud.Items.Add("19200");
            comboBoxBaud.Items.Add("38400");
            comboBoxBaud.Items.Add("57600");
            comboBoxBaud.Items.Add("115200");
            comboBoxBaud.Items.Add("921600");
            comboBoxBaud.SelectedItem = comboBoxBaud.Items[2];

            initChart();
        }

        private void ComboBoxPortName_DropDownOpened(object sender, EventArgs e)
        {
            string[] portnames = SerialPort.GetPortNames();
            //Console.WriteLine(portnames.Length);
            ComboBox x = sender as ComboBox;
            x.Items.Clear();
            foreach (string xx in portnames)
            {
                //Console.WriteLine(xx);
                x.Items.Add(xx);
            }
        }

        private void ComboBoxBaud_DropDownOpened(object sender, EventArgs e)
        {

        }

        private void ComboBoxBaud_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox x = sender as ComboBox;
            Console.WriteLine("Selected:"+x.SelectedItem);
        }

        private void BtnCloseSerialPort_Click(object sender, RoutedEventArgs e)
        {
            if(mySerialPort!=null)
            {
                mySerialPort.DataReceived -= new SerialDataReceivedEventHandler(DataReceivedHandler);
                mySerialPort.Close();
                Console.WriteLine("Closed:"+mySerialPort.ToString());
            }
        }

        private void BtnOpenSerialPort_Click(object sender, RoutedEventArgs e)
        {
            if(comboBoxPortName.SelectedItem !=null)
            {
                if (mySerialPort != null)
                {
                    mySerialPort.DataReceived -= new SerialDataReceivedEventHandler(DataReceivedHandler);
                    mySerialPort.Close();
                }
                mySerialPort = new SerialPort(comboBoxPortName.SelectedItem.ToString());
                
                mySerialPort.BaudRate = int.Parse(comboBoxBaud.SelectedItem.ToString());
                mySerialPort.Parity = Parity.None;
                mySerialPort.StopBits = StopBits.One;
                mySerialPort.DataBits = 8;
                mySerialPort.Handshake = Handshake.None;
                mySerialPort.RtsEnable = false;
                mySerialPort.ReceivedBytesThreshold = 1;
                mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                mySerialPort.Open();
                Console.WriteLine("Open Selected Port:" + comboBoxPortName.SelectedItem);
            }
            else
            {
                Console.WriteLine("No Selected Serial Port:");
                MessageBox.Show("Please select serial port", "Warning");

            }

        }

        private  void DataReceivedHandler(
                    object sender,
                    SerialDataReceivedEventArgs e)
        {
            //SerialPort sp = (SerialPort)sender;
            if (mySerialPort == null)
            {
                return;
            }
            int numOfByte = mySerialPort.BytesToRead;
            for(int i=0;i< numOfByte;i++)
            {

                int indata = mySerialPort.ReadByte(); 


                //Console.Write("\n{0:X2}\n ", indata);
                if ((indata & 0x80) !=0)
                {
         
                    //Console.Write("\n New Data Frame:");
                    Rxdata.DataIdx = 0;
                    Rxdata.SerialDatas[Rxdata.DataIdx] =(byte) indata;
                    Rxdata.DataIdx++;
                }else if(Rxdata.DataIdx < Rxdata.SerialDatas.Length)
                {
                   //Console.Write("{0:X2} ", indata);
                    Rxdata.SerialDatas[Rxdata.DataIdx] = (byte)indata;
                    Rxdata.DataIdx++;
                }

                if (Rxdata.DataIdx >= 3)
                {
                    //Output
                    //Console.Write("\n OneFrame:{0:X2}-{1:X2}-{2:X2}", Rxdata.SerialDatas[0],
                    //    Rxdata.SerialDatas[1],
                    //    Rxdata.SerialDatas[2]);

                    string msg = string.Format("\n OneFrame:{0:X2}-{1:X2}-{2:X2}, RealData=0x{3:X4}", Rxdata.SerialDatas[0],
                        Rxdata.SerialDatas[1],
                        Rxdata.SerialDatas[2],
                        Rxdata.RealData);
                    SetTextInTextBox(txtRxData,msg);
                    Rxdata.FrameMsg = string.Format("ADVal=0x{0:X4}={0:d}", Rxdata.RealData);

                    DateTime startDateTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
                    double t = (DateTime.Now - startDateTime).TotalSeconds;

                    this.Dispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Normal,
                            new Action(
                            delegate ()
                            {
                                if (Rxdata.SerialDatas[0] == 0xe0)
                                {
                                    int temp = Rxdata.SerialDatas[2];
                                    temp = (temp << 7) + (Rxdata.SerialDatas[1] & 0x7f);
  
                                    double value = 5 * (temp / 1023.0);
                                    double temper = (5 - value) / value * 10000;
                                    double result = (1.0 / (Math.Log(temper / 10000) / 3435.0 + 1 / 298.15)) - 273.15;
                                    PointPair point = new PointPair(t, result);
                                    Console.WriteLine(result);
                                    showTemper.Text = String.Format("{0:00.00}", result);
                                    tempList.Add(point);
                                }
                                else if (Rxdata.SerialDatas[0] == 0xe1)
                                {
                                    int light = Rxdata.SerialDatas[2];
                                    light = (light << 7) + (Rxdata.SerialDatas[1] & 0x7f);
                                    PointPair point = new PointPair(t, light);
                                    showShine.Text = light.ToString();
                                    lightList.Add(point);
                                }

                                zedGraphControl1.AxisChange();//画到zedGraphControl1控件中，此句必加
                                zedGraphControl1.Refresh();
                            }
                        )
                    );
                    if (isLogging) {

                        log(msg);
                        byte[] buffer = { 0xa1, 0x1, 0x0 };
                        mySerialPort.Write(buffer, 0, 3);

                    }
                }
            }
        }
        private void initChart() {
            GraphPane mPane = zedGraphControl1.GraphPane;//获取索引到GraphPane面板上

            mPane.XAxis.Title.Text = "waveLength";//X轴标题
            mPane.YAxis.Title.Text = "A/D";//Y轴标题
            mPane.Title.Text = "NIRS";//标题
            //mPane.XAxis.Scale.MaxAuto = true;
            mPane.XAxis.Type = ZedGraph.AxisType.LinearAsOrdinal;//出现图表右侧出现空白的情况....
            lightList = new PointPairList();//数据点
            tempList = new PointPairList();//数据点
            mPane.XAxis.CrossAuto = true;//容许x轴的自动放大或缩小

            LineItem mLightCure = mPane.AddCurve("光强", lightList, System.Drawing.Color.Blue, SymbolType.None);
            LineItem mTempCure = mPane.AddCurve("温度", tempList, System.Drawing.Color.Red, SymbolType.None);
            zedGraphControl1.AxisChange();//画到zedGraphControl1控件中，此句必加
        }

        private delegate void SetTextCallback(TextBox control, string text);
        public void SetTextInTextBox(TextBox control, string msg)
        {
            if (txtRxData.Dispatcher.CheckAccess())
            {
                txtRxData.AppendText(msg);
                txtRxData.ScrollToEnd();
            }
            else
            {
                SetTextCallback d = new SetTextCallback(SetTextInTextBox);
                Dispatcher.Invoke(d, new object[] { control, msg });
            }
        }

        private void btnSendData_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnSendData_Click_1(object sender, RoutedEventArgs e)
        {
            int val_red = (int)red_slider.Value;
            //int val_green = (int)green_silder.Value;
            int val_blue = (int)blue_slider.Value;
            int val_white = (int)white_slider.Value;
            MessageBox.Show("红灯亮度： " + val_red.ToString() + "\n"
                //+ "绿灯亮度： " + val_green.ToString() + "\n"
                + "蓝灯亮度： " + val_blue.ToString() + "\n"
                + "白灯亮度： " + val_white.ToString() + "\n");
            byte[] redBuffer = { 0xA0, 0x3,(byte)val_red };
            
            byte[] blueBuffer = { 0xA0, 0x9, (byte)val_blue };

            byte[] whiteBuffer = { 0xA0, 0x6, (byte)val_white };
            mySerialPort.Write(redBuffer, 0, 3);
            Thread.Sleep(100);
            //;portWrite(light_green);
            Thread.Sleep(100);
            mySerialPort.Write(blueBuffer, 0, 3);
            Thread.Sleep(100);
            mySerialPort.Write(whiteBuffer, 0, 3);

        }

        private void Data_sent_Click(object sender, RoutedEventArgs e)
        {
            valueList = new List<int>();
            string[] hexValuesSplit = this.txtTxData.Text.Split(' ');
            if (hexValuesSplit.Length > 0)
            {
                foreach (String hex in hexValuesSplit)
                {
                    int value = Int32.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                    valueList.Add(value);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                BSendTemp[i] = (Byte)valueList[i];

                Console.WriteLine(BSendTemp[i]);
            }

            mySerialPort.Write(BSendTemp, 0, 3);
        }

        private void Log_start_Click(object sender, RoutedEventArgs e)
        {
            isLogging = true;
            byte[] buffer = { 0xa1, 0x1, 0x0 };
            mySerialPort.Write(buffer, 0, 3);

        }

        private void log(string msg) {
            logStr += msg;
        }

        private void Log_end_Click(object sender, RoutedEventArgs e)
        {
            isLogging = false;
            // save logStr
            byte[] buffer = { 0xa1, 0x2, 0x0 };
            mySerialPort.Write(buffer, 0, 3);

            DateTime dt = DateTime.Now;
            string filename = string.Format("log-{0:yyyy-MM-dd-HH-mm-ss}.txt", dt);
            File.WriteAllText(filename, logStr);

            MessageBox.Show(" 已保存文件：" + filename);
            logStr = "";
        }
    }
}

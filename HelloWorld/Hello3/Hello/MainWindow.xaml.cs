﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Hello
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {

            InitializeComponent();
            ForDataBinding data = new ForDataBinding();
            string[] pargs = Environment.GetCommandLineArgs();
            if(pargs.Length >= 2)
            {
                string s = "";
                for(int i = 1; i < pargs.Length; i++)
                {
                    s += pargs[i];
                    s += " ";
                }
                data.Input = s;
            }
            else
            {
                data.Input = "Nothing input";
            }
           
            this.fordatabinding.DataContext = data;

        }
        class ForDataBinding
        {
            public string Input { get; set; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("今天好");
        }
    }
}

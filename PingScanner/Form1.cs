using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingScanner
{
    public partial class Form1 : Form
    {
        IPAddress ipaddress;
        IPAddress netmask;
        IPAddress networkaddress;
        string networkstring;
        Thread thread = null;

        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            ipaddress = GetIPAdress();
            netmask = GetNetmask();
            networkaddress = GetNetworkAddress(ipaddress, netmask);
            networkstring = networkaddress.ToString();
            networkstring = Regex.Replace(networkstring, ".[^\\.]*$", "");
            inputNetwork.Text = networkstring;
            buttonStop.Enabled = false;
        }

        public IPAddress GetIPAdress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null && !addr.Address.ToString().Equals("0.0.0.0"))
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public IPAddress GetNetmask()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null && !addr.Address.ToString().Equals("0.0.0.0"))
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.IPv4Mask;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAddressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAddressBytes.Length != subnetMaskBytes.Length)
                MessageBox.Show("Length of ip address and netmask are different !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            byte[] broadcastAddress = new byte[ipAddressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAddressBytes[i] & (subnetMaskBytes[i]));
            }
            return new IPAddress(broadcastAddress);
        }

        private void buttonScan_Click(object sender, EventArgs e)
        {
            if (inputNetwork.Text == string.Empty)
            {
                MessageBox.Show("Insert the network address that you want to scan !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                thread = new Thread(() => scan(inputNetwork.Text));
                thread.Start();
                if (thread.IsAlive)
                {
                    buttonStop.Enabled = true;
                    buttonScan.Enabled = false;
                    buttonScan.Enabled = false;
                }
            }
        }

        public void scan(String prefix)
        {
            try
            {
                progressBar1.Maximum = 254;
                progressBar1.Value = 0;
                Ping ping;
                PingReply reply;
                IPAddress ipaddress;
                IPHostEntry host;
                listHosts.Items.Clear();
                for (int i = 1; i < 255; i++)
                {
                    string suffix = "." + i.ToString();
                    ping = new Ping();
                    reply = ping.Send(prefix + suffix, 300);
                    textStatus.ForeColor = System.Drawing.Color.Red;
                    textStatus.Text = "Scanning " + prefix + suffix;
                    if (reply.Status == IPStatus.Success)
                    {
                        try
                        {
                            ipaddress = IPAddress.Parse(prefix + suffix);
                            host = Dns.GetHostEntry(ipaddress);
                            listHosts.Items.Add(new ListViewItem(new String[] { prefix + suffix, host.HostName, "Up" }));
                        }
                        catch
                        {
                            MessageBox.Show("Can't ping " + prefix + suffix + " host !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                    }
                    progressBar1.Value += 1;
                }
                textStatus.ForeColor = System.Drawing.Color.Green;
                textStatus.Text = "Done";
                int count = listHosts.Items.Count;
                MessageBox.Show("Found " + count + " hosts in this LAN !", "Scan terminated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonStop.Enabled = false;
                buttonScan.Enabled = true;
            }
            catch
            {
                MessageBox.Show("Lan not valid !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            stopWork();
        }

        public void stopWork()
        {
            try
            {
                thread.Suspend();
            }
            catch
            {
                MessageBox.Show("Can't stop thread !", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            buttonScan.Enabled = true;
            buttonStop.Enabled = false;
            buttonScan.Enabled = true;
            progressBar1.Value = 0;
            textStatus.ForeColor = System.Drawing.Color.Red;
            textStatus.Text = "Idle";
        }

        private void esciToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void informazioniSuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("PingScanner v.1.0\n\nDeveloped by: Mirko Bastianini", "Informazioni", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

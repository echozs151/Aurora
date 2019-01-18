using Aurora.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aurora.Devices.YeeLight
{
    public class YeeLightDevice : Device
    {
        private string devicename = "YeeLight";

        private bool isConnected;
        private Stopwatch sw = new Stopwatch();

        private Stopwatch watch = new Stopwatch();
        private YeeLight light = new YeeLight();
        private long lastUpdateTime = 0;

        private VariableRegistry default_registry = null;

        public string GetDeviceDetails()
        {
            if (isConnected)
                return devicename + ": Connected";
            else
                return devicename + ": Not connected";
        }

        public string GetDeviceName()
        {
            return devicename;
        }

        public bool Initialize()
        {
            if (!isConnected)
            {
                try
                {
                    Connect();
                }
                catch (Exception exc)
                {
                    Global.logger.Error($"Device {devicename} encountered an error during Connecting. Exception: {exc}");
                    isConnected = false;

                    return false;
                }
            }

            return isConnected;
        }

        public bool IsConnected()
        {
            return isConnected;
        }

        public bool IsInitialized()
        {
            return IsConnected();
        }

        public bool IsKeyboardConnected()
        {
            throw new NotImplementedException();
        }

        public bool IsPeripheralConnected()
        {
            throw new NotImplementedException();
        }

        public bool Reconnect()
        {
            light.CloseConnection();

            isConnected = false;

            Connect();
            return true;
        }

        public void Reset()
        {
            Reconnect();
        }

        public void Shutdown()
        {
            light.CloseConnection();

            isConnected = false;

            if (sw.IsRunning)
                sw.Stop();
        }

        public void Connect(DoWorkEventArgs token = null)
        {
            try
            {
                if (!light.isConnected())
                {
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                    {
                        IPAddress.TryParse(Global.Configuration.VarRegistry.GetVariable<string>($"{devicename}_yeelight_IP"), out IPAddress lightIP);
                        socket.Connect(lightIP, 65530);
                        isConnected = light.Connect(lightIP, 55443) && light.SetMusicMode(((IPEndPoint)socket.LocalEndPoint).Address, 54321);
                    }
                }
            }
            catch (Exception exc)
            {
                Global.logger.Error($"Device {devicename} encountered an error during Connecting. Exception: {exc}");
                isConnected = false;
            }
        }

        public bool UpdateDevice(DeviceColorComposition colorComposition, DoWorkEventArgs e, bool forced = false)
        {
            if (e.Cancel) return false;

            watch.Restart();

            // Connect if needed
            if (!isConnected)
                Connect(e);

            if (e.Cancel) return false;

            // Reduce sending based on user config
            if (!sw.IsRunning)
                sw.Start();

            if (e.Cancel) return false;

            if (sw.ElapsedMilliseconds >
                Global.Configuration.VarRegistry.GetVariable<int>($"{devicename}_send_delay"))
            {
                Color targetColor;
                if (!Global.Configuration.VarRegistry.GetVariable<bool>($"{devicename}_use_peripheral_led_as_color"))
                {
                    lock (colorComposition.bitmapLock)
                    {
                        //Fix conflict with debug bitmap
                        lock (colorComposition.keyBitmap)
                        {

                            targetColor = Utils.BitmapUtils.GetRegionColor(
                                (Bitmap)colorComposition.keyBitmap,
                                new BitmapRectangle(0, 0, colorComposition.keyBitmap.Width,
                                    colorComposition.keyBitmap.Height)
                            );


                        }
                    }
                }
                else
                {
                    targetColor = colorComposition.keyColors.FirstOrDefault(pair => pair.Key == DeviceKeys.Peripheral_Logo).Value;
                }

                light.SetColor(targetColor.R, targetColor.G, targetColor.B);
                sw.Restart();
            }

            if (e.Cancel) return false;

            watch.Stop();
            lastUpdateTime = watch.ElapsedMilliseconds;

            return true;
        }

        public bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, DoWorkEventArgs e, bool forced = false)
        {
            throw new NotImplementedException();
        }

        public string GetDeviceUpdatePerformance()
        {
            return (IsConnected() ? lastUpdateTime + " ms" : "");
        }

        public VariableRegistry GetRegisteredVariables()
        {
            if (default_registry == null)
            {
                default_registry = new VariableRegistry();
                default_registry.Register($"{devicename}_use_peripheral_led_as_color", true, "Use peripheral logo led as color", null, null, "Uses mouse logo led as color instead of average of colors if selected");
                default_registry.Register($"{devicename}_send_delay", 100, "Send delay (ms)");
                default_registry.Register($"{devicename}_yeelight_IP", "0.0.0.0", "YeeLight IP", null, null, "Supports only one light at the moment, make sure LAN Control is enabled");
            }

            return default_registry;
        }
    }
}

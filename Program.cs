using HidSharp;

namespace DualSenseBattery
{
    class Program
    {
        static void Main(string[] args)
        {

            var list = DeviceList.Local;
            list.Changed += (sender, e) => Console.WriteLine("Device list changed.");

            var hidDeviceList = list.GetHidDevices().ToArray();

            foreach (HidDevice dev in hidDeviceList)
            {
                if (dev.VendorID != 1356 || dev.ProductID != 3302)
                {
                    continue;
                }

                Console.WriteLine(dev);
                Console.WriteLine();

                try
                {
                    var reportDescriptor = dev.GetReportDescriptor();

                    foreach (var deviceItem in reportDescriptor.DeviceItems)
                    {
                        Console.WriteLine("Opening device...");

                        HidStream hidStream;
                        if (dev.TryOpen(out hidStream))
                        {
                            Console.WriteLine("Opened device");
                            hidStream.ReadTimeout = Timeout.Infinite;

                            using (hidStream)
                            {
                                var inputReportBuffer = new byte[dev.GetMaxInputReportLength()];

                                IAsyncResult ar = null;
                                int offsetAddress = 0;
                                int batteryLevelPercent = 0;
                                bool batteryFull = false;
                                bool batteryCharging = false;

                                while (true)
                                {
                                    if (ar == null)
                                    {
                                        ar = hidStream.BeginRead(inputReportBuffer, 0, inputReportBuffer.Length, null, null);
                                    }

                                    if (ar != null)
                                    {
                                        if (ar.IsCompleted)
                                        {
                                            int byteCount = hidStream.EndRead(ar);
                                            ar = null;

                                            if (byteCount > 0)
                                            {
                                                //string hexOfBytes = string.Join(" ", inputReportBuffer.Take(byteCount).Select(b => b.ToString("X2")));
                                                //Console.WriteLine("  {0}", hexOfBytes);

                                                Console.WriteLine();
  
                                                if (inputReportBuffer.Length == 64) //USB
                                                    offsetAddress = 1;
                                                else if (inputReportBuffer.Length == 78) //BT
                                                    offsetAddress = 0;
                                                else
                                                    break;

                                                batteryLevelPercent = (inputReportBuffer[54 - offsetAddress] & 0x0f) * 100 / 8;
                                                batteryFull = (inputReportBuffer[54 - offsetAddress] & 0x20) != 0;
                                                batteryCharging = (inputReportBuffer[55 - offsetAddress] & 0x08) != 0;

                                                Console.WriteLine("Percent: " + batteryLevelPercent);
                                                Console.WriteLine("Full: " + batteryFull);
                                                Console.WriteLine("Charging: " + batteryCharging);
                                                Console.WriteLine();

                                                Console.WriteLine("----------------------------------");
                                                Thread.Sleep(5000);

                                            }
                                        }
                                        else
                                        {
                                            ar.AsyncWaitHandle.WaitOne(1000);
                                        }
                                    }
                                }
                            }
                            Console.WriteLine("Closed device.");
                        }
                        else
                        {
                            Console.WriteLine("Failed to open device.");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
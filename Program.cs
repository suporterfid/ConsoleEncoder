﻿using Impinj.OctaneSdk;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ConsoleEncoder
{
    internal static class Program
    {
        private static readonly ConcurrentQueue<TagEvent> _messageQueueTagSmartReaderTagEventSocketServer = new();
        private static readonly ImpinjReader reader = new();
        private static ushort NUM_WORDS_USER_MEMORY = 128;
        private static int opIdUser;
        private static int opIdTid;
        private static string? hostname;

        private static async Task Main(string[] args)
        {
            TcpListener listener = new(IPAddress.Any, 14150);
            listener.Start();

            Console.WriteLine($"Server listening on port 14150 ...");
            if (args.Length == 0)
            {
                Console.WriteLine("Error: No hostname specified.  Pass in the reader hostname as a command line argument when running the app.");
            }
            else
            {
                Program.hostname = args[0];
                if(args.Length > 0 && args.Length == 2)
                {
                    try
                    {
                        Program.NUM_WORDS_USER_MEMORY = ushort.Parse(args[0]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsing user memory size (words): " + ex.Message);
                    }
                    
                }
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("Client connected.");
                    _ = Program.HandleClientAsync(client);
                }
            }
        }

        private static async Task HandleClientAsync(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            byte[]? buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int num = await stream.ReadAsync(buffer, 0, buffer.Length);
                    int bytesRead;
                    if ((bytesRead = num) > 0)
                    {
                        string? receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        if (receivedData.StartsWith("START"))
                        {
                            CancellationTokenSource cancellationTokenSource = new();
                            Program.StartRead(hostname);
                            _ = Task.Run(() => Program.SendDataAsync(stream, cancellationTokenSource.Token));
                            do
                            {
                                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                                receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                            }
                            while (!receivedData.StartsWith("STOP"));
                            Program.Stop();
                            Program._messageQueueTagSmartReaderTagEventSocketServer.Clear();
                            cancellationTokenSource.Cancel();
                        }
                        
                    }
                    else
                    {
                        break;
                    }
                }
                buffer = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                buffer = null;
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }

        private static void OptimizedRead(Settings settings, ushort wordPointer, ushort wordCount)
        {
            try
            {
                TagReadOp tagReadOp1 = new()
                {
                    MemoryBank = MemoryBank.User,
                    WordCount = wordCount,
                    WordPointer = wordPointer
                };
                TagReadOp tagReadOp2 = new()
                {
                    MemoryBank = MemoryBank.Tid,
                    WordCount = 6,
                    WordPointer = 0
                };
                settings.Report.OptimizedReadOps.Add(tagReadOp1);
                settings.Report.OptimizedReadOps.Add(tagReadOp2);
                Program.opIdUser = tagReadOp1.Id;
                Program.opIdTid = tagReadOp2.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private static void StartRead(string hostname)
        {
            try
            {
                Program.reader.Connect(hostname);
                // ISSUE: reference to a compiler-generated field
                // ISSUE: reference to a compiler-generated field
                Program.reader.TagOpComplete += OnTagOpComplete;
                Settings settings = Program.reader.QueryDefaultSettings();
                settings.Antennas.EnableAll();
                settings.Antennas.RxSensitivityMax = true;
                settings.Antennas.TxPowerMax = false;
                settings.Antennas.TxPowerInDbm = 18.0;
                Program.OptimizedRead(settings, 3, NUM_WORDS_USER_MEMORY);
                Program.reader.ApplySettings(settings);
                Program.reader.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private static void Stop()
        {
            try
            {
                Program.reader.TagOpComplete -= Program.OnTagOpComplete;
                Program.reader.Stop();
                Program.reader.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private static void WriteUserMemory(string targetEpc)
        {
            try
            {
                TagOpSequence sequence = new();
                sequence.TargetTag.MemoryBank = MemoryBank.Epc;
                sequence.TargetTag.BitPointer = 32;
                sequence.TargetTag.Data = targetEpc;
                sequence.BlockWriteEnabled = true;
                sequence.BlockWriteWordCount = 2;
                TagWriteOp tagWriteOp = new()
                {
                    MemoryBank = MemoryBank.User
                };
                string hex = new('0', 512);
                tagWriteOp.Data = TagData.FromHexString(hex);
                tagWriteOp.WordPointer = 0;
                sequence.Ops.Add(tagWriteOp);
                Program.reader.AddOpSequence(sequence);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private static void OnTagOpComplete(ImpinjReader reader, TagOpReport report)
        {
            string str1;
            string targetEpc = str1 = "";
            string str2 = str1;
            string str3 = str1;
            foreach (TagOpResult tagOpResult in report)
            {
                if (tagOpResult != null)
                {
                    if (tagOpResult is TagReadOpResult and TagReadOpResult tagReadOpResult)
                    {
                        targetEpc = tagReadOpResult.Tag.Epc.ToHexString();
                        if (tagReadOpResult.OpId == Program.opIdUser)
                        {
                            str3 = tagReadOpResult.Data.ToHexString();
                        }
                        else if (tagReadOpResult.OpId == Program.opIdTid)
                        {
                            str2 = tagReadOpResult.Data.ToHexString();
                        }
                    }
                    else if (tagOpResult is TagWriteOpResult and TagWriteOpResult tagWriteOpResult)
                    {
                        Console.WriteLine("Write complete.");
                        Console.WriteLine("EPC : {0}", tagWriteOpResult.Tag.Epc);
                        Console.WriteLine("Status : {0}", tagWriteOpResult.Result);
                        Console.WriteLine("Number of words written : {0}", tagWriteOpResult.NumWordsWritten);

                    }
                }

            }
            TagEvent tagEvent = new()
            {
                Epc = targetEpc,
                Tid = str2,
                User = str3
            };
            try
            {
                Program._messageQueueTagSmartReaderTagEventSocketServer.Enqueue(tagEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("EPC:{0};User:{1}", targetEpc, str3);
            if (string.IsNullOrEmpty(str3) || str3.StartsWith("00"))
            {
                return;
            }

            try
            {
                Program.WriteUserMemory(targetEpc);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task SendDataAsync(
          NetworkStream stream,
          CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!Program._messageQueueTagSmartReaderTagEventSocketServer.IsEmpty)
                    {
                        TagEvent? tagEvent = null;
                        while (Program._messageQueueTagSmartReaderTagEventSocketServer.TryDequeue(out tagEvent))
                        {
                            if (tagEvent != null)
                            {
                                string? dataToSend = "EPC:" + tagEvent.Epc + ";USER:" + tagEvent.User;
                                byte[]? bytesToSend = Encoding.ASCII.GetBytes(dataToSend + "\r\n");
                                await stream.WriteAsync(bytesToSend, 0, bytesToSend.Length);
                                
                            }
                        }
                        
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Data sending canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
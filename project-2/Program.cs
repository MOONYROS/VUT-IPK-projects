// IPK - project 2 (2023) - IOTA: Server for Remote Calculator
// xlukas15 (xlukas15@stud.fit.vutbr.cz)

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;

using Microsoft.Extensions.CommandLineUtils; // deprecated, but included in .NET Core and has sufficient Linux compatibility, so I use this
//using McMaster.Extensions.CommandLineUtils; // or newer open source version can be used (that would need some changes in the code though)
using System.Text.RegularExpressions;

namespace ipkcpd
{
    class Program
    {
        public const int DEFAULT_PORT = 2023;

        private static TcpListener? listener = null;
        private static CancellationTokenSource cts = new();
        static List<TcpClient> clients = new();

        private static UdpClient? udpServer = null;

        static void CloseAllConnections()
        {
            while (clients.Count > 0)
            {
                TcpClient clnt = clients[0];
                // we ignore this in case something happened, because we are finishing anyway
                try
                {
                    NetworkStream strm = clnt.GetStream();
                    byte[] msg = Encoding.ASCII.GetBytes("BYE\n");
                    strm.Write(msg, 0, msg.Length);
                }
                finally
                {
                    clients.Remove(clnt);
                    clnt.Close();
                }
            }

            // we close the listener in a similar way
            try
            {
                listener?.Stop();
            }
            catch
            {
            }
            
            // no need to Dispose() UDP client, as it uses 'using' in implementation
        }

        static void ErrorExit(string msg)
        {
            Console.Error.WriteLine(msg);
            cts.Cancel();
            CloseAllConnections();
            Environment.Exit(-1);
        }

        static void ErrorExitHelp(string msg)
        {
            Console.Error.WriteLine(msg);
            Console.Error.WriteLine("Use ipkcpd --help for help");
            cts.Cancel();
            CloseAllConnections();
            Environment.Exit(-1);
        }

#pragma warning disable CS8622
        private static void CancelKeyPress(object sender, ConsoleCancelEventArgs eventArgs)
        {
            cts.Cancel();
            eventArgs.Cancel = true;
            CloseAllConnections();
        }

        static bool IsNumeric(string input)
        {
            foreach (char c in input)
            {
                if (!Char.IsDigit(c))
                {
                    return false;
                }
            }
            return true;
        }

        static double EvaluateSimple(char op, double num1, double num2)
        {
            if (op == '/' && num2 == 0)
                throw new ArgumentException("Division by zero.");

            return op switch
            {
                '+' => num1 + num2,
                '-' => num1 - num2,
                '*' => num1 * num2,
                '/' => num1 / num2,
                _ => throw new ArgumentException($"Invalid operator: {op}."),// should not pass through regex, but just to be sure
            };
        }
        static string EvalExpression(string expr)
        {
            // Check if the expression has a pair of parentheses
            if (!expr.StartsWith("(") || !expr.EndsWith(")"))
            {
                throw new ArgumentException("Expression to solve must be enclosed in parentheses.");
            }
            return EvalExpressionRecursive(expr).ToString();

        }
        static double EvalExpressionRecursive(string expr)
        {
            // Define a regular expression pattern to match an expression
            string pattern = @"\(([\+\-\*/]) ((?:\([^()]*\)|[^()\s])+) ((?:\([^()]*\)|[^()\s])+)\)";

            // Replace matched expressions with their evaluated results
            while (Regex.IsMatch(expr, pattern))
            {
                expr = Regex.Replace(expr, pattern, match =>
                {
                    char op = match.Groups[1].Value[0];
                    for (int i = 2; i <= 3; i++)
                    {
                        if (IsNumeric(match.Groups[i].Value) && match.Groups[i].Length > 1)
                        { 
                            throw new ArgumentException("More than one digit used in number");
                        }
                    }

                    double num1 = EvalExpressionRecursive(match.Groups[2].Value.Trim());
                    double num2 = EvalExpressionRecursive(match.Groups[3].Value.Trim());

                    // Perform the operation and return the result as a string
                    return EvaluateSimple(op, num1, num2).ToString();
                });
            }

            // If the expression is fully evaluated, return its value
            if (double.TryParse(expr, out double result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException("Invalid expression format.");
            }
        }

        static async Task WriteIgnoreException(StreamWriter writer, string str)
        {
            try
            {
                await writer.WriteLineAsync(str);
            }
            catch
            {
                // we ignore it
            }
        }

        private static async void HandleTcpClientAsync(TcpClient client, CancellationToken ct)
        {
            // Console.WriteLine("Client connected");

            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.ASCII);
            var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            try
            {
                string? res = await reader.ReadLineAsync();
                if (res == "HELLO")
                {
                    await writer.WriteLineAsync("HELLO");
                }
                else if (res == "BYE")
                {
                    throw new Exception($"BYE received - sending response BYE");
                }
                else
                {
                    throw new Exception($"HELLO expected - sending response BYE");
                }

                while (!ct.IsCancellationRequested)
                {
                    string? request = await reader.ReadLineAsync();

                    if (request == null)
                    {
                        throw new Exception($"NULL request received - sending response BYE");
                    }


                    if (request == "BYE")
                    {
                        throw new Exception($"BYE received - sending response BYE");
                    }
                    else if (request.StartsWith("SOLVE "))
                    {
                        string expression = request["SOLVE ".Length..].Trim();

                        string calcResult = EvalExpression(expression);

                        if ( calcResult.Length != 1 )
                        {
                            throw new Exception($"Result of expression is more then 1 DIGIT - sending response BYE");
                        }

                        string response = $"RESULT {calcResult}";
                        await writer.WriteLineAsync(response);
                    }
                    else // we recieved something else than 'BYE' or 'SOLVE'
                    {
                        throw new Exception($"Unexpected command received - sending response BYE");
                    }
                }
            }
            catch
            {
                await WriteIgnoreException(writer, "BYE"); // we will try to send bye to the client
            }

            clients.Remove(client);
            client.Dispose();
        }

        static void IPKCP_TCP_server(string host, int port)
        {
            // handle TCP server
            try
            {
                listener = new TcpListener(IPAddress.Parse(host), port);
                listener?.Start();
            }
            catch
            {
                ErrorExit("Error creating and starting TcpListener");
            }

            while (!cts.Token.IsCancellationRequested)
            {
                TcpClient? client = null;

                try
                {
                    client = listener?.AcceptTcpClient();
                }
                catch (SocketException ex)
                {
                    if (!cts.Token.IsCancellationRequested)
                    {
                        ErrorExit($"Chyba pri pripojovani klienta: {ex.Message}");
                    }
                }

                if (client != null)
                {
                    clients.Add(client);
                    HandleTcpClientAsync(client, cts.Token);
                }
            }
        }

        static async Task IPKCP_UDP_server(string host, int port)
        {
            IPAddress address = IPAddress.Parse(host);
            IPEndPoint localEndpoint = new(address, port);
            try
            {
                using (udpServer = new UdpClient(localEndpoint))
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var receiveTask = udpServer.ReceiveAsync();

                        if (await Task.WhenAny(receiveTask, Task.Delay(-1, cts.Token)) == receiveTask)
                        {
                            var result = await receiveTask;

                            string responseText = "";
                            byte[] receivedData = result.Buffer;
                            byte errCode = 0;
                            if (receivedData.Length < 3 || receivedData[0] != 0)
                            { 
                                responseText = "Malformed request received.";
                                errCode = 1;
                            }
                            else
                            {
                                int len = receivedData[1];
                                byte[] byteResp = receivedData.Skip(2).Take(len).ToArray();
                                string payload = Encoding.ASCII.GetString(byteResp);
                                try
                                {
                                    responseText = EvalExpression(payload);
                                }
                                catch (ArgumentException ex)
                                {
                                    responseText = ex.Message;
                                    errCode = 1;
                                }
                                if (errCode == 0)
                                {
                                    if (responseText.Length > 1) 
                                    {
                                        responseText = "Calulation result larger than one digit.";
                                        errCode = 1;
                                    }
                                }
                            }

                            // Send a response to the client
                            // Putting message into correct format before sending.
                            byte[] msg = Encoding.ASCII.GetBytes(responseText);
                            byte[] head = new byte[] { 1, errCode, (byte)msg.Length };
                            byte[] responseBytes = head.Concat(msg).ToArray(); // linq

                            await udpServer.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorExit($"UDP client ERROR: {ex.Message}");
            }
        }


        static void Main(string[] args)
        {
            CommandLineApplication app = new()
            {
                Name = "ipkcpd",
                Description = "Project 2 - IOTA: Server for Remote Calculator"
            };
            app.FullName = app.Name;

            app.HelpOption("--help");
            app.VersionOption("--version", "version 1.0.0");

            var hostOption = app.Option("-h|--host <VALUE>", "host IPv4 address to listen on [default: 0.0.0.0]", CommandOptionType.SingleValue);
            var modeOption = app.Option("-m|--mode <VARIANT>", "application mode to use [default: tcp] [choices: tcp, udp]", CommandOptionType.SingleValue);
            var portOption = app.Option("-p|--port <INT>", "port number to listen on [default: 2023]", CommandOptionType.SingleValue);

            app.ExtendedHelpText = "\nAuthor:\n  Ondrej Lukasek <xlukas15@stud.fit.vutbr.cz>";

            app.OnExecute(() =>
            {
                string mode = modeOption.HasValue() ? modeOption.Value() : "tcp";
                if (mode != "tcp" && mode != "udp")
                {
                    ErrorExitHelp("Error: Mode must be tcp or udp");
                }

                string host = hostOption.HasValue() ? hostOption.Value() : "0.0.0.0";

                int port = DEFAULT_PORT;
                try
                {
                    port = portOption.HasValue() ? int.Parse(portOption.Value()) : DEFAULT_PORT;
                    if (port <= 0 || port > 65535)
                        throw new Exception("Port out of range");
                }
                catch
                {
                    ErrorExitHelp("Error: Port must be whole number in range <1; 65535>");
                }

                Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPress);

                if (mode == "tcp")
                    IPKCP_TCP_server(host, port);
                else
                    IPKCP_UDP_server(host, port).Wait();

                return 0;
            });

            try
            {
                app.Execute(args);
            }
            catch
            {
                ErrorExit("Error parsing arguments");
            }
        }
    }
}


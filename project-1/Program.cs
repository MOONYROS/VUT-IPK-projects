// IPK - project 1 (2023) - Remote Calculator Protocol
// xlukas15 (xlukas15@stud.fit.vutbr.cz)

using System.Net.Sockets;
using System.Net;
using System.Text;

using UdpClient udpClient = new UdpClient();
using TcpClient tcpClient = new TcpClient();

// Reaction to Ctrl+C keypress
Console.CancelKeyPress += delegate {
    try
    {
        if (args[5] == "udp")
            udpClient.Close();
        else
        {
            // Finish TCP communication by sending "BYE" command and close stream and client
            using StreamWriter streamWriter = new StreamWriter(tcpClient.GetStream(), Encoding.ASCII);
            streamWriter.Write("BYE\n");
            streamWriter.Flush();
            streamWriter.Close();
            tcpClient.Close();
        }
    }
    finally
    {
        Environment.Exit(0);
    }
};

// Function to simplify calling errors.
static void errorExit(string msg)
{
    Console.Error.WriteLine(msg);
    Console.Error.WriteLine("Expected argument format: ipkcpc -h <host> -p <port> -m <mode>"); 
    Environment.Exit(-1);
}

// This if/else statement checks whether program was called using correct number and format of arguments.
if (args.Length != 6 && args.Length != 1)
    errorExit("Wrong number of arguments!");
if (args.Length == 1)
{
    if (args[0] != "--help")
        errorExit("Wrong argument. Try ./ipkcpc --help");
    else
    {
        Console.WriteLine("NAME:");
        Console.WriteLine("     ipkcpc - A calculator client over TCP / UDP");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("     ipkcpc -h <host> -p <port> -m <mode>");
        Console.WriteLine("     ipkcpc --help");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("     --help       display usage information and exit");
        Console.WriteLine("     -h <host>    host IPv4 address of server to connect");
        Console.WriteLine("     -p <port>    port number to connect to");
        Console.WriteLine("     -m <mode>    application mode to use [choices: tcp, udp]");
        Console.WriteLine();
        Console.WriteLine("AUTHOR:");
        Console.WriteLine("     Ondrej Lukasek <xlukas15@stud.fit.vutbr.cz>");

        Environment.Exit(0);
    }
}
else // I expect format of arguments to be "ipkcpc -h <host> -p <port> -m <mode>".
{
    if (args[0] != "-h")
        errorExit("Wrong -h argument");
    if (args[2] != "-p")
        errorExit("Wrong -p argument");
    if (args[4] != "-m")
        errorExit("Wrong -m argument");
    if (args[5] != "udp" && args[5] != "tcp")
        errorExit("Mode must be \"udp\" or \"tcp\"");
} // Other arguments (<host>, <port>) can be given in any shape by the user.

IPEndPoint ep = new IPEndPoint(IPAddress.Parse(args[1]), int.Parse(args[3])); // Endpoint where server is listening.

if (args[5] == "udp") // If the mode was set to UDP, we try to connect in that mode otherwise we exit with errorExit().
{
    try // We try to connect to the remote server.
    {
        udpClient.Connect(ep);
    }
    catch
    {
        errorExit("Error creating UDP connection."); // Oterwise we exit.
    }

    while (true) // The connection was successful.
    {
        string? cmd = Console.ReadLine();
        if (cmd != null)
        {
            // Putting message into correct format before sending.
            byte[] msg = Encoding.ASCII.GetBytes(cmd);
            byte[] head = new byte[] { 0, (byte)msg.Length };
            byte[] frame = head.Concat(msg).ToArray(); // linq

            try
            {
                udpClient.Send(frame, frame.Length); // Sending the message.
            }
            catch
            {
                errorExit("Error sending UDP data.");
            }

            byte[] receivedData = new byte[256];
            try
            {
                receivedData = udpClient.Receive(ref ep); // Recieving message.
            }
            catch
            {
                errorExit("Error receiving UDP data.");
            }

            if (receivedData[0] != 1)
                Console.Error.WriteLine("Something else than response received");
            else
            {
                if (receivedData[1] == 0) // Checking if recieved data were correct.
                    Console.Write("OK:");
                else
                    Console.Write("ERR:");

                int len = receivedData[2];
                byte[] byteResp = receivedData.Skip(3).Take(len).ToArray();
                string sznaky = Encoding.ASCII.GetString(byteResp);
                Console.WriteLine(sznaky); // Writing out recieved data.
            }
        }  
    }
}
else // TCP mode
{
    StreamWriter streamWriter;
    StreamReader streamReader;
    try
    {
        tcpClient.Connect(ep);
        streamWriter = new StreamWriter(tcpClient.GetStream(), Encoding.ASCII);
        streamWriter.NewLine = "\n"; // Communicating with LINUX server, so line ending is always \n
        streamReader = new StreamReader(tcpClient.GetStream(), Encoding.ASCII);
    }
    catch
    {
        errorExit("Error creating TCP connection.");
        return; // This part is (practically) unnecessary, but IntelliSense did not know that errorExit() exits the program.
    }

    while (true)
    {
        string? cmd = Console.ReadLine();
        if (cmd != null)
        {
            try
            {
                streamWriter.WriteLine(cmd); // Sending message to server.
                streamWriter.Flush();
            }
            catch
            {
                streamWriter.Close(); // If send operation is unsuccessful, we close the stream and connection later.
                streamReader.Close();
                errorExit("Error sending TCP stream data.");
            }
            // Then receive data.
            string? responseData = null;
            try
            {
                responseData = streamReader.ReadLine();
            }
            catch
            {
                streamWriter.Close(); // If recieving was unsuccessful, we close the stream and connection later.
                streamReader.Close();
                errorExit("Error receiving TCP stream data.");
            }

            if (responseData != null) // If recieved data are OK, we print them on the output.
            {
                Console.WriteLine(responseData);
                if (responseData == "BYE") // If recieved message was BYE, we exit the program because there is no point in keeping it running.
                {
                    streamWriter.Close();
                    streamReader.Close();
                    tcpClient.Close();
                    Environment.Exit(0);
                }
            }
        }
    }
}
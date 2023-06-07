# IPK project 2 (2023) -  IOTA: Server for Remote Calculator

Author: Ondrej Lukasek (xlukas15)

## General information

This project is about creating calculator server communicating through network with other clients (previous project `ipkcpc`). The server (`ipkcpd`) is communicating with clients through an internet port.
It accepts [instructions](https://git.fit.vutbr.cz/NESFIT/IPK-Projekty/src/branch/master/Project%201/Protocol.md) in the same format as the client in the [previous submission](https://git.fit.vutbr.cz/NESFIT/IPK-Projekty/src/branch/master/Project%201) was supposed to send.

It is able to communicate via 2 protocols:

* TCP
* UDP

## Prerequisites

* Working development environment to build the application.

    I was provided with `nix develop ./Devenv#csharp` command on the provided NixOS virtual machine.

* To run tests (described later in this file) you might need to give the `tst.sh` authorizations to run.

    You can do that by executing command `chmod +x tst.sh` in the folder, where the shell script is located.

* Working client in order to run testing script.
  
    I tested this task using my [first project](https://git.fit.vutbr.cz/xlukas15/IPK_project-1) I submitted (without any changes).

## Functionality

This project is programmed in C# language, using .NET 6 and using [NuGet package CommandLineUtils](https://www.nuget.org/packages/Microsoft.Extensions.CommandLineUtils/) (it does not solve the core of this project), testing is done using shell file comparison and the application can be built via Makefile.

The whole project was developed in Windows, however it can also be built in Linux using Makefile with proper environment development (tasting was done on NixOS operating system, according to specification).

### Usage

To compile and use this project, it is recommended to use attached Makefile and run it using `make` command, counting that you have working development environment, as said before. After successful compilation, you can use run the server the same way, as in the [specification](https://git.fit.vutbr.cz/NESFIT/IPK-Projekty/src/branch/master/Project%202/iota) for this project.

In both the UDP and TCP connection, the server is capable of having multiple clients connected to it and is able to solve sent instruction and send the results back to the original sender.

### UDP workflow

UDP server uses `UdpClient` class. For communication async methods are used (eg. `RecieveAsync`, `SendAsync`). Cancellation token is used for server termination. Main loop asynchronously waits for data, then processes them, calculates expression and at last sends result (or error message) to the endpoint from where the request came.

### TCP workflow

TCP server uses asynchronous tasks as well. Class `TcpListener` is used with its method `AcceptTcpClient` to wait for client connection.

To handle the client our own asynchronous method `HandleTcpClienAsync` is called. Server then waits for another client connection. Also, the client is added to the `clients` list.

`HandleTcpClienAsync` uses `StreamReader` and `StreamWriter` classes and its methods `ReadLineAsync` and `WriteLineAsync`. States are processed according to the [assignment](https://git.fit.vutbr.cz/NESFIT/IPK-Projekty/src/branch/master/Project%202/iota) and all possible errors are handled by throwing exceptions.

Cancellation token is used for server termination. All client connections are closed on exit.

### Expression evaluation

To process expressions, regular expression is used. Nested regex groups are handled by recursive functions. Internal expression evaluation is calculated using `double` variable type, but final result is checked to have only one digit as in the assignment. That means that intermediate results can have **more than one digit** but input numbers and end result are one digit only.

## Testing

The testing was done via automatic tests written in shell. Tests can be found in the `tests` folder and can be run with **already compiled server and client**, also the client needs to be in the same folder, as the server. You can run them using `./tst.sh`, but make sure the shell file has privilege to run. If not, you can use the command specified in the Prerequisites section.

The tests work on the basis of running the client with input file (`inp`) and its result will be saved into output file (`.out`) then the script compares the output file with the expected result file (`.res`). Because the tests are automatic, it does not matter how many tests there are in the `tests` folder. Any user can add or remove tests, but when adding the test, both the input file and the result file have to be present and have to be named the same.

### Input file example (`udp_multi.inp`)

```{.cs}
(+ 1 2)
(- 2 1)
(* 2 3)
(/ 8 4)
(+ 456)
```

### Result file example (`udp_multi.res`)

```{.cs}
OK:3
OK:1
OK:6
OK:2
ERR:Invalid expression format.
```

The tests test both the UDP and TCP connection and they are aimed to test all the possible inputs and errors that can occur. Mainly tested is correct expression evaluation and correct disconnecting clients.

It is also worth to mention that the script waits for one second after server start to initialize the server properly. Without it, it sometimes happened that the server was not yet initialized and the client could not connect to it, so the test crashed.

Similar thing, that takes a little while is the testing of UDP connection, because after every test, the client has to be killed and started again for another test, as there is no other way to close UDP client gracefully.

## Interesting parts of code

* For expression evaluation, regular expression is used to check correct syntax of the whole expression (including nested expressions). Capture groups are used to split the expression. Recursive calls are used for nested expressions.
* Asynchronous tasks are used for both UDP and TCP, so large number of clients can connect or access the server at once.
* Test script (`tst.sh`) is always waiting 1 second after server start, so the server is properly initialized and the testing client can connect to it.

## References

[1] [Instruction format](https://git.fit.vutbr.cz/NESFIT/IPK-Projekty/src/branch/master/Project%201/Protocol.md)

[2] [First task assignment](https://git.fit.vutbr.cz/NESFIT/IPK-Projekty/src/branch/master/Project%201)

[3] [CommandLineUtils NuGet package](https://www.nuget.org/packages/Microsoft.Extensions.CommandLineUtils/)

[4] [Project number 1](https://git.fit.vutbr.cz/xlukas15/IPK_project-1)

[5] [Second task assignment (IOTA variant)](https://git.fit.vutbr.cz/NESFIT/IPK-Projekty/src/branch/master/Project%202/iota)

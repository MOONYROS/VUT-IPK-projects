# IPK project 1 (2023) - Remote calculator client

## General information

This project is about creating application communicating through network. The application is communicating with another application through internet port.
It is sending instructions in format specified below.
It is communicating via 2 protocols:

- TCP
- UDP

This project is programmed in C# language, using .NET 6, testing is done using shell file comparison and the application can be built via Makefile.

## Prerequisites

- Working server for the remote calculator that connects to a port on network.
  
    I was provided with `ipkpd` command that created the server.

- Working development environment to build the application.

    I was provided with `nix develop ./Devenv#csharp` command on the provided NixOS virtual machine.

- To run tests (described later in this file) you might need to give the `tst.sh` authorizations to run.

You can do that by executing command `chmod +x tst.sh` in the folder, where the shell script is located.

## Functionality

As said, the application connects to a remote host and works with instructions.
Workflow depends on the protocol (specified further below).
The client is started using command `ipkcpc -h <host> -p <port> -m <mode>` where `host` is the IPv4 address of the server, `port` the server port, and `mode` either `tcp` or `udp` (e.g., ipkcpc -h 1.2.3.4 -p 2023 -m udp).
(this part of text was taken from the project task documentation about [protocols](https://git.fit.vutbr.cz/NESFIT/IPK-Projekty/src/branch/master/Project%201/README.md))
Client can be exitted using `Ctrl + C` command. This will lead to client exitting with exit code `0`.

### Instructions

Instructions have to be written in the format: `(<OPERATOR> <DIGIT> <DIGIT>)` or `<DIGIT>`, where:

- `<OPERATOR>` can be `+, -, *, /`.
- `<DIGIT>` can be any digit from `0 to 9`.

### UDP workflow

The application will connect to the remote server if the `mode` argument is set to `udp`, otherwise the client will not connect.
After the connection you can start sending instructions in the format specified above. Server will respond after every sent instruction.

### TCP workflow

The application will connect to the remote server if the `mode` argument is set to `udp`. In order to set the communication between server and application, client has to send `HELLO` instruction.
If the user wants the server to give results to sent instructions, user has to type `SOLVE` in front of every instruction like in this: `SOLVE (+ 1 2)`. The server should respond with `RESULT: 3`.

If the user sends anything else than `SOLVE` instruction, the server will respond with `BYE` and then closes the connection. Also the application exits with code 0 because server disconnected from it and so there is no point in keeping the application running (the user is not able to send any other instructions).

In order to continue sending instructions, the user needs to start the application in the format specified above.

## Testing

The testing was done via automatic tests writen in shell. Tests can be found in the `tests` folder and can be run with **already compiled** application via command `./tst.sh` or through `Makefile` using `make tests`. Both `Makefile` and `tst.sh` files have to be located in the same directory as the compiled application.

Tests work on the basis of comparing result files with files containing expected output. If any failure in testing phase occurs, testing will immediately stop. If tests ran correctly, all of them will have `OK` written by them.

## Extra functionality

- When wrong instruction is sent in the TCP mode, client exits with exit code 0. This is because the server will disconnect and can no longer communicate with the application, so there is no need in keeping the application running.

## Interesting parts of code

- I would point out the `errorExit()` function, which lets me exit the program with error and also lets me write a message for errors individually and very simply just by writing it into the function parameter.
- I should also point out, that I do a lot of exception handling using `try-catch` functions. I use them because in every code part, that communicates with the server, exception can happen by either not recieving or being unable to send the message or even by not even being able to connect to the server in the first place.

## References

[1] [Provided virtual machine](https://nextcloud.fit.vutbr.cz/s/YTxbDiCxFjHL29o)

[2] [Protocol documentation](https://git.fit.vutbr.cz/NESFIT/IPK-Projekty/src/branch/master/Project%201/README.md)

[3] [Try-catch description](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/try-catch)

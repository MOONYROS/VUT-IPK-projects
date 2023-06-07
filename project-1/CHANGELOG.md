# Changelog - IPK project 1 (2023)

## Functionality and limitations

### Format of arguments has to be specifically set in format, described in the [README.md](README.md) file of this project

That is because in the task description, there was nothing said about changing or not changing the order of arguments.
Also setting it in the format of `-h <host> -p <port> -m <mode>` makes the most sense.

### The application exits after recieving `BYE` from a remote server

Reason for this is that it makes the most sense. If the remote server closes connection, there is no reason to keep the application open because the remote server would not listen to it anyway.

### After the recieving `BYE` from the server, the application always exits with exit code 0, even if the input is wrong

That is because I think that the application shouldn't check, whether the input is correct. That should be dealt with on the server side. This is the reason why my application allows anything to be sent.

### Tests are written using shell only and not through any testing framework

Answer to this is quite simple, tests written only in shell (like mine) have next to no chance of crashing because of missing dependencies. They do need a little more labor, however they are the most simple for running in any machine without extensive setup.

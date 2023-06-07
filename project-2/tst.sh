HOST="127.0.0.1"
PORT="2023"

echo "Starting ipkcpd as TCP mode server"
./ipkcpd -m tcp &
serverPID=$!
sleep 1
for file in tests/tcp_*.inp
do
    echo ${file%%.*}
    ./ipkcpc -h 127.0.0.1 -p 2023 -m tcp <${file%%.*}.inp >${file%%.*}.out
    # echo "ipkpc:      " $?
    if [ $? -ne 0 ]
    then
        echo "Client exited with error!"
        kill $serverPID
        exit 1
    fi
    diff ${file%%.*}.res ${file%%.*}.out
    # echo "diff       :" $?
    if [ $? -ne 0 ]
    then
        echo "Expected results does not match!"
        kill $serverPID
        exit 1
    fi
    echo "OK!"
    rm ${file%%.*}.out
done
kill $serverPID

echo "Starting ipkpd as UDP mode server"
./ipkcpd -m udp &
serverPID=$!
sleep 1
for file in tests/udp_*.inp
do
    echo ${file%%.*}
    ./ipkcpc -h $HOST -p $PORT -m udp <${file%%.*}.inp >${file%%.*}.out &
    sleep 1
    kill $!
    diff ${file%%.*}.res ${file%%.*}.out
    # echo "diff       :" $?
    if [ $? -ne 0 ]
    then
        echo "Expected results does not match!"
        kill $serverPID
        exit 1
    fi
    echo "OK!"
    rm ${file%%.*}.out
done
kill $serverPID

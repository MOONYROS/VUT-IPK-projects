all : clean restore build publish
.PHONY: tests

clean:
	dotnet clean -c Release
	-rm ipkcpc ipkcpc.deps.json ipkcpc.dll ipkcpc.pdb ipkcpc.runtimeconfig.json
	-rm -r bin obj

restore:
	dotnet restore

build:
	dotnet build -c Release

publish:
	dotnet publish -c Release -o .

run:
	./ipkcpc -h 127.0.0.1 -p 2023 -m tcp

tests:
	bash ./tst.sh

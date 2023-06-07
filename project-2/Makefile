all : clean restore build publish

clean:
	dotnet clean -c Release
	-rm ipkcpd ipkcpd.deps.json *.dll ipkcpd.pdb ipkcpd.runtimeconfig.json
	-rm -r bin obj

restore:
	dotnet restore

build:
	dotnet build -c Release

publish:
	dotnet publish -c Release -o .

run:
	./ipkcpd

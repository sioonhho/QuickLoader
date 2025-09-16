scripts := $(wildcard scripts/*.csx)
references := $(wildcard references/*.dll)

all: linux windows

linux: build/QuickLoader-linux.zip

windows: build/QuickLoader-windows.zip

build/QuickLoader-linux.zip: install.sh $(scripts) $(references) QuickLoader.sh manifest.toml LICENSE build/linux
	mkdir -p QuickLoader
	cp -r build/linux QuickLoader/cli
	cp UndertaleModTool/LICENSE.txt QuickLoader/cli
	cp -r install.sh scripts references QuickLoader.sh manifest.toml LICENSE QuickLoader
	zip -rq9 $@ QuickLoader
	rm -rf QuickLoader

build/QuickLoader-windows.zip: install.bat $(scripts) $(references) QuickLoader.ps1 manifest.toml LICENSE build/windows
	mkdir -p QuickLoader
	cp -r build/windows QuickLoader/cli
	cp UndertaleModTool/LICENSE.txt QuickLoader/cli
	cp -r install.bat scripts references QuickLoader.ps1 manifest.toml LICENSE QuickLoader
	zip -rq9 $@ QuickLoader
	rm -rf QuickLoader

build/linux:
	dotnet publish UndertaleModTool/UndertaleModCli --verbosity quiet --runtime linux-x64 --self-contained --output build/linux

build/windows:
	dotnet publish UndertaleModTool/UndertaleModCli --verbosity quiet --runtime win-x64 --self-contained --output build/windows

clean:
	rm -rf build QuickLoader

.PHONY: all linux windows clean

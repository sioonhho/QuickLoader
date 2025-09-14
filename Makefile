scripts := $(wildcard scripts/*.csx)
references := $(wildcard references/*.dll)

all: linux windows

linux: build/QuickLoader-linux.zip

windows: build/QuickLoader-windows.zip

build/QuickLoader-linux.zip: $(scripts) $(references) QuickLoader.sh manifest.toml build/linux
	mkdir -p QuickLoader
	cp -r build/linux QuickLoader/cli
	cp UndertaleModTool/LICENSE.txt QuickLoader/cli
	cp -r scripts references QuickLoader.sh manifest.toml QuickLoader
	zip -rq9 $@ QuickLoader
	rm -rf QuickLoader

build/QuickLoader-windows.zip: $(scripts) $(references) QuickLoader.ps1 manifest.toml build/windows
	mkdir -p QuickLoader
	cp -r build/windows QuickLoader/cli
	cp UndertaleModTool/LICENSE.txt QuickLoader/cli
	cp -r scripts references QuickLoader.ps1 manifest.toml QuickLoader
	zip -rq9 $@ QuickLoader
	rm -rf QuickLoader

build/linux:
	dotnet publish UndertaleModTool/UndertaleModCli --verbosity quiet --runtime linux-x64 --self-contained --output build/linux

build/windows:
	dotnet publish UndertaleModTool/UndertaleModCli --verbosity quiet --runtime win-x64 --self-contained --output build/windows

clean:
	rm -rf build QuickLoader

.PHONY: all linux windows clean

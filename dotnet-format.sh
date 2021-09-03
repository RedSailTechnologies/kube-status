#!/bin/bash

mkdir -p bin

FILE=./bin/dotnet-format.exe
if [[ ! -f "$FILE" ]]; then
    echo "dotnet-format not found, installing..."
    dotnet tool install --tool-path ./bin dotnet-format
fi

$FILE ./src --folder
